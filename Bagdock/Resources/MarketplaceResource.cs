using System.Net.Http.Json;
using System.Text.Json;

namespace Bagdock.Resources;

public class MarketplaceResource
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    internal MarketplaceResource(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<JsonElement> SearchAsync(CancellationToken ct = default) =>
        await GetAsync("marketplace/search", ct);

    public async Task<JsonElement> GetListingAsync(string id, CancellationToken ct = default) =>
        await GetAsync($"marketplace/listings/{id}", ct);

    public async Task<JsonElement> CreateRentalAsync(object data, CancellationToken ct = default) =>
        await PostAsync("marketplace/rentals", data, ct);

    public async Task<JsonElement> GetRentalAsync(string id, CancellationToken ct = default) =>
        await GetAsync($"marketplace/rentals/{id}", ct);

    private async Task<JsonElement> GetAsync(string path, CancellationToken ct)
    {
        var response = await _http.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(_json, ct);
    }

    private async Task<JsonElement> PostAsync(string path, object data, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(path, data, _json, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(_json, ct);
    }
}
