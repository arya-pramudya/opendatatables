namespace OpenSelect2.AspNetCore;

/// <summary>
/// Options for OpenSelect2, configured via <c>AddOpenSelect2(...)</c> and surfaced to the browser
/// through the <c>&lt;os2-scripts /&gt;</c> tag helper as <c>window.OpenSelect2.config</c>.
/// </summary>
public sealed class OpenSelect2Options
{
    /// <summary>Default page size used when a <c>Select2ViewModel.Limit</c> is not specified.</summary>
    public int DefaultLimit { get; set; } = 10;

    /// <summary>
    /// Where the client redirects after an HTTP 401 (session expired). When null the client falls
    /// back to reloading the current page.
    /// </summary>
    public string? LoginUrl { get; set; }

    /// <summary>Debounce (ms) applied to AJAX search requests.</summary>
    public int AjaxDelayMs { get; set; } = 250;

    /// <summary>Client-facing localized strings.</summary>
    public Select2Localization Localization { get; set; } = Select2Localization.English;
}
