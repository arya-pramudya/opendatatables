namespace OpenDataTables.AspNetCore.Models;

/// <summary>
/// What an editable DataTable does when the user changes page (or page size) while there are unsaved inline
/// edits on the current page. Server-side paging redraws the table, which would otherwise discard those
/// edits silently. Only applies to editable grids in <see cref="EditableTableSaveMode.Manual"/> or
/// <see cref="EditableTableSaveMode.Custom"/> mode (<see cref="EditableTableSaveMode.Auto"/> already
/// persists each change as it happens).
/// </summary>
public enum UnsavedEditBehavior
{
    /// <summary>Warn the user (a confirm dialog) and let them discard the edits or stay and keep editing.</summary>
    Warn,

    /// <summary>Persist the current page's edits (Manual → the save endpoint; Custom → the <c>OnSave</c> handler) before navigating.</summary>
    AutoSave,

    /// <summary>Do nothing — the unsaved edits on the current page are discarded by the redraw (legacy behavior).</summary>
    None
}
