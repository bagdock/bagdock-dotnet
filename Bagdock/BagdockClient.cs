using System.Net.Http.Json;
using System.Text.Json;
using Bagdock.OAuth;
using Bagdock.Resources;

namespace Bagdock;

public class BagdockClient : IDisposable
{
    private const int DefaultMaxRetries = 3;
    private const int MaxRetryCap = 5;

    private readonly HttpClient _http;
    private readonly TokenManager? _tokenManager;
    private readonly string? _staticToken;
    internal readonly int MaxRetries;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public OperatorResource Operator { get; }
    public MarketplaceResource Marketplace { get; }
    public LoyaltyResource Loyalty { get; }

    /// <summary>Create with an API key.</summary>
    public BagdockClient(string apiKey, string? baseUrl = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new BagdockException("Missing API key");

        _staticToken = apiKey;
        _tokenManager = null;
        _http = BuildHttpClient(baseUrl, apiKey);
        Operator = new OperatorResource(_http, JsonOptions);
        Marketplace = new MarketplaceResource(_http, JsonOptions);
        Loyalty = new LoyaltyResource(_http, JsonOptions);
    }

    /// <summary>Create with an existing OAuth access token.</summary>
    public static BagdockClient WithAccessToken(string accessToken, string? baseUrl = null)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new BagdockException("Missing access token");
        return new BagdockClient(accessToken, baseUrl);
    }

    /// <summary>Create with OAuth2 client credentials (auto-fetches tokens).</summary>
    public static BagdockClient WithClientCredentials(
        string clientId, string clientSecret,
        string[]? scopes = null, string? issuer = null, string? baseUrl = null)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new BagdockException("Missing clientId or clientSecret");

        var tm = new TokenManager(clientId, clientSecret, scopes, issuer);
        return new BagdockClient(tm, baseUrl);
    }

    private BagdockClient(TokenManager tokenManager, string? baseUrl, int maxRetries = DefaultMaxRetries)
    {
        _staticToken = null;
        _tokenManager = tokenManager;
        MaxRetries = Math.Min(maxRetries, MaxRetryCap);
        _http = BuildHttpClient(baseUrl, null);
        Operator = new OperatorResource(_http, JsonOptions);
        Marketplace = new MarketplaceResource(_http, JsonOptions);
        Loyalty = new LoyaltyResource(_http, JsonOptions);
    }

    private static HttpClient BuildHttpClient(string? baseUrl, string? bearerToken)
    {
        var url = (baseUrl ?? "https://api.bagdock.com/api/v1").TrimEnd('/');
        var http = new HttpClient { BaseAddress = new Uri(url + "/") };
        if (bearerToken != null)
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
        http.DefaultRequestHeaders.Add("User-Agent", "bagdock-dotnet/0.1.0");
        http.Timeout = TimeSpan.FromSeconds(30);
        return http;
    }

    internal async Task<string> ResolveTokenAsync()
    {
        if (_staticToken != null) return _staticToken;
        return await _tokenManager!.GetTokenAsync();
    }

    internal void InvalidateToken() => _tokenManager?.Invalidate();

    internal HttpClient Http => _http;
    internal static JsonSerializerOptions Json => JsonOptions;

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
}
