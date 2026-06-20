using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.TagHelpers;

/// <summary>
/// Declarative equivalent of <c>@await Component.InvokeAsync("DataTable", config)</c>: renders a
/// server-side DataTable from tag attributes and nested <c>&lt;odt-column&gt;</c> children by delegating
/// to the existing <c>DataTable</c> ViewComponent — so the emitted markup, JSON config block, filter
/// card, and CSP-friendly action buttons are identical to the imperative form.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;odt-table ajax-url="/Products/GetData" page-length="10"
///            child-ajax-url="/Products/GetData" child-param-category="row.id"
///            group-by="category" save-ajax-url="/Products/Save"&gt;
///     &lt;odt-button text="Export" placement="top" on-click="exportFn" /&gt;
///     &lt;odt-column data="name" title="Name" filter="Text" editor="Text" /&gt;
///     &lt;odt-column data="status" title="Status" filter="Select" options-url="/Lookup/Status" /&gt;
///     &lt;odt-child-column data="sku" title="SKU" /&gt;
/// &lt;/odt-table&gt;
/// </code>
/// Child rows (<c>&lt;odt-child-column&gt;</c>), custom buttons (<c>&lt;odt-button&gt;</c>), inline
/// editors (<c>editor="…"</c> on a column), and grouping (<c>group-by</c>) are all expressible as tags —
/// no <c>config</c> model required. The <c>config</c> attribute remains an escape hatch for raw
/// <c>DataTableOptions</c> or reusing a prebuilt model; explicit attributes then override the same
/// property, nested <c>&lt;odt-column&gt;</c> children replace its <c>Columns</c>, and nested
/// <c>&lt;odt-button&gt;</c> children are appended. Requires <c>@addTagHelper *, OpenDataTables.AspNetCore</c>.
/// </remarks>
[HtmlTargetElement("odt-table")]
public class DataTableTagHelper : TagHelper
{
    /// <summary>
    /// Marker/payload type shared via <see cref="TagHelperContext.Items"/> so nested
    /// <c>&lt;odt-column&gt;</c> children can register their columns on the parent.
    /// </summary>
    internal sealed class ColumnBag : List<DataTableColumnViewModel> { }

    /// <summary>Marker bag for nested <c>&lt;odt-child-column&gt;</c> children (the expand/child grid).</summary>
    internal sealed class ChildColumnBag : List<DataTableColumnViewModel> { }

    /// <summary>Marker bag for nested <c>&lt;odt-button&gt;</c> children (custom toolbar/row buttons).</summary>
    internal sealed class ButtonBag : List<DataTableButtonViewModel> { }

    /// <summary>Marker bag for column-level <c>editor="…"</c> declarations (editable tables).</summary>
    internal sealed class EditorBag : List<DataTableColumnEditorConfig> { }

    private readonly IViewComponentHelper _viewComponentHelper;

    /// <summary>Creates the tag helper.</summary>
    public DataTableTagHelper(IViewComponentHelper viewComponentHelper)
        => _viewComponentHelper = viewComponentHelper;

    /// <summary>The current view context (required to contextualize the ViewComponent helper).</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    // --- Scalar attributes (nullable so "unset" is distinguishable from an explicit value) ---

    /// <summary>The data endpoint (required unless supplied via <see cref="Config"/>).</summary>
    [HtmlAttributeName("ajax-url")] public string? AjaxUrl { get; set; }

    /// <summary>The HTML id of the table (auto-generated when omitted).</summary>
    [HtmlAttributeName("table-id")] public string? TableId { get; set; }

    /// <summary>Page size.</summary>
    [HtmlAttributeName("page-length")] public int? PageLength { get; set; }

    /// <summary>Default sort column (data key).</summary>
    [HtmlAttributeName("default-sort-column")] public string? DefaultSortColumn { get; set; }

    /// <summary>Default sort direction (<c>asc</c>/<c>desc</c>).</summary>
    [HtmlAttributeName("default-sort-direction")] public string? DefaultSortDirection { get; set; }

