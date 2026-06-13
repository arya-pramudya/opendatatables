namespace OpenDataTables.AspNetCore.Models;

/// <summary>
/// The AJAX response contract for a DataTable. Lower-case property names are intentional — they are part
/// of the DataTables JS wire contract and must not be renamed.
/// </summary>
/// <typeparam name="T">The row view-model type.</typeparam>
#pragma warning disable IDE1006, CA1707 // wire-contract names are lower-case by design
public class DataTableResponseViewModel<T>
{
    /// <summary>Echo of the request <c>draw</c> counter.</summary>
    public string draw { get; set; } = "0";

    /// <summary>Total records before filtering.</summary>
    public int recordsTotal { get; set; }

    /// <summary>Total records after filtering.</summary>
    public int recordsFiltered { get; set; }

    /// <summary>The page of rows.</summary>
    public List<T>? data { get; set; }

    /// <summary>Optional per-group counts (grouped tables).</summary>
    public Dictionary<string, int>? groupCounts { get; set; }

    /// <summary>Optional grand-total sum (footer aggregates).</summary>
    public double grandTotalSum { get; set; }
}
#pragma warning restore IDE1006, CA1707
