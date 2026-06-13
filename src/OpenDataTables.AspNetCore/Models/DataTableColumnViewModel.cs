using OpenSelect2.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Models;

/// <summary>Configuration for an individual column in a DataTable.</summary>
public class DataTableColumnViewModel
{
    /// <summary>The property name in the data source to bind to this column.</summary>
    public required string Data { get; set; }

    /// <summary>The column header text.</summary>
    public required string Title { get; set; }

    /// <summary>Whether the column is visible in the table.</summary>
    public bool IsVisible { get; set; } = true;

    // --- Filtering ---

    /// <summary>The type of filter input for this column (text, select, none, …).</summary>
    public DataTableFilterType Filter { get; set; } = DataTableFilterType.None;

    /// <summary>Optional display format string (e.g. <c>"DD MMM YYYY"</c> for dates).</summary>
    public string? Format { get; set; }

    // --- Options for select filters ---

    /// <summary>Static options for select dropdown filters (simple string list).</summary>
    public List<string>? Options { get; set; }

    /// <summary>URL for AJAX-loaded options for select dropdowns.</summary>
    public string OptionsUrl { get; set; } = "";

    /// <summary>Field name for the value in AJAX option results.</summary>
    public string OptionValueField { get; set; } = "value";

    /// <summary>Field name for the display text in AJAX option results.</summary>
    public string OptionTextField { get; set; } = "text";

    /// <summary>Extra parameters to send with the AJAX request for options.</summary>
    public Dictionary<string, string>? OptionsUrlParameters { get; set; } = new();

    /// <summary>Data property name of the parent column to cascade this filter on.</summary>
    public string? ParentFilterColumn { get; set; }

    /// <summary>Custom Bootstrap grid width class (e.g. <c>"col-md-6"</c>); overrides the automatic calculation.</summary>
    public string CustomColumnWidthClass { get; set; } = "";

    /// <summary>Explicit ordering for this column within the filter UI.</summary>
    public int? FilterIndex { get; set; }

    /// <summary>Explicit ordering for this column within the table head.</summary>
    public int? TableIndex { get; set; }

    /// <summary>Pre-selected items for this column's select filter.</summary>
    public List<Select2ListItem>? SelectedItems { get; set; }

    /// <summary>Render this column's filter disabled.</summary>
    public bool IsDisabled { get; set; }

    /// <summary>Whether this column is sortable.</summary>
    public bool IsSortable { get; set; } = true;

    /// <summary>Static options for a <see cref="DataTableFilterType.SelectStatic"/> filter.</summary>
    public List<Select2ListItem>? StaticOptions { get; set; }

    /// <summary>Alternative column name to filter on when it differs from <see cref="Data"/> (e.g. display <c>statusText</c>, filter <c>status</c>).</summary>
    public string? FilterColumn { get; set; }

    /// <summary>Where this column's filter is placed. Default <see cref="DataTableFilterPlacement.Inline"/> for back-compat.</summary>
    public DataTableFilterPlacement FilterPlacement { get; set; } = DataTableFilterPlacement.Inline;

    // --- Styling / CSS customization ---

    /// <summary>CSS classes applied to the header cell (<c>th</c>).</summary>
    public string HeaderClass { get; set; } = "";

    /// <summary>Inline style for the header cell (<c>th</c>).</summary>
    public string HeaderStyle { get; set; } = "";

    /// <summary>CSS classes applied to data cells (<c>td</c>).</summary>
    public string CellClass { get; set; } = "";

    /// <summary>Inline style for data cells (<c>td</c>).</summary>
    public string CellStyle { get; set; } = "";

    /// <summary>Preferred column width (e.g. <c>"120px"</c> or <c>"12%"</c>).</summary>
    public string Width { get; set; } = "";

    /// <summary>Prevent wrapping for this column's cells.</summary>
    public bool NoWrap { get; set; }

    // --- Filter input styling ---

    /// <summary>Extra CSS classes for the inline filter input.</summary>
    public string FilterClass { get; set; } = "";

    /// <summary>Inline style for the inline filter input.</summary>
    public string FilterStyle { get; set; } = "";

    /// <summary>Extra CSS classes for the top filter input.</summary>
    public string FilterTopClass { get; set; } = "";

    /// <summary>Inline style for the top filter input.</summary>
    public string FilterTopStyle { get; set; } = "";
}