    /// <summary>How filters are displayed (<c>FilterCard</c> or <c>None</c>; case-insensitive).</summary>
    [HtmlAttributeName("filter-ui-mode")] public string? FilterUiMode { get; set; }

    /// <summary>When to load data (<c>Immediate</c>, <c>OnClick</c>, <c>Modal</c>, <c>Custom</c>; case-insensitive).</summary>
    [HtmlAttributeName("load-trigger")] public string? LoadTrigger { get; set; }

    /// <summary>Selector for the element that triggers deferred loading.</summary>
    [HtmlAttributeName("trigger-selector")] public string? TriggerSelector { get; set; }

    /// <summary>Event name for a custom load trigger.</summary>
    [HtmlAttributeName("trigger-event")] public string? TriggerEvent { get; set; }

    /// <summary>Save mode for editable tables (<c>Manual</c>, <c>Auto</c>, <c>Custom</c>; case-insensitive).</summary>
    [HtmlAttributeName("save-mode")] public string? SaveMode { get; set; }

    /// <summary>
    /// What an editable grid does when the user changes page with unsaved inline edits (<c>Warn</c>,
    /// <c>AutoSave</c>, <c>None</c>; case-insensitive). Defaults to <c>Warn</c>.
    /// </summary>
    [HtmlAttributeName("unsaved-edit-behavior")] public string? UnsavedEditBehavior { get; set; }

    /// <summary>Save endpoint for editable tables.</summary>
    [HtmlAttributeName("save-ajax-url")] public string? SaveAjaxUrl { get; set; }

    /// <summary>Render a leading row-number column.</summary>
    [HtmlAttributeName("has-numbering")] public bool? HasNumbering { get; set; }

    /// <summary>Enable expandable child rows.</summary>
    [HtmlAttributeName("has-child-rows")] public bool? HasChildRows { get; set; }

    /// <summary>AJAX endpoint for child row data.</summary>
    [HtmlAttributeName("child-ajax-url")] public string? ChildAjaxUrl { get; set; }

    /// <summary>Show the Add button.</summary>
    [HtmlAttributeName("show-add")] public bool? ShowAdd { get; set; }

    /// <summary>Show the per-row Edit button.</summary>
    [HtmlAttributeName("show-edit")] public bool? ShowEdit { get; set; }

    /// <summary>Show the per-row Delete button.</summary>
    [HtmlAttributeName("show-delete")] public bool? ShowDelete { get; set; }

    /// <summary>Show the per-row View button.</summary>
    [HtmlAttributeName("show-view")] public bool? ShowView { get; set; }

    /// <summary>Override text for the Add button.</summary>
    [HtmlAttributeName("custom-add-text")] public string? CustomAddText { get; set; }

    /// <summary>Override text for the Edit button.</summary>
    [HtmlAttributeName("custom-edit-text")] public string? CustomEditText { get; set; }

    /// <summary>Override text for the Delete button.</summary>
    [HtmlAttributeName("custom-delete-text")] public string? CustomDeleteText { get; set; }

    /// <summary>JS function name for the Add action.</summary>
    [HtmlAttributeName("on-add")] public string? OnAdd { get; set; }

    /// <summary>JS function name for the View action.</summary>
    [HtmlAttributeName("on-view")] public string? OnView { get; set; }

    /// <summary>JS function name for the Edit action.</summary>
    [HtmlAttributeName("on-edit")] public string? OnEdit { get; set; }

    /// <summary>JS function name for the Delete action.</summary>
    [HtmlAttributeName("on-delete")] public string? OnDelete { get; set; }

    /// <summary>JS function name for custom save (when <c>save-mode="Custom"</c>).</summary>
    [HtmlAttributeName("on-save")] public string? OnSave { get; set; }

    /// <summary>JS function name invoked for each row (DataTables <c>rowCallback</c>).</summary>
    [HtmlAttributeName("row-callback")] public string? RowCallback { get; set; }

    /// <summary>JS function name invoked after a child grid is created.</summary>
    [HtmlAttributeName("child-row-callback")] public string? ChildRowCallback { get; set; }

