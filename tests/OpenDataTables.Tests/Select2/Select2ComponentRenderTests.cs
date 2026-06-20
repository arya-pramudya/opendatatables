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
    public async Task ExtraParamsHtml_emits_live_dom_sourced_param_in_config()
    {
        // ExtraParamsHTML maps a param name to a CSS selector whose value is read at request time. It must
        // reach the rendered JSON config under "extraParamsHTML" (so openselect2.js sends the live value),
        // not be dropped or folded into the static "extraParams".
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/Select2")).Content.ReadAsStringAsync();

        Assert.Contains("\"extraParamsHTML\":{\"maxPrice\":", html, StringComparison.Ordinal);
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

    [Fact]
    public async Task ExtraAttributes_value_is_html_encoded_preventing_attribute_breakout()
    {
        // Regression test for A4: ExtraAttributes values must be HTML-attribute-encoded.
        // A raw double-quote in the value must not be able to break out of the attribute context.
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/Home/TestExtraAttributesXss");

        // The encoded form must appear, confirming the quote was not passed through raw.
        Assert.Contains("&quot;", html, StringComparison.Ordinal);
        // Neither double-quote nor single-quote breakout forms should appear as a live attribute.
        Assert.DoesNotContain("onmouseover=\"alert(1)\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("onmouseover='alert(1)'", html, StringComparison.Ordinal);
        // The full encoded attribute value must appear correctly enclosed.
        Assert.Contains("data-x=\"a&quot; onmouseover=&quot;alert(1)\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtraAttributes_event_handler_names_are_rejected()
    {
        // Regression test for A4 hardening: on* attribute names must be silently dropped.
        // The controller supplies ["onclick"] = "alert('xss')" which must not appear in the output.
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/Home/TestExtraAttributesXss");

        // The onclick key must have been dropped — neither the attribute name nor the JS value should appear.
        Assert.DoesNotContain("onclick=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("alert('xss')", html, StringComparison.Ordinal);
        // The safe data-x attribute must still be present (verify the filter is key-level, not wholesale).
        Assert.Contains("data-x=", html, StringComparison.Ordinal);
    }
}
