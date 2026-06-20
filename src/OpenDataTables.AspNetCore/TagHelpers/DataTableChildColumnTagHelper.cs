using Microsoft.AspNetCore.Razor.TagHelpers;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.TagHelpers;

/// <summary>
/// A single column of the child grid shown when an <c>&lt;odt-table&gt;</c> row is expanded
/// (table-in-a-table). Declaring any <c>&lt;odt-child-column&gt;</c> turns on child rows. Renders nothing
/// itself — it registers the column on the parent via <see cref="TagHelperContext.Items"/>.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;odt-table ajax-url="/Categories/GetData" child-ajax-url="/Products/GetData" child-param-category="row.id"&gt;
///     &lt;odt-column data="name" title="Category" /&gt;
///     &lt;odt-child-column data="name" title="Product" /&gt;
///     &lt;odt-child-column data="price" title="Price" /&gt;
/// &lt;/odt-table&gt;
/// </code>
/// Child cells honor the date-token <c>format</c> only (named formatters apply to the parent grid).
/// </remarks>
[HtmlTargetElement("odt-child-column", ParentTag = "odt-table")]
public class DataTableChildColumnTagHelper : TagHelper
{
    /// <summary>The data-source property bound to this child column (required).</summary>
    [HtmlAttributeName("data")] public string? Data { get; set; }

    /// <summary>The column header text (defaults to <see cref="Data"/> when omitted).</summary>
    [HtmlAttributeName("title")] public string? Title { get; set; }

    /// <summary>Optional moment-style date token (e.g. <c>"DD MMM YYYY"</c>) applied when the value is a date.</summary>
    [HtmlAttributeName("format")] public string? Format { get; set; }

    /// <summary>Whether the child column is sortable.</summary>
    [HtmlAttributeName("sortable")] public bool Sortable { get; set; } = true;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrEmpty(Data))
            throw new InvalidOperationException("<odt-child-column> requires a 'data' attribute.");

        if (context.Items.TryGetValue(typeof(DataTableTagHelper.ChildColumnBag), out var bag)
            && bag is List<DataTableColumnViewModel> childColumns)
        {
            childColumns.Add(new DataTableColumnViewModel
            {
                Data = Data,
                Title = Title ?? Data,
                Format = Format,
                IsSortable = Sortable
            });
        }

        output.SuppressOutput(); // the child grid is rendered client-side from this config, not here
    }
}
