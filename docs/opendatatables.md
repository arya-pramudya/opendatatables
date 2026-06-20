# OpenDataTables

Server-side **jQuery DataTables** configured from C#: a filter card, server-side sort/paging, custom
buttons, child rows, and an editable mode — described with a `DataTableViewModel`, fed by one helper
call. Depends on OpenSelect2 (its *select* filters render Select2 dropdowns).

> Assumes you've completed [Getting started](getting-started.md) (DI, view imports, and both
> `<os2-scripts />` **and** `<odt-scripts />`).

## The two halves

1. **View** — render the table:

   ```cshtml
   @await Component.InvokeAsync("DataTable", new DataTableViewModel
   {
       TableId = "tblProducts",
       AjaxUrl = Url.Action("GetData", "Products")!,
       DefaultSortColumn = "name",
       DefaultSortDirection = "asc",
       PageLength = 10,
       FilterUiMode = DataTableFilterUiMode.FilterCard,
       Columns =
       [
           new() { Data = "id",   Title = "Id", IsVisible = false },
           new() { Data = "name", Title = "Name", Filter = DataTableFilterType.Text },
           new() { Data = "category", Title = "Category", Filter = DataTableFilterType.Select,
                   OptionsUrl = Url.Action("Categories", "Lookup")! },
           new() { Data = "status", Title = "Status", Filter = DataTableFilterType.SelectStatic,
                   StaticOptions =
                   [
                       new() { Id = "Active",   Text = "Active" },
                       new() { Id = "Inactive", Text = "Inactive" },
                   ] },
           new() { Data = "price", Title = "Price", CellClass = "text-end" },
           new() { Data = "createdAt", Title = "Created",
                   Filter = DataTableFilterType.Date, Format = "DD MMM YYYY" },
       ],
   })
   ```

   Or the declarative tag form, with columns as nested `<odt-column>` children (identical output —
   see [Tag Helper syntax](getting-started.md#tag-helper-syntax)):

   ```cshtml
   <odt-table table-id="tblProducts" ajax-url="@Url.Action("GetData", "Products")"
              default-sort-column="name" default-sort-direction="asc" page-length="10">
       <odt-column data="id"        title="Id"       visible="false" />
       <odt-column data="name"      title="Name"     filter="Text" />
       <odt-column data="category"  title="Category" filter="Select"
                   options-url="@Url.Action("Categories", "Lookup")" />
       <odt-column data="status"    title="Status"   filter="SelectStatic"
                   static-options="Active,Inactive" />
       <odt-column data="price"     title="Price"    cell-class="text-end" />
       <odt-column data="createdAt" title="Created"  filter="Date" format="DD MMM YYYY" />
   </odt-table>
   ```

2. **Controller** — return the page. Bind `DataTableQueryViewModel` (the table POSTs), accept each
   active filter as an extra bound parameter, then call `ToDataTableResponseAsync` (an extension method;
   add `using OpenDataTables.AspNetCore.Extensions;` to the controller):

   ```csharp
   [HttpPost]
   public async Task<IActionResult> GetData(
       DataTableQueryViewModel query, string? name, int? category, string? status)
   {
       var rows =
           from p in _db.Products.AsNoTracking()
           join c in _db.Categories on p.CategoryId equals c.Id
           where string.IsNullOrEmpty(name)     || p.Name.Contains(name)
           where !category.HasValue             || p.CategoryId == category.Value
           where string.IsNullOrEmpty(status)   || p.Status == status
           select new ProductRow
           {
               Id = p.Id, Name = p.Name, Category = c.Name,
               Status = p.Status, Price = p.Price, CreatedAt = p.CreatedAt
           };

       var total  = await _db.Products.CountAsync();
       var result = await rows.ToDataTableResponseAsync(query, total, defaultSortColumn: "name");
       return Json(result);
   }
   ```

The row view-model's **property names become the JSON column keys**, which must match each column's
`Data`. The filter values arrive as parameters whose names are the column `Data` keys (or `FilterColumn`
when set).

### `ToDataTableResponseAsync` parameters

| Parameter | Purpose |
|---|---|
| `query` | The bound `DataTableQueryViewModel`. |
| `recordsTotal` | Unfiltered count (shown as "x of y"). |
| `columnSelectors` | Optional `data key → expression` map for sorting computed/renamed columns. Without it, the sort column is resolved by reflection against `T`. |
| `defaultSortColumn` / `defaultSortDirection` | Applied when the request carries no explicit sort. |
| `recordsFiltered` | Supply it to skip the extra `COUNT` (otherwise computed for you). |

It calls `query.NormalizeSort()` and honors multi-column (shift-click) sort. `Length = -1` ("All")
returns every row from `Start`. The response (`DataTableResponseViewModel<T>`) serializes with the
lower-case keys DataTables expects — `draw`, `recordsTotal`, `recordsFiltered`, `data` — **don't rename
them**. There's an in-memory `ToDataTableResponse` for already-materialized data, and `ApplySorting`
for export flows that keep the UI sort but return all rows.

## Filters

Set `Filter` per column. The default UI is a **filter card** (`FilterUiMode = FilterCard`); use `None`
to render no filter UI.

| `DataTableFilterType` | Renders | Source |
|---|---|---|
| `None` | nothing | — |
| `Text` | text input | free-form `Contains` |
| `Date` | date picker | parsed date |
| `Select` | Select2 dropdown | AJAX via `OptionsUrl` (+ `OptionValueField`/`OptionTextField`/`OptionsUrlParameters`) |
| `SelectMultiple` | multi-select Select2 | same source as `Select`; chosen values are sent comma-joined |
| `SelectStatic` | Select2 dropdown | `StaticOptions` on the column |

Cascading filters: set the child column's `ParentFilterColumn` to the parent's `Data` key. Use
`FilterColumn` when the filter parameter name should differ from the displayed column (e.g. display
`statusText`, filter on `status`).

A column does **not** have to be visible in the grid to be filtered — set a `Filter` on a hidden column
(`IsVisible = false`) and it still gets a filter-card input (e.g. filter by a hidden foreign-key column).

> **Not yet implemented:** `DataTableFilterType.Range`, and the `Inline` / `Top` / `Mixed`
> `FilterUiMode` values. Selecting them throws `DataTableConfigurationException` at render time naming
> the offending property. Call `model.Validate()` right after building the model to fail fast.

## Action buttons (CSP-friendly)

Toolbar/row buttons are wired by **JS function name**, resolved at click time via delegated events — no
inline handlers, so it works under a strict CSP. Toggle the built-ins with `ShowAdd`/`ShowEdit`/
`ShowDelete`/`ShowView`; point them at your handlers with `OnAdd`/`OnView`/`OnEdit`/`OnDelete`.

```cshtml
@await Component.InvokeAsync("DataTable", new DataTableViewModel
{
    TableId = "tblProducts", AjaxUrl = Url.Action("GetData", "Products")!,
    OnAdd = "productAdd", OnView = "productView",
    OnEdit = "productEdit", OnDelete = "productDelete",
    CustomButtons =
    [
        new() { Text = "Export", Icon = "fas fa-file-csv",
                CssClass = "btn-outline-success", Placement = "top", OnClick = "productExport" },
    ],
    Columns = [ /* … */ ],
})

@section Scripts {
    <script>
        function productAdd()      { OpenDataTables.util.notify('info', 'Add clicked'); }
        function productView(id)   { OpenDataTables.util.notify('info', 'View #' + id); }
        function productEdit(id)   { OpenDataTables.util.notify('info', 'Edit #' + id); }
        function productDelete(id) {
            OpenDataTables.util.confirm({ text: 'Delete #' + id + '?' })
                .then(ok => { if (ok) OpenDataTables.util.notify('success', 'Deleted #' + id); });
        }
        function productExport()   { OpenDataTables.util.notify('info', 'Export clicked'); }
    </script>
}
```

Row handlers receive the row id (`function(id)`); `OnAdd` takes none. `OpenDataTables.util.notify` /
`confirm` wrap SweetAlert2 (the optional peer dep). In tag form the built-ins are `show-add` / `on-add`
(etc.) attributes, and custom buttons are nested `<odt-button text="Export" placement="top"
on-click="productExport" />` children.

## Nested rows (table inside a table)

Set `HasChildRows = true` with a `ChildAjaxUrl` and `ChildColumns`, and each parent row gets a `+`
toggle that expands to a second server-side grid. `ChildCustomParameters` forwards values to the child
request; a `row.` prefix reads the **parent** row's JSON, so the child can be filtered by the row it
hangs off. Here a *categories* grid expands to its *products* (the child reuses the products endpoint,
passing `category = ` the parent row's id):

```cshtml
@await Component.InvokeAsync("DataTable", new DataTableViewModel
{
    TableId = "tblCategories",
    AjaxUrl  = Url.Action("GetCategories", "Products")!,
    Columns =
    [
        new() { Data = "id",   Title = "Id", IsVisible = false },
        new() { Data = "name", Title = "Category" },
        new() { Data = "productCount", Title = "# Products", CellClass = "text-end" },
    ],
    HasChildRows = true,
    ChildAjaxUrl = Url.Action("GetData", "Products")!,
    ChildCustomParameters = new() { ["category"] = "row.id" }, // row.id → the parent category's id
    ChildColumns =
    [
        new() { Data = "name",      Title = "Product" },
        new() { Data = "status",    Title = "Status" },
        new() { Data = "price",     Title = "Price", CellClass = "text-end" },
        new() { Data = "createdAt", Title = "Created", Format = "DD MMM YYYY" },
    ],
})
```

The child grid POSTs to `ChildAjaxUrl` the same way the parent does, so the endpoint is an ordinary
`GetData`-style action. Optional `ChildRowCallback` runs after a child grid is built. Child cells honor
the **date-token** `Format` only — named formatters (registered via `OpenDataTables.on`) apply to the
parent grid, not child grids.

The tag-helper form is fully declarative — `child-ajax-url` plus `<odt-child-column>` children turn on
child rows, and `child-param-{name}="row.col"` forwards parent-row values:

```cshtml
<odt-table table-id="tblCategories" ajax-url="@Url.Action("GetCategories", "Products")"
           child-ajax-url="@Url.Action("GetData", "Products")" child-param-category="row.id">
    <odt-column data="name" title="Category" />
    <odt-child-column data="name"  title="Product" />
    <odt-child-column data="price" title="Price" />
</odt-table>
```

## Row grouping

There's no first-class grouping option — use the datatables.net **RowGroup** extension through the
[escape hatch](#other-features). Load the plugin (CSS + JS) after datatables.net and before
`<odt-scripts />`, then band rows by a data key. Because the grid is server-side, group the rows by
sorting on that key so each group is contiguous within the page:

```cshtml
@await Component.InvokeAsync("DataTable", new DataTableViewModel
{
    TableId = "tblGrouped",
    AjaxUrl  = Url.Action("GetData", "Products")!,
    DefaultSortColumn = "category",   // sort by the group key so groups don't fragment
    Columns =
    [
        new() { Data = "category", Title = "Category", IsVisible = false }, // grouped-on, hidden
        new() { Data = "name",     Title = "Product" },
        new() { Data = "price",    Title = "Price", CellClass = "text-end" },
    ],
    DataTableOptions = new()
    {
        ["rowGroup"] = new Dictionary<string, object?> { ["dataSrc"] = "category" }
    },
})
```

`rowGroup.dataSrc` reads the row data, so the grouped column can be hidden. Grouping is per page (the
extension bands the rows the server returned); re-sorting on a non-group column will fragment the
groups. For function-valued RowGroup options (e.g. `startRender`) register a `beforeInit` patch via
`OpenDataTables.on(tableId, { beforeInit })` instead of putting the function in `DataTableOptions`.

The tag helper exposes this as a first-class `group-by` attribute (it sets up RowGroup and, with no
explicit `default-sort-column`, sorts by the key):

```cshtml
<odt-table ajax-url="@Url.Action("GetData", "Products")" group-by="category">
    <odt-column data="category" title="Category" visible="false" />
    <odt-column data="name"     title="Product" />
</odt-table>
```

## Other features

- **Row numbering** — `HasNumbering = true` adds a leading number column.
- **Child rows** — `HasChildRows = true` with `ChildAjaxUrl`, `ChildColumns`, optional
  `ChildRowCallback` and `ChildCustomParameters` (supports `row.` token values).
- **Deferred loading** — `LoadTrigger` (`Immediate` default; otherwise set `TriggerSelector` /
  `TriggerEvent`) to delay the first data fetch until a UI event.
- **Dynamic parameters** — `CustomParameters` (static) and `DynamicValueSources`
  (`{ "param": "#element-id" }` or `{ "param": "#tableId|rowData.col" }`) sent with each request.
- **Editable mode** — `EditorConfigs` per column, `SaveMode` (`Manual` default, `Auto`, or `Custom`), and a
  save endpoint via `SaveAjaxUrl` (or an `OnSave` JS handler for `Custom`). See the sample's `Editable.cshtml`.
  Tag form: add `editor="Number"` (or `Text`/`Date`/`Select2Static` with `editor-static-options`) to an
  `<odt-column>`, plus `save-ajax-url` / `save-mode` on `<odt-table>`. Because paging is server-side, a page
  change redraws (and would discard unsaved inline edits) — `UnsavedEditBehavior` controls that: `Warn`
  (default; confirm and offer to discard), `AutoSave` (persist the page's edits first), or `None` (legacy:
  edits are lost). Tag attribute: `unsaved-edit-behavior`. Not applicable to `Auto` (it saves each change).
- **Escape hatch** — `DataTableOptions` (a `Dictionary<string, object?>`) is merged last over the
  computed datatables.net options (nested objects deep-merged; arrays like `order`/`lengthMenu`
  replace wholesale). Function options register from host JS:
  `OpenDataTables.on(tableId, { beforeInit: fn, myFormatter: fn })`.

## Column cell formatting

`DataTableColumnViewModel.Format` is either the name of a formatter function registered via
`OpenDataTables.on(tableId, { myFormat: fn })` (invoked as `fn(value)`), or a moment-style date token
string (e.g. `"DD MMM YYYY"`) applied when the value parses as a date.

## Full property list

See the [configuration reference](configuration.md#datatableviewmodel) for every `DataTableViewModel`
and `DataTableColumnViewModel` property.
