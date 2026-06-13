namespace OpenDataTables.AspNetCore.Models;

/// <summary>Configures how filters are displayed for a DataTable.</summary>
public enum DataTableFilterUiMode
{
    /// <summary>Do not render any filter UI.</summary>
    None,

    /// <summary>Render the FilterCard component above the table.</summary>
    FilterCard,

    /// <summary>Render inline filters under each column header in the table head.</summary>
    Inline,

    /// <summary>Render filters as simple controls directly above the table (no card wrapper).</summary>
    Top,

    /// <summary>Use per-column <see cref="DataTableColumnViewModel.FilterPlacement"/> to position each filter (mix top + inline).</summary>
    Mixed
}
