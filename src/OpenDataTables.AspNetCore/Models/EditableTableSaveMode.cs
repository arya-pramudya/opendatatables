namespace OpenDataTables.AspNetCore.Models;

/// <summary>How an editable DataTable persists changes.</summary>
public enum EditableTableSaveMode
{
    /// <summary>Custom save logic supplied by the host.</summary>
    Custom,

    /// <summary>Show Save/Cancel buttons; persist on Save.</summary>
    Manual,

    /// <summary>Persist automatically on each change.</summary>
    Auto
}
