using Microsoft.AspNetCore.Razor.TagHelpers;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.TagHelpers;

/// <summary>
/// A custom toolbar or per-row button inside an <c>&lt;odt-table&gt;</c>. Renders nothing itself — it
/// registers a <see cref="DataTableButtonViewModel"/> on the parent via <see cref="TagHelperContext.Items"/>.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;odt-table ajax-url="/Products/GetData"&gt;
///     &lt;odt-button text="Export" icon="fas fa-file-csv" css-class="btn-outline-success"
///                 placement="top" on-click="productExport" /&gt;
/// &lt;/odt-table&gt;
/// </code>
/// For a row button (<c>placement="row"</c>) the handler receives the row data; use <c>ROW_ID</c> in
/// <c>on-click</c> as the row-id placeholder. Register the handler from host JS.
/// </remarks>
[HtmlTargetElement("odt-button", ParentTag = "odt-table")]
public class DataTableButtonTagHelper : TagHelper
{
    /// <summary>Button text (required).</summary>
    [HtmlAttributeName("text")] public string? Text { get; set; }

    /// <summary>JS handler name invoked on click (required).</summary>
    [HtmlAttributeName("on-click")] public string? OnClick { get; set; }

    /// <summary>Optional icon class (e.g. <c>"fas fa-file-csv"</c>).</summary>
    [HtmlAttributeName("icon")] public string? Icon { get; set; }

    /// <summary>CSS class(es) for the button (default <c>btn-secondary</c>).</summary>
    [HtmlAttributeName("css-class")] public string? CssClass { get; set; }

    /// <summary>Placement: <c>top</c> (above the table) or <c>row</c> (in the action cell, default).</summary>
    [HtmlAttributeName("placement")] public string? Placement { get; set; }

    /// <summary>Optional explicit button id (auto-generated when omitted).</summary>
    [HtmlAttributeName("id")] public string? Id { get; set; }

    /// <summary>Optional inline style.</summary>
    [HtmlAttributeName("style")] public string? Style { get; set; }

    /// <summary>Optional tooltip title.</summary>
    [HtmlAttributeName("title")] public string? Title { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrEmpty(Text))
            throw new InvalidOperationException("<odt-button> requires a 'text' attribute.");
        if (string.IsNullOrEmpty(OnClick))
            throw new InvalidOperationException("<odt-button> requires an 'on-click' attribute.");

        if (context.Items.TryGetValue(typeof(DataTableTagHelper.ButtonBag), out var bag)
            && bag is List<DataTableButtonViewModel> buttons)
        {
            var button = new DataTableButtonViewModel { Text = Text, OnClick = OnClick };
            if (Id is not null) button.Id = Id;
            if (Icon is not null) button.Icon = Icon;
            if (CssClass is not null) button.CssClass = CssClass;
            if (Placement is not null) button.Placement = Placement;
            if (Style is not null) button.Style = Style;
            if (Title is not null) button.Title = Title;
            buttons.Add(button);
        }

        output.SuppressOutput(); // the button is rendered by the parent's ViewComponent, not here
    }
}
