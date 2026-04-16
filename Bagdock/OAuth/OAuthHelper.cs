using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Bagdock.OAuth;

public class OAuthException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public OAuthException(string message, string errorCode = "oauth_error", int statusCode = 0)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

public record PKCEPair(string CodeVerifier, string CodeChallenge);

public record TokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken = null,
    string? IdToken = null,
    string? Scope = null);

public record DeviceAuthResponse(
    string DeviceCode,
    string UserCode,
    string VerificationUri,
    int ExpiresIn,
    int Interval,
    string? VerificationUriComplete = null);

public static class OAuthHelper
{
    private const string DefaultIssuer = "https://api.bagdock.com";
    private const string DeviceCodeGrantType = "urn:ietf:params:oauth:grant-type:device_code";

    public static PKCEPair GeneratePKCE()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var codeVerifier = Base64UrlEncode(bytes);
        var digest = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = Base64UrlEncode(digest);
        return new PKCEPair(codeVerifier, codeChallenge);
    }

    public static string BuildAuthorizeUrl(
        string clientId, string redirectUri, string codeChallenge,
        string? scope = null, string? state = null, string? issuer = null)
    {
        issuer = (issuer ?? DefaultIssuer).TrimEnd('/');
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = clientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["code_challenge"] = codeChallenge;
        query["code_challenge_method"] = "S256";
        if (scope != null) query["scope"] = scope;
        if (state != null) query["state"] = state;
        return $"{issuer}/oauth2/authorize?{query}";
    }

    public static async Task<TokenResponse> ExchangeCodeAsync(
        string clientId, string code, string redirectUri, string codeVerifier,
        string? clientSecret = null, string? issuer = null)
    {
        issuer = (issuer ?? DefaultIssuer).TrimEnd('/');
        var data = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
        };
        if (clientSecret != null) data["client_secret"] = clientSecret;
        return ToTokenResponse(await PostFormAsync($"{issuer}/oauth2/token", data));
    }

    public static async Task<TokenResponse> RefreshTokenAsync(
        string clientId, string refreshToken,
        string? clientSecret = null, string? issuer = null)
    {
        issuer = (issuer ?? DefaultIssuer).TrimEnd('/');
        var data = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId,
            ["refresh_token"] = refreshToken,
        };
        if (clientSecret != null) data["client_secret"] = clientSecret;
        return ToTokenResponse(await PostFormAsync($"{issuer}/oauth2/token", data));
    }

    public static async Task RevokeTokenAsync(
        string token, string? tokenTypeHint = null, string? issuer = null)
    {
        issuer = (issuer ?? DefaultIssuer).TrimEnd('/');
        var data = new Dictionary<string, string> { ["token"] = token };
        if (tokenTypeHint != null) data["token_type_hint"] = tokenTypeHint;
        await PostFormAsync($"{issuer}/oauth2/token/revoke", data);
    }

    public static async Task<DeviceAuthResponse> DeviceAuthorizeAsync(
        string clientId, string? scope = null, string? issuer = null)
    {
        issuer = (issuer ?? DefaultIssuer).TrimEnd('/');
        var data = new Dictionary<string, string> { ["client_id"] = clientId };
        if (scope != null) data["scope"] = scope;
        var body = await PostFormAsync($"{issuer}/oauth2/device/authorize", data);
        return new DeviceAuthResponse(
            body.GetProperty("device_code").GetString()!,
            body.GetProperty("user_code").GetString()!,
            body.GetProperty("verification_uri").GetString()!,
            body.GetProperty("expires_in").GetInt32(),
            body.GetProperty("interval").GetInt32(),
            body.TryGetProperty("verification_uri_complete", out var v) ? v.GetString() : null);
    }

    public static async Task<TokenResponse> PollDeviceTokenAsync(
        string clientId, string deviceCode,
        int interval = 5, int timeout = 600, string? issuer = null)
    {
        issuer = (issuer ?? DefaultIssuer).TrimEnd('/');
        var deadline = DateTimeOffset.UtcNow.AddSeconds(timeout);
        var pollInterval = interval;

        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(pollInterval * 1000);
            var data = new Dictionary<string, string>
            {
                ["grant_type"] = DeviceCodeGrantType,
                ["client_id"] = clientId,
                ["device_code"] = deviceCode,
            };
            try
            {
                return ToTokenResponse(await PostFormAsync($"{issuer}/oauth2/token", data));
            }
            catch (OAuthException ex) when (ex.ErrorCode == "authorization_pending")
            {
                continue;
            }
            catch (OAuthException ex) when (ex.ErrorCode == "slow_down")
            {
                pollInterval += 5;
            }
        }

        throw new OAuthException("Device authorization timed out", "expired_token", 408);
    }

    internal static async Task<JsonElement> PostFormAsync(string url, Dictionary<string, string> data)
    {
        using var http = new HttpClient();
        using var content = new FormUrlEncodedContent(data);
        var resp = await http.PostAsync(url, content);
        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!resp.IsSuccessStatusCode)
        {
            var errCode = doc.RootElement.TryGetProperty("error", out var ec) ? ec.GetString() ?? "oauth_error" : "oauth_error";
            var errDesc = doc.RootElement.TryGetProperty("error_description", out var ed) ? ed.GetString() ?? $"HTTP {(int)resp.StatusCode}" : $"HTTP {(int)resp.StatusCode}";
            throw new OAuthException(errDesc, errCode, (int)resp.StatusCode);
        }

        return doc.RootElement;
    }

    private static TokenResponse ToTokenResponse(JsonElement el)
    {
        return new TokenResponse(
            el.GetProperty("access_token").GetString()!,
            el.GetProperty("token_type").GetString()!,
            el.GetProperty("expires_in").GetInt32(),
            el.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            el.TryGetProperty("id_token", out var it) ? it.GetString() : null,
            el.TryGetProperty("scope", out var sc) ? sc.GetString() : null);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
