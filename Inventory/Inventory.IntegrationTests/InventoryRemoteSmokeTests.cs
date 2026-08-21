using System.Net;

namespace Inventory.IntegrationTests;

public sealed class InventoryRemoteSmokeTests
{
    [Fact]
    [Trait("Category", "Prod")]
    public async Task GetRoot_ReturnsOkFromConfiguredAzureEndpoint()
    {
        var baseUrl = Environment.GetEnvironmentVariable("INVENTORY_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute)
        };

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Inventory API is running.", content);
    }
}
