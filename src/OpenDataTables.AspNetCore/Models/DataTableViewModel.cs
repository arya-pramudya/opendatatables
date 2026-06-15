namespace OpenDataTables.AspNetCore.Models;

/// <summary>Configuration for a server-side DataTable rendered by the <c>DataTable</c> ViewComponent.</summary>
public class DataTableViewModel
{
    /// <summary>Optional JS function name invoked for each row (DataTables <c>rowCallback</c>).</summary>
    public string? RowCallback { get; set; }

    // --- Action callbacks (JS function names; invoked via delegated events, CSP-friendly) ---

    /// <summary>JS function name called when the Add button is clicked. Signature: <c>function()</c>.</summary>
    public string? OnAdd { get; set; }

    /// <summary>JS function name called when a row's View button is clicked. Signature: <c>function(id)</c>.</summary>
    public string? OnView { get; set; }

    /// <summary>JS function name called when a row's Edit button is clicked. Signature: <c>function(id)</c>.</summary>
    public string? OnEdit { get; set; }

    /// <summary>JS function name called when a row's Delete button is clicked. Signature: <c>function(id)</c>.</summary>
    public string? OnDelete { get; set; }

    /// <summary>JS function name called to persist edits when <see cref="SaveMode"/> is <see cref="EditableTableSaveMode.Custom"/>. Signature: <c>function(rows)</c>.</summary>
    public string? OnSave { get; set; }

    /// <summary>Override text for the Add button.</summary>
    public string? CustomAddText { get; set; }

    /// <summary>Override text for the Edit button.</summary>
    public string? CustomEditText { get; set; }

    /// <summary>Override text for the Delete button.</summary>
    public string? CustomDeleteText { get; set; }

    /// <summary>Custom buttons (top-of-table or per-row).</summary>
    public List<DataTableButtonViewModel>? CustomButtons { get; set; }

    /// <summary>The HTML id of the table (auto-generated if blank).</summary>
    public string TableId { get; set; } = "dataTable";

    /// <summary>The AJAX endpoint returning the <see cref="DataTableResponseViewModel{T}"/> payload.</summary>
    public required string AjaxUrl { get; set; }

    /// <summary>Optional save endpoint (editable tables).</summary>
    public string? SaveAjaxUrl { get; set; }

    /// <summary>The column definitions.</summary>
    public List<DataTableColumnViewModel> Columns { get; set; } = new();

    /// <summary>Default sort column (data key). When the implicit <c>id</c> column is the initial sort, this takes over.</summary>
    public string? DefaultSortColumn { get; set; }

    /// <summary>Default sort direction (<c>asc</c>/<c>desc</c>).</summary>
    public string? DefaultSortDirection { get; set; }

    /// <summary>Resolved per-column filter configs (auto-built from <see cref="Columns"/> when empty).</summary>
    public List<DataTableColumnFilterConfig> FilterConfigs { get; set; } = new();

    /// <summary>Per-column editor configs (editable tables).</summary>
    public List<DataTableColumnEditorConfig> EditorConfigs { get; set; } = new();

    // --- Action button visibility ---

    /// <summary>Show the Add button.</summary>
    public bool ShowAdd { get; set; } = true;

    /// <summary>Show the per-row Edit button.</summary>
    public bool ShowEdit { get; set; } = true;

    /// <summary>Show the per-row Delete button.</summary>
    public bool ShowDelete { get; set; } = true;

    /// <summary>Show the per-row View button.</summary>
    public bool ShowView { get; set; } = true;

    // --- Custom parameters and dynamic loading ---

    /// <summary>Static extra parameters sent with every data request.</summary>
    public Dictionary<string, string>? CustomParameters { get; set; }

    /// <summary>
    /// Selectors for dynamic value sources. Format: <c>{ "paramName": "#element-id" }</c> or
    /// <c>{ "paramName": "#tableId|rowData.columnName" }</c>.
    /// </summary>
    public Dictionary<string, string>? DynamicValueSources { get; set; }

