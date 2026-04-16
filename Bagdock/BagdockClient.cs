using System.Net.Http.Json;
using System.Text.Json;
using Bagdock.Resources;

namespace Bagdock;

public class BagdockClient : IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public OperatorResource Operator { get; }
    public MarketplaceResource Marketplace { get; }
    public LoyaltyResource Loyalty { get; }

    public BagdockClient(string apiKey, string? baseUrl = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new BagdockException("Missing API key");

        var url = (baseUrl ?? "https://api.bagdock.com/api/v1").TrimEnd('/');

        _http = new HttpClient { BaseAddress = new Uri(url + "/") };
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _http.DefaultRequestHeaders.Add("User-Agent", "bagdock-dotnet/0.1.0");
        _http.Timeout = TimeSpan.FromSeconds(30);

        Operator = new OperatorResource(_http, JsonOptions);
        Marketplace = new MarketplaceResource(_http, JsonOptions);
        Loyalty = new LoyaltyResource(_http, JsonOptions);
    }

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
}
