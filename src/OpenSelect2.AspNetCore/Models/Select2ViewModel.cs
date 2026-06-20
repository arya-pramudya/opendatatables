namespace OpenSelect2.AspNetCore.Models;

/// <summary>
/// Configuration for an AJAX-driven Select2 dropdown rendered by the <c>Select2</c> ViewComponent.
/// </summary>
public class Select2ViewModel
{
    /// <summary>The HTML <c>id</c> of the select element (auto-generated if not set).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>The HTML <c>name</c> of the select element (required).</summary>
    public required string Name { get; set; }

    /// <summary>Optional label rendered above the control.</summary>
    public string? Label { get; set; }

    // --- Selection behavior ---

    /// <summary>Allow selecting multiple values.</summary>
    public bool IsMultiple { get; set; }

    /// <summary>Mark the field required (adds an asterisk to the label and the <c>required</c> attribute).</summary>
    public bool IsRequired { get; set; }

    /// <summary>Render the control disabled.</summary>
    public bool IsDisabled { get; set; }

    /// <summary>Render the control read-only (value preserved, but not changeable).</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Keep the control disabled even when its parent has a value. Overrides the normal
    /// parent/child enabling logic.
    /// </summary>
    public bool ForceDisabled { get; set; }

    /// <summary>Inject a synthetic "(Select All)" option as the first result on page 1.</summary>
    public bool CanSelectAll { get; set; }

    // --- Data source (local OR remote) ---

    /// <summary>
    /// Static options. When set (non-empty), the dropdown is rendered as a local list with no AJAX —
    /// <see cref="AjaxUrl"/> and the other AJAX-only fields are ignored. Leave null/empty to use
    /// <see cref="AjaxUrl"/> instead.
    /// </summary>
    public List<Select2ListItem>? Items { get; set; }

    /// <summary>The endpoint that returns <c>{ items, hasMore }</c>. Ignored when <see cref="Items"/> is set.</summary>
    public string AjaxUrl { get; set; } = "";

    /// <summary>Page size sent to the endpoint.</summary>
    public int Limit { get; set; } = 10;

    /// <summary>Placeholder text shown when nothing is selected.</summary>
    public string? Placeholder { get; set; } = "Select an option";

    /// <summary>The element <c>id</c> of the parent control. The child auto-disables until the parent has a value.</summary>
    public string? ParentId { get; set; }

    /// <summary>Static extra query parameters sent with every AJAX request (param name → literal value).</summary>
    public Dictionary<string, string>? ExtraParams { get; set; }

    /// <summary>
    /// Live DOM-sourced query parameters: param name → CSS selector whose current value is read and
    /// sent at request time.
    /// </summary>
    public Dictionary<string, string>? ExtraParamsHTML { get; set; }

    /// <summary>
    /// Enable child dropdowns even when this (parent) dropdown is disabled but has a pre-selected value.
    /// </summary>
    public bool EnableChildrenIfPreSelected { get; set; }

    // --- Selected items ---

    /// <summary>Items pre-selected on first render (also emitted as <c>&lt;option selected&gt;</c>).</summary>
    public List<Select2ListItem>? SelectedItems { get; set; }

    /// <summary>
    /// Opaque key handed to the host's <see cref="Abstractions.ISelect2Preselector"/> (if registered)
    /// to populate <see cref="SelectedItems"/> / disabled state from server-side context (e.g. the
    /// current user). Null means no preselection hook runs. Replaces the old domain-specific
    /// <c>PreSelectCurrentUser*</c> flags.
    /// </summary>
    public string? PreselectKey { get; set; }

    /// <summary>Extra CSS classes appended to the select element.</summary>
    public string? CssClass { get; set; }

    /// <summary>Additional raw HTML attributes to emit on the select element (e.g. <c>data-column</c>).</summary>
    public Dictionary<string, string>? ExtraAttributes { get; set; }

    /// <summary>
    /// Raw select2 options merged last over the computed settings (escape hatch for any option the C#
    /// model does not yet expose). Nested objects are deep-merged; array values replace the computed
    /// array wholesale rather than merging element-by-element. Function-valued options (e.g.
    /// <c>templateResult</c>) must be registered via
    /// <c>OpenSelect2.on(id, { templateResult, templateSelection, beforeInit })</c> from host JS.
    /// </summary>
    public Dictionary<string, object?>? Select2Options { get; set; }

    /// <summary>
    /// Returns a copy safe for the render pipeline (TagHelper attribute overrides, auto <see cref="Id"/>,
    /// <see cref="Abstractions.ISelect2Preselector"/>) to mutate without affecting a caller-supplied
    /// template that may be reused across requests or rendered more than once on a page. Scalars are copied;
    /// the mutable item lists are copied (element instances are shared).
    /// </summary>
    internal Select2ViewModel CloneForRender()
    {
        var copy = (Select2ViewModel)MemberwiseClone();
        if (Items is not null) copy.Items = new List<Select2ListItem>(Items);
        if (SelectedItems is not null) copy.SelectedItems = new List<Select2ListItem>(SelectedItems);
        return copy;
    }
}
