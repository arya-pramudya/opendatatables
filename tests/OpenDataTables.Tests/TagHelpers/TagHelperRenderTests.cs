using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OpenDataTables.Tests.TagHelpers;

/// <summary>
/// End-to-end tests for the declarative tag-helper syntax (<c>&lt;os2-select&gt;</c> / <c>&lt;odt-table&gt;</c>).
/// The /Home/TagHelpers page mirrors the imperative demos; these assertions confirm the tag helpers
/// emit the same component markup, JSON config blocks, and runtime hooks as <c>Component.InvokeAsync</c>.
/// </summary>
public class TagHelperRenderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TagHelperRenderTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Os2_select_tag_renders_the_Select2_component()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // Same component output as Component.InvokeAsync("Select2", …): a JSON config block + runtime.
        Assert.Contains("data-component=\"Select2\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"tagCategory\"", html, StringComparison.Ordinal);
        // The cascade child carries its parent id (parent-id attribute).
        Assert.Contains("data-parent-id=\"tagCategory\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Os2_item_children_produce_a_static_list_with_selection()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // Nested <os2-item> children turn the dropdown into a local list (isStatic) …
        Assert.Contains("\"isStatic\":true", html, StringComparison.Ordinal);
        // … with the options rendered as <option> elements, the first one selected.
        Assert.Contains("<option value=\"Active\" selected=\"selected\">Active</option>", html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"Inactive\">Inactive</option>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Os2_select_extra_html_param_emits_live_dom_sourced_param()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // Regression: extra-html-param-{name} must populate ExtraParamsHTML. Its prefix is deliberately NOT
        // a superset of the static extra-param- prefix, otherwise Razor would bind it to ExtraParams (the
        // first matching prefix) and the live param would silently never be sent. (HTML lowercases the
        // attribute name, so the key arrives as "maxprice".)
        Assert.Contains("\"extraParamsHTML\":{\"maxprice\":", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Odt_table_tag_renders_the_DataTable_component_with_columns()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // Same component output as Component.InvokeAsync("DataTable", …).
        Assert.Contains("id=\"tblTagProducts\"", html, StringComparison.Ordinal);
        Assert.Contains("data-component=\"DataTable\"", html, StringComparison.Ordinal);
        // Columns supplied via <odt-column> children are present as headers …
        Assert.Contains("<th>Name</th>", html, StringComparison.Ordinal);
        Assert.Contains("<th>Status</th>", html, StringComparison.Ordinal);
        // … and a filter column makes the filter card render.
        Assert.Contains("data-component=\"FilterCard\"", html, StringComparison.Ordinal);
        // Action buttons are delegated (CSP-friendly), not inline onclick.
        Assert.Contains("data-odt-action=\"add\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Odt_column_static_options_attribute_populates_select_static_filter()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // static-options="Active,Inactive,Discontinued" must reach the rendered config/filter.
        Assert.Contains("Discontinued", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Odt_child_column_children_enable_child_rows_declaratively()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // <odt-child-column> children turn on child rows (no config model) and define the child grid.
        Assert.Contains("\"hasChildRows\":true", html, StringComparison.Ordinal);
        Assert.Contains("odt-details-control", html, StringComparison.Ordinal);
        // child-param-category="row.id" reaches the child request params.
        Assert.Contains("\"childCustomParameters\":{\"category\":\"row.id\"}", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Odt_button_child_renders_a_custom_toolbar_button()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // <odt-button placement="top" on-click="tagExport"> renders a delegated (CSP-friendly) button.
        Assert.Contains("data-odt-handler=\"tagExport\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Group_by_attribute_wires_the_rowgroup_extension()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // group-by="category" emits the RowGroup escape-hatch option via DataTableOptions.
        Assert.Contains("\"rowGroup\":{\"dataSrc\":\"category\"}", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Column_editor_attribute_makes_the_table_editable_declaratively()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // editor="…" on <odt-column> produces editor configs and the inline-edit toolbar (no config model).
        Assert.Contains("\"editorConfigs\":[", html, StringComparison.Ordinal);
        Assert.Contains("id=\"tblTagEdit-edit-mode-btn\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unsaved_edit_behavior_attribute_reaches_the_config()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Home/TagHelpers")).Content.ReadAsStringAsync();

        // unsaved-edit-behavior="AutoSave" on the editable tag table must flow through to the JSON config
        // (serialized lower-cased) so the runtime guard persists edits before a page change.
        Assert.Contains("\"unsavedEditBehavior\":\"autosave\"", html, StringComparison.Ordinal);
    }
}
