using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace OpenSelect2.AspNetCore.TagHelpers;

/// <summary>
/// Emits the OpenSelect2 client runtime: an inline <c>window.OpenSelect2.config</c> (login URL,
/// localization, delay) followed by the <c>openselect2(.min).js</c> script tag served from
/// <c>_content/OpenSelect2.AspNetCore</c>. Place once near the end of <c>&lt;body&gt;</c>, after jQuery
/// and select2.
/// </summary>
/// <remarks>Usage: <c>&lt;os2-scripts /&gt;</c> (add <c>@addTagHelper *, OpenSelect2.AspNetCore</c>).</remarks>
[HtmlTargetElement("os2-scripts", TagStructure = TagStructure.WithoutEndTag)]
public class Os2ScriptsTagHelper : TagHelper
{
    private const string BasePath = "/_content/OpenSelect2.AspNetCore/js/openselect2";

    // JsonSerializerDefaults.Web → camelCase + the default HTML-escaping encoder, which turns
    // '<' into '<' and so is safe to embed inside an executable <script> block.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenSelect2Options _options;

    /// <summary>Creates the tag helper.</summary>
    public Os2ScriptsTagHelper(IOptions<OpenSelect2Options> options) => _options = options.Value;

    /// <summary>Reference the minified asset (default <c>true</c>); set <c>false</c> to load the readable build.</summary>
    [HtmlAttributeName("minified")]
    public bool Minified { get; set; } = true;

    /// <summary>The current view context (used to honor the app's path base).</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        output.TagName = null; // do not render the <os2-scripts> wrapper element itself

        var config = new
        {
            loginUrl = _options.LoginUrl,
            ajaxDelayMs = _options.AjaxDelayMs,
            defaultLimit = _options.DefaultLimit,
            locale = new
            {
                sessionExpiredTitle = _options.Localization.SessionExpiredTitle,
                sessionExpiredMessage = _options.Localization.SessionExpiredMessage,
                selectAllText = _options.Localization.SelectAllText,
                errorTitle = _options.Localization.ErrorTitle
            }
        };

        var json = JsonSerializer.Serialize(config, JsonOptions);
        var pathBase = ViewContext.HttpContext.Request.PathBase.Value ?? string.Empty;
        var src = pathBase + BasePath + (Minified ? ".min.js" : ".js");

        output.Content.SetHtmlContent(
            "<script>window.OpenSelect2=window.OpenSelect2||{};window.OpenSelect2.config=" + json + ";</script>" +
            "<script src=\"" + src + "\"></script>");
    }
}
