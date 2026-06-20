using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using OpenSelect2.AspNetCore.Models;

namespace OpenSelect2.AspNetCore.TagHelpers;

/// <summary>
/// Declarative equivalent of <c>@await Component.InvokeAsync("Select2", model)</c>: renders an
/// AJAX-driven (or static) Select2 dropdown from tag attributes by delegating to the existing
/// <c>Select2</c> ViewComponent — so the emitted markup, JSON config block, and preselector hooks
/// are identical to the imperative form.
/// </summary>
/// <remarks>
/// Usage: <c>&lt;os2-select name="Category" ajax-url="/Lookup/Categories" /&gt;</c>. A static list can be
/// authored with nested <c>&lt;os2-item&gt;</c> children. For options the attributes do not surface
/// (e.g. raw <c>Select2Options</c>) pass a fully-built model via <c>config="@model"</c>; explicit
/// attributes then override the same property on that model. Requires
/// <c>@addTagHelper *, OpenSelect2.AspNetCore</c>.
/// </remarks>
[HtmlTargetElement("os2-select")]
public class Select2TagHelper : TagHelper
{
    /// <summary>
    /// Marker/payload type shared via <see cref="TagHelperContext.Items"/> so nested
    /// <c>&lt;os2-item&gt;</c> children can register their options on the parent.
    /// </summary>
    internal sealed class ItemBag : List<Select2ListItem> { }

    private readonly IViewComponentHelper _viewComponentHelper;

    /// <summary>Creates the tag helper.</summary>
    public Select2TagHelper(IViewComponentHelper viewComponentHelper)
        => _viewComponentHelper = viewComponentHelper;

    /// <summary>The current view context (required to contextualize the ViewComponent helper).</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    // --- Scalar attributes (nullable so "unset" is distinguishable from an explicit value) ---

    /// <summary>The HTML <c>name</c> (required unless supplied via <see cref="Config"/>).</summary>
    [HtmlAttributeName("name")] public string? Name { get; set; }

    /// <summary>The HTML <c>id</c> (auto-generated when omitted).</summary>
    [HtmlAttributeName("id")] public string? Id { get; set; }

    /// <summary>Optional label rendered above the control.</summary>
    [HtmlAttributeName("label")] public string? Label { get; set; }

    /// <summary>Allow selecting multiple values.</summary>
    [HtmlAttributeName("multiple")] public bool? Multiple { get; set; }

    /// <summary>Mark the field required.</summary>
    [HtmlAttributeName("required")] public bool? Required { get; set; }

    /// <summary>Render the control disabled.</summary>
    [HtmlAttributeName("disabled")] public bool? Disabled { get; set; }

    /// <summary>Render the control read-only (value preserved, not changeable).</summary>
    [HtmlAttributeName("readonly")] public bool? ReadOnly { get; set; }

    /// <summary>Keep the control disabled even when its parent has a value.</summary>
    [HtmlAttributeName("force-disabled")] public bool? ForceDisabled { get; set; }

    /// <summary>Inject a synthetic "(Select All)" option.</summary>
    [HtmlAttributeName("can-select-all")] public bool? CanSelectAll { get; set; }

    /// <summary>The endpoint returning <c>{ items, hasMore }</c> (ignored when nested items are present).</summary>
    [HtmlAttributeName("ajax-url")] public string? AjaxUrl { get; set; }

    /// <summary>AJAX page size.</summary>
    [HtmlAttributeName("limit")] public int? Limit { get; set; }

    /// <summary>Placeholder text shown when nothing is selected.</summary>
    [HtmlAttributeName("placeholder")] public string? Placeholder { get; set; }

    /// <summary>The element id of the parent control (cascade).</summary>
    [HtmlAttributeName("parent-id")] public string? ParentId { get; set; }

    /// <summary>Enable children even when this dropdown is disabled but pre-selected.</summary>
    [HtmlAttributeName("enable-children-if-preselected")] public bool? EnableChildrenIfPreSelected { get; set; }

    /// <summary>Opaque key handed to the host's <see cref="Abstractions.ISelect2Preselector"/>.</summary>
    [HtmlAttributeName("preselect-key")] public string? PreselectKey { get; set; }

    /// <summary>Extra CSS classes appended to the select element.</summary>
    [HtmlAttributeName("css-class")] public string? CssClass { get; set; }

