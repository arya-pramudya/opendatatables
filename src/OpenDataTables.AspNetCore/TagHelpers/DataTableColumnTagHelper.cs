using Microsoft.AspNetCore.Razor.TagHelpers;
using OpenDataTables.AspNetCore.Models;
using OpenSelect2.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.TagHelpers;

/// <summary>
/// A single column inside an <c>&lt;odt-table&gt;</c>. Renders nothing itself — it registers the column
/// on the parent via <see cref="TagHelperContext.Items"/>.
/// </summary>
/// <remarks>
/// Usage: <c>&lt;odt-column data="name" title="Name" filter="Text" /&gt;</c>.
/// For a <c>SelectStatic</c> filter, supply <c>static-options="Active,Inactive"</c> (id == text). For
/// value≠text options or other advanced column settings, build the table with a <c>config</c> model.
/// </remarks>
[HtmlTargetElement("odt-column", ParentTag = "odt-table")]
public class DataTableColumnTagHelper : TagHelper
{
    /// <summary>The data-source property bound to this column (required).</summary>
    [HtmlAttributeName("data")] public string? Data { get; set; }

    /// <summary>The column header text (defaults to <see cref="Data"/> when omitted).</summary>
    [HtmlAttributeName("title")] public string? Title { get; set; }

    /// <summary>The filter input type for this column.</summary>
    [HtmlAttributeName("filter")] public DataTableFilterType Filter { get; set; } = DataTableFilterType.None;

    /// <summary>Whether the column is visible.</summary>
    [HtmlAttributeName("visible")] public bool Visible { get; set; } = true;

    /// <summary>Whether the column is sortable.</summary>
    [HtmlAttributeName("sortable")] public bool Sortable { get; set; } = true;

    /// <summary>Cell display format (formatter function name or moment-style date token).</summary>
    [HtmlAttributeName("format")] public string? Format { get; set; }

    /// <summary>URL for AJAX-loaded select options.</summary>
    [HtmlAttributeName("options-url")] public string? OptionsUrl { get; set; }

    /// <summary>Field name for the value in AJAX option results.</summary>
    [HtmlAttributeName("option-value-field")] public string? OptionValueField { get; set; }

    /// <summary>Field name for the display text in AJAX option results.</summary>
    [HtmlAttributeName("option-text-field")] public string? OptionTextField { get; set; }

    /// <summary>Comma-separated static options for a <see cref="DataTableFilterType.SelectStatic"/> filter (id == text).</summary>
    [HtmlAttributeName("static-options")] public string? StaticOptions { get; set; }

    /// <summary>Data property of the parent column to cascade this filter on.</summary>
    [HtmlAttributeName("parent-filter-column")] public string? ParentFilterColumn { get; set; }

    /// <summary>Alternative column name to filter on when it differs from <see cref="Data"/>.</summary>
    [HtmlAttributeName("filter-column")] public string? FilterColumn { get; set; }

    /// <summary>Where this column's filter is placed (<c>Inline</c>, <c>Top</c>, <c>None</c>; case-insensitive).</summary>
    [HtmlAttributeName("filter-placement")] public string? FilterPlacement { get; set; }

    /// <summary>Render this column's filter disabled.</summary>
    [HtmlAttributeName("disabled")] public bool Disabled { get; set; }

    /// <summary>Preferred column width (e.g. <c>"120px"</c>).</summary>
    [HtmlAttributeName("width")] public string? Width { get; set; }

    /// <summary>CSS classes applied to data cells.</summary>
    [HtmlAttributeName("cell-class")] public string? CellClass { get; set; }

    /// <summary>CSS classes applied to the header cell.</summary>
    [HtmlAttributeName("header-class")] public string? HeaderClass { get; set; }

    /// <summary>Prevent wrapping for this column's cells.</summary>
    [HtmlAttributeName("no-wrap")] public bool NoWrap { get; set; }

    // --- Inline editor (editable tables) ---

    /// <summary>
    /// Makes this column editable in an editable table. The working editor types are <c>Text</c>,
    /// <c>Number</c>, <c>Date</c>, and <c>Select2Static</c> (supply <see cref="EditorStaticOptions"/>);
    /// case-insensitive.
    /// </summary>
    [HtmlAttributeName("editor")] public string? Editor { get; set; }

    /// <summary>Comma-separated options for a <c>Select2Static</c> editor (id == text).</summary>
    [HtmlAttributeName("editor-static-options")] public string? EditorStaticOptions { get; set; }

    /// <summary>AJAX options endpoint for a select editor.</summary>
    [HtmlAttributeName("editor-options-url")] public string? EditorOptionsUrl { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrEmpty(Data))
            throw new InvalidOperationException("<odt-column> requires a 'data' attribute.");

        if (context.Items.TryGetValue(typeof(DataTableTagHelper.ColumnBag), out var bag)
            && bag is List<DataTableColumnViewModel> columns)
        {
            var column = new DataTableColumnViewModel
            {
                Data = Data,
                Title = Title ?? Data,
                Filter = Filter,
                IsVisible = Visible,
                IsSortable = Sortable,
                IsDisabled = Disabled,
                NoWrap = NoWrap
            };

            if (Format is not null) column.Format = Format;
            if (OptionsUrl is not null) column.OptionsUrl = OptionsUrl;
            if (OptionValueField is not null) column.OptionValueField = OptionValueField;
            if (OptionTextField is not null) column.OptionTextField = OptionTextField;
            if (ParentFilterColumn is not null) column.ParentFilterColumn = ParentFilterColumn;
            if (FilterColumn is not null) column.FilterColumn = FilterColumn;
            if (TagHelperEnum.Parse<DataTableFilterPlacement>(FilterPlacement, "odt-column", "filter-placement") is { } placement)
                column.FilterPlacement = placement;
            if (Width is not null) column.Width = Width;
            if (CellClass is not null) column.CellClass = CellClass;
            if (HeaderClass is not null) column.HeaderClass = HeaderClass;

            if (!string.IsNullOrWhiteSpace(StaticOptions))
            {
                column.StaticOptions = StaticOptions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => new Select2ListItem { Id = value, Text = value })
                    .ToList();
            }

            columns.Add(column);
        }

        // An editor="…" turns this column into an inline editor — register it on the parent's editor bag
        // (which makes the grid editable, exactly like supplying EditorConfigs imperatively).
        if (TagHelperEnum.Parse<DataTableEditorType>(Editor, "odt-column", "editor") is { } editorType
            && context.Items.TryGetValue(typeof(DataTableTagHelper.EditorBag), out var editorBag)
            && editorBag is List<DataTableColumnEditorConfig> editors)
        {
            var editor = new DataTableColumnEditorConfig { Column = Data, EditorType = editorType };
            if (EditorOptionsUrl is not null) { editor.OptionsUrl = EditorOptionsUrl; editor.AjaxUrl = EditorOptionsUrl; }
            if (!string.IsNullOrWhiteSpace(EditorStaticOptions))
            {
                editor.StaticOptions = EditorStaticOptions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => new Select2ListItem { Id = value, Text = value })
                    .ToList();
            }
            editors.Add(editor);
        }

        output.SuppressOutput(); // the column is rendered by the parent's ViewComponent, not here
    }
}
