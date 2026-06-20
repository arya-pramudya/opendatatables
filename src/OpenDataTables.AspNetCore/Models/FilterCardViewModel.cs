namespace OpenDataTables.AspNetCore.Models;

/// <summary>Configuration for the FilterCard component.</summary>
public class FilterCardViewModel
{
    /// <summary>The HTML id of the filter form.</summary>
    public string FormId { get; set; } = "filter-form";

    /// <summary>The card title.</summary>
    public string Title { get; set; } = "Filter Data";

    /// <summary>The columns to render filters for.</summary>
    public List<DataTableColumnViewModel> Columns { get; set; } = new();

    /// <summary>Number of filter columns per row.</summary>
    public int ColumnsPerRow { get; set; } = 3;

    /// <summary>The id of the associated table.</summary>
    public string TableId { get; set; } = "dataTable";

    /// <summary>Custom Bootstrap grid width class; overrides the automatic calculation.</summary>
    public string CustomColumnWidthClass { get; set; } = "";

    /// <summary>Custom parameters included in AJAX requests (outside filter fields).</summary>
    public Dictionary<string, string>? CustomParameters { get; set; }

    /// <summary>
    /// The columns that get a filter input. A column only needs a <see cref="DataTableColumnViewModel.Filter"/>
    /// type — grid visibility is irrelevant, so a column hidden in the table (<c>IsVisible = false</c>) can
    /// still be filtered (e.g. a hidden foreign-key column).
    /// </summary>
    public List<DataTableColumnViewModel> GetFilterableColumns() =>
        Columns.Where(c => c.Filter != DataTableFilterType.None).ToList();
}
