```
  ----++                                ----++                    ---+++     
  ---+++                                ---++                     ---++      
 ----+---     -----     ---------  --------++ ------     -----   ----++----- 
 ---------+ --------++----------++--------+++--------+ --------++---++---++++
 ---+++---++ ++++---++---+++---++---+++---++---+++---++---++---++------++++  
----++ ---++--------++---++----++---++ ---++---++ ---+---++     -------++    
----+----+---+++---++---++----++---++----++---++---+++--++ --------+---++   
---------++--------+++--------+++--------++ -------+++ -------++---++----++  
 +++++++++   +++++++++- +++---++   ++++++++    ++++++    ++++++  ++++  ++++  
                     --------+++                                             
                       +++++++                                               
```

# Bagdock.NET

The official .NET SDK for the Bagdock API — manage facilities, contacts, tenancies, invoices, marketplace listings, and loyalty programs with async/await and System.Text.Json.

[![NuGet](https://img.shields.io/nuget/v/Bagdock.svg)](https://www.nuget.org/packages/Bagdock)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Install

```bash
dotnet add package Bagdock
```

Or via the NuGet Package Manager:

```powershell
Install-Package Bagdock
```

## Quick start

```csharp
using Bagdock;

var client = new BagdockClient(Environment.GetEnvironmentVariable("BAGDOCK_API_KEY")!);

// List operator facilities
var facilities = await client.Operator.ListFacilitiesAsync();

// Create a contact
var contact = await client.Operator.CreateContactAsync(new
{
    Email = "jane@example.com",
    FirstName = "Jane",
    LastName = "Doe"
});

// Search marketplace
var results = await client.Marketplace.SearchAsync();
```

## API reference

### `client.Operator`

| Method | Description |
|--------|-------------|
| `ListFacilitiesAsync(ct?)` | List facilities |
| `GetFacilityAsync(id, ct?)` | Get a facility |
| `ListContactsAsync(ct?)` | List contacts |
| `CreateContactAsync(data, ct?)` | Create a contact |
| `ListListingsAsync(ct?)` | List listings |
| `ListTenanciesAsync(ct?)` | List tenancies |
| `ListUnitsAsync(ct?)` | List units |
| `ListInvoicesAsync(ct?)` | List invoices |

### `client.Marketplace`

| Method | Description |
|--------|-------------|
| `SearchAsync(ct?)` | Search marketplace locations |
| `GetListingAsync(id, ct?)` | Get a listing |
| `CreateRentalAsync(data, ct?)` | Create a rental |
| `GetRentalAsync(id, ct?)` | Get a rental |

### `client.Loyalty`

| Method | Description |
|--------|-------------|
| `ListMembersAsync(ct?)` | List loyalty members |
| `GetMemberAsync(id, ct?)` | Get a member |
| `AwardPointsAsync(data, ct?)` | Award points |
| `ListRewardsAsync(ct?)` | List rewards |

## Error handling

```csharp
try
{
    var facility = await client.Operator.GetFacilityAsync("fac_nonexistent");
}
catch (BagdockApiException ex)
{
    Console.Error.WriteLine($"API error {ex.StatusCode}: {ex.ErrorCode} — {ex.Message}");
    Console.Error.WriteLine($"Request ID: {ex.RequestId}");
}
```

## Configuration

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `apiKey` | `string` | — | **Required.** Your Bagdock API key |
| `baseUrl` | `string?` | `https://api.bagdock.com/api/v1` | API base URL |

## License

MIT — see [LICENSE](LICENSE)
