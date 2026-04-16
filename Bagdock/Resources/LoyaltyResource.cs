using System.Net.Http.Json;
using System.Text.Json;

namespace Bagdock.Resources;

public class LoyaltyResource
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    internal LoyaltyResource(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<JsonElement> ListMembersAsync(CancellationToken ct = default) =>
        await GetAsync("loyalty/members", ct);

    public async Task<JsonElement> GetMemberAsync(string id, CancellationToken ct = default) =>
        await GetAsync($"loyalty/members/{id}", ct);

    public async Task<JsonElement> AwardPointsAsync(object data, CancellationToken ct = default) =>
        await PostAsync("loyalty/points/award", data, ct);

    public async Task<JsonElement> ListRewardsAsync(CancellationToken ct = default) =>
        await GetAsync("loyalty/rewards", ct);

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
