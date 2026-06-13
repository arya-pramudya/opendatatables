using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OpenDataTables.Tests.DataTable;

public class DataTableComponentRenderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DataTableComponentRenderTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Demo_page_renders_table_filtercard_and_runtime()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/DataTable")).Content.ReadAsStringAsync();

        Assert.Contains("id=\"tblProducts\"", html, StringComparison.Ordinal);
        Assert.Contains("data-component=\"DataTable\"", html, StringComparison.Ordinal);
        Assert.Contains("data-component=\"FilterCard\"", html, StringComparison.Ordinal);
        Assert.Contains("window.OpenDataTables.config=", html, StringComparison.Ordinal);
        Assert.Contains("data-odt-action=\"add\"", html, StringComparison.Ordinal); // delegated, not inline onclick
        Assert.DoesNotContain("onclick=\"onAddRow", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Editable_grid_is_the_same_DataTable_component_with_editor_configs()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/Editable")).Content.ReadAsStringAsync();

        // One component: the editable page renders a DataTable config block (not a separate component)…
        Assert.Contains("data-component=\"DataTable\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-component=\"EditableDataTable\"", html, StringComparison.Ordinal);
        // …with editor configs and the inline-edit toolbar.
        Assert.Contains("\"editorConfigs\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"tblEdit-edit-mode-btn\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"tblEdit-save-btn\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Style_and_script_assets_are_served()
    {
        var client = _factory.CreateClient();

        var css = await client.GetAsync("/_content/OpenDataTables.AspNetCore/css/opendatatables.css");
        var js = await client.GetAsync("/_content/OpenDataTables.AspNetCore/js/opendatatables-datatable.js");

        Assert.True(css.IsSuccessStatusCode);
        Assert.True(js.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetData_returns_datatables_contract()
    {
        var client = _factory.CreateClient();

        var form = new Dictionary<string, string>
        {
            ["Draw"] = "1", ["Start"] = "0", ["Length"] = "5",
            ["SortColumnName"] = "name", ["SortDirection"] = "asc"
        };
        var resp = await client.PostAsync("/Products/GetData", new FormUrlEncodedContent(form));
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("1", root.GetProperty("draw").GetString());
        Assert.Equal(120, root.GetProperty("recordsTotal").GetInt32());
        Assert.True(root.GetProperty("data").GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetData_applies_filter_param()
    {
        var client = _factory.CreateClient();

        var form = new Dictionary<string, string>
        {
            ["Draw"] = "2", ["Start"] = "0", ["Length"] = "100",
            ["SortColumnName"] = "name", ["SortDirection"] = "asc", ["status"] = "Active"
        };
        var resp = await client.PostAsync("/Products/GetData", new FormUrlEncodedContent(form));
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        Assert.True(data.GetArrayLength() > 0);
        foreach (var row in data.EnumerateArray())
            Assert.Equal("Active", row.GetProperty("status").GetString());
    }
}
