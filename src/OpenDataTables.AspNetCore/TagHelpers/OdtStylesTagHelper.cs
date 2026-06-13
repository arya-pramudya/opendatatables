using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace OpenDataTables.AspNetCore.TagHelpers;

/// <summary>
/// Emits the OpenDataTables stylesheet link (served from <c>_content/OpenDataTables.AspNetCore</c>).
/// Place in <c>&lt;head&gt;</c>. Usage: <c>&lt;odt-styles /&gt;</c>.
/// </summary>
[HtmlTargetElement("odt-styles", TagStructure = TagStructure.WithoutEndTag)]
public class OdtStylesTagHelper : TagHelper
{
    private const string CssPath = "/_content/OpenDataTables.AspNetCore/css/opendatatables.css";

    /// <summary>The current view context (used to honor the app's path base).</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        output.TagName = null;
        var pathBase = ViewContext.HttpContext.Request.PathBase.Value ?? string.Empty;
        output.Content.SetHtmlContent($"<link rel=\"stylesheet\" href=\"{pathBase}{CssPath}\" />");
    }
}
