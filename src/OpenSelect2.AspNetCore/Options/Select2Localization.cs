namespace OpenSelect2.AspNetCore;

/// <summary>
/// Localizable client-facing strings for the Select2 component. Flowed to the browser via
/// <c>window.OpenSelect2.config.locale</c>.
/// </summary>
public sealed class Select2Localization
{
    /// <summary>Title of the "session expired" dialog shown on an HTTP 401 response.</summary>
    public string SessionExpiredTitle { get; set; } = "Warning";

    /// <summary>Body of the "session expired" dialog shown on an HTTP 401 response.</summary>
    public string SessionExpiredMessage { get; set; } = "Your session has expired. Please log in again.";

    /// <summary>Text of the synthetic "Select All" option when <c>CanSelectAll</c> is enabled.</summary>
    public string SelectAllText { get; set; } = "(Select All)";

    /// <summary>Title of the generic error dialog.</summary>
    public string ErrorTitle { get; set; } = "Error";

    /// <summary>English defaults.</summary>
    public static Select2Localization English => new();

    /// <summary>Indonesian strings (matches the original PBBNetCore wording).</summary>
    public static Select2Localization Indonesian => new()
    {
        SessionExpiredTitle = "Peringatan",
        SessionExpiredMessage = "Session anda telah habis! Silahkan login ulang.",
        SelectAllText = "(Pilih Semua)",
        ErrorTitle = "Error",
    };
}
