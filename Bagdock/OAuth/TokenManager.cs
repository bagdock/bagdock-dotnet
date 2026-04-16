using System.Text.Json;

namespace Bagdock.OAuth;

internal class TokenManager
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string[]? _scopes;
    private readonly string _tokenUrl;
    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
    private readonly object _lock = new();

    public TokenManager(string clientId, string clientSecret, string[]? scopes = null, string? issuer = null)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scopes = scopes;
        _tokenUrl = (issuer ?? "https://api.bagdock.com").TrimEnd('/') + "/oauth2/token";
    }

    public async Task<string> GetTokenAsync()
    {
        lock (_lock)
        {
            if (_accessToken != null && DateTimeOffset.UtcNow < _expiresAt)
                return _accessToken;
        }

        return await FetchTokenAsync();
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            _accessToken = null;
            _expiresAt = DateTimeOffset.MinValue;
        }
    }

    private async Task<string> FetchTokenAsync()
    {
        var data = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
        };
        if (_scopes is { Length: > 0 })
            data["scope"] = string.Join(" ", _scopes);

        var el = await OAuthHelper.PostFormAsync(_tokenUrl, data);
        var token = el.GetProperty("access_token").GetString()!;
        var expiresIn = el.GetProperty("expires_in").GetInt32();

        lock (_lock)
        {
            _accessToken = token;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
        }

        return token;
    }
}
