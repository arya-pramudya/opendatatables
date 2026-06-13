using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OpenDataTables.Tests.Select2;

/// <summary>End-to-end render/contract tests booting the SampleApp via WebApplicationFactory.</summary>
public class Select2ComponentRenderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Select2ComponentRenderTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Demo_page_renders_components_and_runtime()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Home/Select2");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // No per-instance inline script — just JSON config blocks + the scanner runtime.
        Assert.Contains("data-component=\"Select2\"", html, StringComparison.Ordinal);
        // The static dropdown is the same Select2 component with a local list (isStatic flag).
        Assert.Contains("\"isStatic\":true", html, StringComparison.Ordinal);
        Assert.Contains("window.OpenSelect2.config=", html, StringComparison.Ordinal);
        Assert.Contains("openselect2", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preselect_hook_locks_the_control()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/Select2")).Content.ReadAsStringAsync();

        // The SamplePreselector preselects + disables the "first-category" dropdown.
        var idx = html.IndexOf("id=\"preselCategory\"", StringComparison.Ordinal);
        Assert.True(idx >= 0, "preselCategory select not found");
        var tagEnd = html.IndexOf('>', idx);
        Assert.Contains("disabled", html[idx..tagEnd], StringComparison.Ordinal);
        Assert.Contains(">Electronics</option>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Static_asset_is_served_from_content_root()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/_content/OpenSelect2.AspNetCore/js/openselect2.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var js = await response.Content.ReadAsStringAsync();
        Assert.Contains("window.OpenSelect2", js, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lookup_endpoint_returns_items_hasMore_contract()
    {
        var client = _factory.CreateClient();

        var json = await client.GetStringAsync("/Lookup/Categories?searchTerm=&page=1&limit=3");

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("items", out var items));
        Assert.True(doc.RootElement.TryGetProperty("hasMore", out _));
        Assert.True(items.GetArrayLength() <= 3);
        Assert.True(items[0].TryGetProperty("id", out _));
        Assert.True(items[0].TryGetProperty("text", out _));
    }
}
