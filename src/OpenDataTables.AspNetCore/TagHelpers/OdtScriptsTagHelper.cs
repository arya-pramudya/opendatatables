using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace OpenDataTables.AspNetCore.TagHelpers;

/// <summary>
/// Emits the OpenDataTables client runtime: an inline <c>window.OpenDataTables.config</c> followed by the
/// component scripts (core → datatable → filtercard → init), served from
/// <c>_content/OpenDataTables.AspNetCore</c>. Place once near the end of <c>&lt;body&gt;</c>, after jQuery
/// and datatables.net. Select filters additionally require <c>&lt;os2-scripts /&gt;</c> (OpenSelect2).
/// </summary>
/// <remarks>Usage: <c>&lt;odt-scripts /&gt;</c> (add <c>@addTagHelper *, OpenDataTables.AspNetCore</c>).</remarks>
[HtmlTargetElement("odt-scripts", TagStructure = TagStructure.WithoutEndTag)]
public class OdtScriptsTagHelper : TagHelper
{
    private const string Base = "/_content/OpenDataTables.AspNetCore/js/";

    private static readonly string[] Files =
    {
        "opendatatables-core",
        "opendatatables-datatable",
        "opendatatables-filtercard",
        "opendatatables-init"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenDataTablesOptions _options;

    /// <summary>Creates the tag helper.</summary>
    public OdtScriptsTagHelper(IOptions<OpenDataTablesOptions> options) => _options = options.Value;

    /// <summary>Reference the minified assets (default <c>true</c>).</summary>
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
        output.TagName = null;

        var loc = _options.Localization;
        var config = new
        {
            loginUrl = _options.LoginUrl,
            pageLength = _options.DefaultPageLength,
            reinitEvents = _options.ReinitEvents,
            locale = new
            {
                add = loc.Add,
                edit = loc.Edit,
                delete = loc.Delete,
                view = loc.View,
                search = loc.Search,
                resetFilters = loc.ResetFilters,
                actions = loc.Actions,
                filterTitle = loc.FilterTitle,
                sessionExpiredTitle = loc.SessionExpiredTitle,
                sessionExpiredMessage = loc.SessionExpiredMessage
            }
        };

        var json = JsonSerializer.Serialize(config, JsonOptions);
        var pathBase = ViewContext.HttpContext.Request.PathBase.Value ?? string.Empty;
        var ext = Minified ? ".min.js" : ".js";

        var sb = new StringBuilder();
        sb.Append("<script>window.OpenDataTables=window.OpenDataTables||{};window.OpenDataTables.config=").Append(json).Append(";</script>");
        foreach (var file in Files)
            sb.Append("<script src=\"").Append(pathBase).Append(Base).Append(file).Append(ext).Append("\"></script>");

        output.Content.SetHtmlContent(sb.ToString());
    }
}
