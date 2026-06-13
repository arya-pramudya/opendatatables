namespace OpenDataTables.AspNetCore;

/// <summary>
/// Options for OpenDataTables, configured via <c>AddOpenDataTables(...)</c> and surfaced to the browser
/// through the <c>&lt;odt-scripts /&gt;</c> tag helper as <c>window.OpenDataTables.config</c>.
/// </summary>
public sealed class OpenDataTablesOptions
{
    /// <summary>Default page length applied when a model does not specify one.</summary>
    public int DefaultPageLength { get; set; } = 50;

    /// <summary>Where the client redirects after an HTTP 401. Null falls back to reloading the page.</summary>
    public string? LoginUrl { get; set; }

    /// <summary>Extra DOM event names (besides <c>htmx:afterSwap</c>) that should re-scan for tables.</summary>
    public IList<string> ReinitEvents { get; } = new List<string>();

    /// <summary>Client-facing localized strings.</summary>
    public DataTableLocalization Localization { get; set; } = DataTableLocalization.English;
}
