namespace OpenDataTables.AspNetCore.Models;

/// <summary>Specifies where a filter should be placed for a DataTable column.</summary>
public enum DataTableFilterPlacement
{
    /// <summary>Filter appears inline under the column header.</summary>
    Inline,

    /// <summary>Filter appears in the top filter area above the table.</summary>
    Top,

    /// <summary>No filter is displayed for this column.</summary>
    None
}
