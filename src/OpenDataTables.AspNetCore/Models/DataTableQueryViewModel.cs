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

    /// <summary>Index of the sorted column (primary sort, back-compat scalar).</summary>
    public int SortColumnIndex { get; set; }

    /// <summary>Data key of the sorted column (primary sort, back-compat scalar).</summary>
    public string SortColumnName { get; set; } = "";

    /// <summary>Sort direction (<c>asc</c>/<c>desc</c>) for the primary sort (back-compat scalar).</summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Full ordered sort list (B5 multi-column sort) and the canonical sort representation. After
    /// <see cref="NormalizeSort"/> runs (the sort/paging extensions call it), <c>SortOrders[0]</c> and the
    /// scalar <see cref="SortColumnName"/>/<see cref="SortDirection"/> always agree, so host code may read
    /// either; entries beyond the first are applied as <c>ThenBy</c>.
    /// </summary>
    public List<SortDescriptor> SortOrders { get; set; } = new();

    /// <summary>
    /// Reconciles the two sort representations into a single canonical one so neither the library nor host
    /// code has to guess which wins:
    /// <list type="bullet">
    /// <item>guarantees <see cref="SortOrders"/> is non-null;</item>
    /// <item>when <see cref="SortOrders"/> is non-empty it is canonical — the back-compat scalars
    /// (<see cref="SortColumnName"/>/<see cref="SortDirection"/>) are derived from <c>SortOrders[0]</c>;</item>
    /// <item>when only the scalars are set (an older client), a single matching <see cref="SortDescriptor"/>
    /// is synthesized so the multi-column path and host code see the same thing.</item>
    /// </list>
    /// Idempotent and safe to call more than once.
    /// </summary>
    public void NormalizeSort()
    {
        SortOrders ??= new();

        if (SortOrders.Count > 0)
        {
            var primary = SortOrders[0];
            if (!string.IsNullOrWhiteSpace(primary.Column)) SortColumnName = primary.Column;
            if (!string.IsNullOrWhiteSpace(primary.Direction)) SortDirection = primary.Direction;
        }
        else if (!string.IsNullOrWhiteSpace(SortColumnName))
        {
            SortOrders.Add(new SortDescriptor
            {
                Column = SortColumnName,
                Direction = string.IsNullOrWhiteSpace(SortDirection) ? "asc" : SortDirection
            });
        }
    }
}

/// <summary>A single column sort entry in a multi-column sort request.</summary>
public class SortDescriptor
{
    /// <summary>Data key of the column to sort.</summary>
    public string Column { get; set; } = "";

    /// <summary>Sort direction: <c>asc</c> or <c>desc</c>.</summary>
    public string Direction { get; set; } = "asc";
}
