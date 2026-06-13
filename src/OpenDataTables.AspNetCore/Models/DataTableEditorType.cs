namespace OpenDataTables.AspNetCore.Models;

/// <summary>The editor control used for an editable DataTable column.</summary>
public enum DataTableEditorType
{
    /// <summary>Free-form text input.</summary>
    Text = 0,

    /// <summary>Numeric input.</summary>
    Number = 1,

    /// <summary>Date input.</summary>
    Date = 2,

    /// <summary>Select2 with static options.</summary>
    Select2Static = 3,

    /// <summary>Select2 with AJAX-loaded options.</summary>
    Select2Dynamic = 4,

    /// <summary>Select2 backed by a lookup table.</summary>
    Select2Table = 5
}
