using Microsoft.AspNetCore.Mvc.Testing;

namespace Inventory.Tests;

public sealed class InventoryRootEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public InventoryRootEndpointTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetRoot_ReturnsInventoryApiRunningMessage()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Inventory API is running.", content);
    }
}