    /// <summary>
    /// A fully-built model used as the starting point; explicit attributes and nested children override it.
    /// Use this escape hatch for options the attributes do not surface (e.g. <c>Select2Options</c>).
    /// </summary>
    [HtmlAttributeName("config")] public Select2ViewModel? Config { get; set; }

    // --- Dictionary-prefix attributes (e.g. extra-param-tenantId="5") ---

    /// <summary>Static query params: <c>extra-param-{name}="value"</c>.</summary>
    [HtmlAttributeName("extra-params", DictionaryAttributePrefix = "extra-param-")]
    public Dictionary<string, string> ExtraParams { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Live DOM-sourced query params: <c>extra-html-param-{name}="#selector"</c>. The prefix is
    /// deliberately NOT <c>extra-param-html-</c>: that is a superset of the <see cref="ExtraParams"/>
    /// prefix (<c>extra-param-</c>), so Razor would bind such attributes to <see cref="ExtraParams"/>
    /// (the first matching prefix) and this dictionary would never be populated.
    /// </summary>
    [HtmlAttributeName("extra-html-params", DictionaryAttributePrefix = "extra-html-param-")]
    public Dictionary<string, string> ExtraParamsHtml { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Raw HTML attributes on the select element: <c>extra-attr-{name}="value"</c>.</summary>
    [HtmlAttributeName("extra-attrs", DictionaryAttributePrefix = "extra-attr-")]
    public Dictionary<string, string> ExtraAttributes { get; set; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        // Seed a shared bag, then realize the child <os2-item> tags (they register into it).
        var items = new ItemBag();
        context.Items[typeof(ItemBag)] = items;
        await output.GetChildContentAsync();

        var model = BuildModel(items);

        ((IViewContextAware)_viewComponentHelper).Contextualize(ViewContext);
        var content = await _viewComponentHelper.InvokeAsync("Select2", model);

        output.TagName = null; // emit the component's HTML without the <os2-select> wrapper
        output.Content.SetHtmlContent(content);
    }

    private Select2ViewModel BuildModel(List<Select2ListItem> childItems)
    {
        Select2ViewModel model;
        if (Config is not null)
        {
            // Clone so attribute overrides and the downstream render pipeline never mutate the caller's
            // template (which may be a cached/shared model or rendered by more than one tag on a page).
            model = Config.CloneForRender();
            if (Name is not null) model.Name = Name;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException(
                    "<os2-select> requires a 'name' attribute (or a 'config' model that supplies Name).");
            model = new Select2ViewModel { Name = Name };
        }

        if (Id is not null) model.Id = Id;
        if (Label is not null) model.Label = Label;
        if (Multiple is { } multiple) model.IsMultiple = multiple;
        if (Required is { } required) model.IsRequired = required;
        if (Disabled is { } disabled) model.IsDisabled = disabled;
        if (ReadOnly is { } readOnly) model.IsReadOnly = readOnly;
        if (ForceDisabled is { } forceDisabled) model.ForceDisabled = forceDisabled;
        if (CanSelectAll is { } canSelectAll) model.CanSelectAll = canSelectAll;
        if (AjaxUrl is not null) model.AjaxUrl = AjaxUrl;
        if (Limit is { } limit) model.Limit = limit;
        if (Placeholder is not null) model.Placeholder = Placeholder;
        if (ParentId is not null) model.ParentId = ParentId;
        if (EnableChildrenIfPreSelected is { } enableChildren) model.EnableChildrenIfPreSelected = enableChildren;
        if (PreselectKey is not null) model.PreselectKey = PreselectKey;
        if (CssClass is not null) model.CssClass = CssClass;

        if (ExtraParams.Count > 0) model.ExtraParams = Merge(model.ExtraParams, ExtraParams);
        if (ExtraParamsHtml.Count > 0) model.ExtraParamsHTML = Merge(model.ExtraParamsHTML, ExtraParamsHtml);
        if (ExtraAttributes.Count > 0) model.ExtraAttributes = Merge(model.ExtraAttributes, ExtraAttributes);

        // Nested <os2-item> children define a static option list (replaces any model Items).
        if (childItems.Count > 0) model.Items = childItems;

        return model;
    }

    private static Dictionary<string, string> Merge(Dictionary<string, string>? existing, Dictionary<string, string> additions)
    {
        var result = existing is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(existing);
        foreach (var kv in additions)
            result[kv.Key] = kv.Value;
        return result;
    }
}
