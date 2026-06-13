namespace OpenDataTables.AspNetCore.Models;

/// <summary>
/// The AJAX request contract sent by the DataTable client. Property names are intentionally stable —
/// they are part of the JS ⇄ server wire contract. Per-filter values and custom parameters arrive as
/// additional bound action parameters.
/// </summary>
public class DataTableQueryViewModel
{
    /// <summary>DataTables draw counter (echoed back unchanged).</summary>
    public string Draw { get; set; } = "0";

    /// <summary>Zero-based index of the first record to return.</summary>
    public int Start { get; set; }

    /// <summary>Number of records to return (page size).</summary>
    public int Length { get; set; } = 10;

    /// <summary>Index of the sorted column.</summary>
    public int SortColumnIndex { get; set; }

    /// <summary>Data key of the sorted column.</summary>
    public string SortColumnName { get; set; } = "";

    /// <summary>Sort direction (<c>asc</c>/<c>desc</c>).</summary>
    public string SortDirection { get; set; } = "asc";
}
