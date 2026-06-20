namespace OpenDataTables.AspNetCore;

/// <summary>
/// Localizable client-facing strings for the DataTable component. Flowed to the browser via
/// <c>window.OpenDataTables.config.locale</c>.
/// </summary>
public sealed class DataTableLocalization
{
    /// <summary>Add button label.</summary>
    public string Add { get; set; } = "Add";

    /// <summary>Edit button label.</summary>
    public string Edit { get; set; } = "Edit";

    /// <summary>Delete button label.</summary>
    public string Delete { get; set; } = "Delete";

    /// <summary>View button label.</summary>
    public string View { get; set; } = "View";

    /// <summary>Search button label.</summary>
    public string Search { get; set; } = "Search";

    /// <summary>Reset-filters button label.</summary>
    public string ResetFilters { get; set; } = "Reset Filters";

    /// <summary>Actions column header.</summary>
    public string Actions { get; set; } = "Actions";

    /// <summary>Filter card title.</summary>
    public string FilterTitle { get; set; } = "Filter Data";

    /// <summary>Title of the "session expired" dialog on HTTP 401.</summary>
    public string SessionExpiredTitle { get; set; } = "Warning";

    /// <summary>Body of the "session expired" dialog on HTTP 401.</summary>
    public string SessionExpiredMessage { get; set; } = "Your session has expired. Please log in again.";

    /// <summary>Title of the unsaved-edits dialog shown when changing page mid-edit.</summary>
    public string UnsavedChangesTitle { get; set; } = "Unsaved changes";

    /// <summary>Body of the unsaved-edits dialog shown when changing page mid-edit.</summary>
    public string UnsavedChangesMessage { get; set; } = "You have unsaved changes on this page. Discard them and continue?";

    /// <summary>Confirm (discard) button label in the unsaved-edits dialog.</summary>
    public string DiscardChanges { get; set; } = "Discard";

    /// <summary>Cancel (keep editing) button label in the unsaved-edits dialog.</summary>
    public string KeepEditing { get; set; } = "Keep editing";

    /// <summary>English defaults.</summary>
    public static DataTableLocalization English => new();

    /// <summary>Indonesian strings (matches the original PBBNetCore wording).</summary>
    public static DataTableLocalization Indonesian => new()
    {
        Add = "Tambah",
        Edit = "Ubah",
        Delete = "Hapus",
        View = "Lihat",
        Search = "Cari",
        ResetFilters = "Reset Filter",
        Actions = "Aksi",
        FilterTitle = "Filter Data",
        SessionExpiredTitle = "Peringatan",
        SessionExpiredMessage = "Session anda telah habis! Silahkan login ulang.",
        UnsavedChangesTitle = "Perubahan belum disimpan",
        UnsavedChangesMessage = "Ada perubahan yang belum disimpan di halaman ini. Buang dan lanjutkan?",
        DiscardChanges = "Buang",
        KeepEditing = "Lanjut mengubah",
    };
}
