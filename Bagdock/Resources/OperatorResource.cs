using System.Net.Http.Json;
using System.Text.Json;

namespace Bagdock.Resources;

public class OperatorResource
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    internal OperatorResource(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<JsonElement> ListFacilitiesAsync(CancellationToken ct = default) =>
        await GetAsync("operator/facilities", ct);

    public async Task<JsonElement> GetFacilityAsync(string id, CancellationToken ct = default) =>
        await GetAsync($"operator/facilities/{id}", ct);

    public async Task<JsonElement> ListContactsAsync(CancellationToken ct = default) =>
        await GetAsync("operator/contacts", ct);

    public async Task<JsonElement> CreateContactAsync(object data, CancellationToken ct = default) =>
        await PostAsync("operator/contacts", data, ct);

    public async Task<JsonElement> ListListingsAsync(CancellationToken ct = default) =>
        await GetAsync("operator/listings", ct);

    public async Task<JsonElement> ListTenanciesAsync(CancellationToken ct = default) =>
        await GetAsync("operator/tenancies", ct);

    public async Task<JsonElement> ListUnitsAsync(CancellationToken ct = default) =>
        await GetAsync("operator/units", ct);

    public async Task<JsonElement> ListInvoicesAsync(CancellationToken ct = default) =>
        await GetAsync("operator/invoices", ct);

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