    /// <summary>
    /// Groups rows by a data key using the datatables.net RowGroup extension (load the plugin first). When
    /// set with no explicit <see cref="DefaultSortColumn"/>, the grid is sorted by this key so each group
    /// stays contiguous. Equivalent to setting <c>DataTableOptions["rowGroup"]</c> imperatively.
    /// </summary>
    [HtmlAttributeName("group-by")] public string? GroupBy { get; set; }

    /// <summary>
    /// A fully-built model used as the starting point; explicit attributes override the same property
    /// and nested <c>&lt;odt-column&gt;</c> children replace its <c>Columns</c>. Use this escape hatch for
    /// custom buttons, editor configs, child columns, or raw <c>DataTableOptions</c>.
    /// </summary>
    [HtmlAttributeName("config")] public DataTableViewModel? Config { get; set; }

    // --- Dictionary-prefix attributes ---

    /// <summary>Static params sent with every request: <c>custom-param-{name}="value"</c>.</summary>
    [HtmlAttributeName("custom-params", DictionaryAttributePrefix = "custom-param-")]
    public Dictionary<string, string> CustomParameters { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Dynamic value sources: <c>dynamic-source-{name}="#element-id"</c>.</summary>
    [HtmlAttributeName("dynamic-sources", DictionaryAttributePrefix = "dynamic-source-")]
    public Dictionary<string, string> DynamicValueSources { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Child-grid request params: <c>child-param-{name}="value"</c>. A <c>row.</c> prefix reads the parent
    /// row's JSON (e.g. <c>child-param-category="row.id"</c> sends the parent row's id as <c>category</c>).
    /// </summary>
    [HtmlAttributeName("child-params", DictionaryAttributePrefix = "child-param-")]
    public Dictionary<string, string> ChildCustomParameters { get; set; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        // Seed the shared bags, then realize the child tags (<odt-column>, <odt-child-column>,
        // <odt-button>, and column-level editor="…") — they register into these.
        var columns = new ColumnBag();
        var childColumns = new ChildColumnBag();
        var buttons = new ButtonBag();
        var editors = new EditorBag();
        context.Items[typeof(ColumnBag)] = columns;
        context.Items[typeof(ChildColumnBag)] = childColumns;
        context.Items[typeof(ButtonBag)] = buttons;
        context.Items[typeof(EditorBag)] = editors;
        await output.GetChildContentAsync();

        var model = BuildModel(columns, childColumns, buttons, editors);

        ((IViewContextAware)_viewComponentHelper).Contextualize(ViewContext);
        var content = await _viewComponentHelper.InvokeAsync("DataTable", model);

        output.TagName = null; // emit the component's HTML without the <odt-table> wrapper
        output.Content.SetHtmlContent(content);
    }

    private DataTableViewModel BuildModel(
        List<DataTableColumnViewModel> columns,
        List<DataTableColumnViewModel> childColumns,
        List<DataTableButtonViewModel> buttons,
        List<DataTableColumnEditorConfig> editors)
    {
        DataTableViewModel model;
        if (Config is not null)
        {
            // Clone so attribute overrides and the downstream render pipeline never mutate the caller's
            // template (which may be a cached/shared model or rendered by more than one tag on a page).
            model = Config.CloneForRender();
            if (AjaxUrl is not null) model.AjaxUrl = AjaxUrl;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(AjaxUrl))
                throw new InvalidOperationException(
                    "<odt-table> requires an 'ajax-url' attribute (or a 'config' model that supplies AjaxUrl).");
            model = new DataTableViewModel { AjaxUrl = AjaxUrl };
        }

        if (TableId is not null) model.TableId = TableId;
        if (PageLength is { } pageLength) model.PageLength = pageLength;
        if (DefaultSortColumn is not null) model.DefaultSortColumn = DefaultSortColumn;
        if (DefaultSortDirection is not null) model.DefaultSortDirection = DefaultSortDirection;
        if (TagHelperEnum.Parse<DataTableFilterUiMode>(FilterUiMode, "odt-table", "filter-ui-mode") is { } filterUiMode)
            model.FilterUiMode = filterUiMode;
        if (TagHelperEnum.Parse<DataTableLoadTriggerType>(LoadTrigger, "odt-table", "load-trigger") is { } loadTrigger)
            model.LoadTrigger = loadTrigger;
        if (TriggerSelector is not null) model.TriggerSelector = TriggerSelector;
        if (TriggerEvent is not null) model.TriggerEvent = TriggerEvent;
        if (TagHelperEnum.Parse<EditableTableSaveMode>(SaveMode, "odt-table", "save-mode") is { } saveMode)
            model.SaveMode = saveMode;
        if (TagHelperEnum.Parse<UnsavedEditBehavior>(UnsavedEditBehavior, "odt-table", "unsaved-edit-behavior") is { } unsavedEditBehavior)
            model.UnsavedEditBehavior = unsavedEditBehavior;
        if (SaveAjaxUrl is not null) model.SaveAjaxUrl = SaveAjaxUrl;
        if (HasNumbering is { } hasNumbering) model.HasNumbering = hasNumbering;
        if (HasChildRows is { } hasChildRows) model.HasChildRows = hasChildRows;
        if (ChildAjaxUrl is not null) model.ChildAjaxUrl = ChildAjaxUrl;
        if (ShowAdd is { } showAdd) model.ShowAdd = showAdd;
        if (ShowEdit is { } showEdit) model.ShowEdit = showEdit;
        if (ShowDelete is { } showDelete) model.ShowDelete = showDelete;
        if (ShowView is { } showView) model.ShowView = showView;
        if (CustomAddText is not null) model.CustomAddText = CustomAddText;
        if (CustomEditText is not null) model.CustomEditText = CustomEditText;
        if (CustomDeleteText is not null) model.CustomDeleteText = CustomDeleteText;
        if (OnAdd is not null) model.OnAdd = OnAdd;
        if (OnView is not null) model.OnView = OnView;
        if (OnEdit is not null) model.OnEdit = OnEdit;
        if (OnDelete is not null) model.OnDelete = OnDelete;
        if (OnSave is not null) model.OnSave = OnSave;
        if (RowCallback is not null) model.RowCallback = RowCallback;
        if (ChildRowCallback is not null) model.ChildRowCallback = ChildRowCallback;

        if (CustomParameters.Count > 0) model.CustomParameters = Merge(model.CustomParameters, CustomParameters);
        if (DynamicValueSources.Count > 0) model.DynamicValueSources = Merge(model.DynamicValueSources, DynamicValueSources);
        if (ChildCustomParameters.Count > 0) model.ChildCustomParameters = Merge(model.ChildCustomParameters, ChildCustomParameters);

        // Nested <odt-column> children replace the model's columns.
        if (columns.Count > 0) model.Columns = columns;

        // Nested <odt-child-column> children define the child grid; their presence turns on child rows
        // (unless the host explicitly set has-child-rows).
        if (childColumns.Count > 0)
        {
            model.ChildColumns = childColumns;
            if (HasChildRows is null) model.HasChildRows = true;
        }

        // Nested <odt-button> children — append to any buttons carried by the config model.
        if (buttons.Count > 0)
            model.CustomButtons = (model.CustomButtons ?? new()).Concat(buttons).ToList();

        // Column-level editor="…" declarations make the grid editable.
        if (editors.Count > 0) model.EditorConfigs = editors;

        // group-by="key": configure the RowGroup extension via the options escape hatch and, unless an
        // explicit sort is set, sort by the group key so groups stay contiguous on the page.
        if (!string.IsNullOrWhiteSpace(GroupBy))
        {
            model.DataTableOptions ??= new();
            model.DataTableOptions["rowGroup"] = new Dictionary<string, object?> { ["dataSrc"] = GroupBy };
            if (string.IsNullOrWhiteSpace(model.DefaultSortColumn)) model.DefaultSortColumn = GroupBy;
        }

        return model;
    }

    private static Dictionary<string, string> Merge(Dictionary<string, string>? existing, Dictionary<string, string> additions)
    {
        var result = existing is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(existing);
        foreach (var kv in additions)
            result[kv.Key] = kv.Value;
        return result;
    }
}