    /// <summary>When to load the DataTable data (default <see cref="DataTableLoadTriggerType.Immediate"/>).</summary>
    public DataTableLoadTriggerType LoadTrigger { get; set; } = DataTableLoadTriggerType.Immediate;

    /// <summary>Selector for the element that triggers loading when <see cref="LoadTrigger"/> is not immediate.</summary>
    public string? TriggerSelector { get; set; }

    /// <summary>Event name for a custom trigger when <see cref="LoadTrigger"/> is <see cref="DataTableLoadTriggerType.Custom"/>.</summary>
    public string? TriggerEvent { get; set; }

    /// <summary>Render a leading row-number column.</summary>
    public bool HasNumbering { get; set; }

    /// <summary>Save mode for editable tables.</summary>
    public EditableTableSaveMode SaveMode { get; set; } = EditableTableSaveMode.Manual;

    /// <summary>How filters are displayed (FilterCard default, Inline, Top, Mixed, or None).</summary>
    public DataTableFilterUiMode FilterUiMode { get; set; } = DataTableFilterUiMode.FilterCard;

    // --- Child row configuration ---

    /// <summary>Enable expandable child rows.</summary>
    public bool HasChildRows { get; set; }

    /// <summary>Page size.</summary>
    public int PageLength { get; set; } = 50;

    /// <summary>AJAX endpoint for child row data.</summary>
    public string? ChildAjaxUrl { get; set; }

    /// <summary>Column definitions for child rows.</summary>
    public List<DataTableColumnViewModel>? ChildColumns { get; set; }

    /// <summary>
    /// Optional JS function invoked after a child table is created.
    /// Signature: <c>function(childTableId, parentRowData, childApi, childConfig)</c>.
    /// </summary>
    public string? ChildRowCallback { get; set; }

    /// <summary>Custom parameters for child AJAX requests (supports <c>row.</c> token values).</summary>
    public Dictionary<string, string>? ChildCustomParameters { get; set; }

    /// <summary>
    /// Raw datatables.net options merged last over the computed options (escape hatch for any option the
    /// C# model does not yet expose). Nested objects are deep-merged; array values (e.g. <c>order</c>,
    /// <c>lengthMenu</c>) replace the computed array wholesale rather than merging element-by-element.
    /// Function-valued options must be registered via <c>OpenDataTables.on(tableId, { beforeInit })</c>
    /// from host JS.
    /// </summary>
    public Dictionary<string, object?>? DataTableOptions { get; set; }

    /// <summary>
    /// Validates that the configuration uses only supported features, throwing
    /// <see cref="DataTableConfigurationException"/> (naming the offending property) when it does not.
    /// Hosts can call this right after building the model to fail fast at configuration time with a clear
    /// message; the <c>DataTable</c> ViewComponent also calls it before rendering. Does not mutate the model.
    /// </summary>
    public void Validate()
    {
        if (FilterUiMode is DataTableFilterUiMode.Inline
                or DataTableFilterUiMode.Top
                or DataTableFilterUiMode.Mixed)
        {
            throw new DataTableConfigurationException(
                nameof(FilterUiMode),
                $"DataTableFilterUiMode.{FilterUiMode} is not implemented yet. " +
                "Use DataTableFilterUiMode.FilterCard or DataTableFilterUiMode.None.");
        }

        // A Range filter only renders when a filter UI is shown, so it can only bite then.
        if (FilterUiMode != DataTableFilterUiMode.None
            && (Columns ?? new()).Any(c => c?.Filter == DataTableFilterType.Range))
        {
            throw new DataTableConfigurationException(
                nameof(Columns),
                "DataTableFilterType.Range is not implemented yet. Use Text/Date/Select/SelectStatic, " +
                "or send a range as two server-side parameters.");
        }
    }
}
