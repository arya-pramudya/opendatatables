# Configuration reference

Every option, view-model property, and DI setting, in one place. Behavior and recipes live in the
[OpenSelect2](openselect2.md) and [OpenDataTables](opendatatables.md) guides.

## DI options

### `OpenSelect2Options` (via `AddOpenSelect2`)

| Property | Default | Meaning |
|---|---|---|
| `DefaultLimit` | `10` | Page size used when a `Select2ViewModel.Limit` isn't set. |
| `LoginUrl` | `null` | Where the client redirects after HTTP 401. Null → reload the current page. |
| `AjaxDelayMs` | `250` | Debounce (ms) applied to AJAX search requests. |
| `Localization` | `Select2Localization.English` | Client-facing localized strings. |

### `OpenDataTablesOptions` (via `AddOpenDataTables`)

| Property | Default | Meaning |
|---|---|---|
| `DefaultPageLength` | `50` | Page length when a model doesn't specify one. |
| `LoginUrl` | `null` | Where the client redirects after HTTP 401. Null → reload. |
| `ReinitEvents` | empty | Extra DOM event names (besides `htmx:afterSwap`) that re-scan for tables. |
| `Localization` | `DataTableLocalization.English` | Client-facing localized strings. |

## Asset tag helpers

| Tag | Place in | Attribute | Notes |
|---|---|---|---|
| `<odt-styles />` | `<head>` | — | OpenDataTables stylesheet link. |
| `<os2-scripts />` | end of `<body>` | `minified` (default `true`) | OpenSelect2 config + runtime. Needed by DataTable select filters too. |
| `<odt-scripts />` | end of `<body>` | `minified` (default `true`) | OpenDataTables config + runtime (core → datatable → filtercard → init). |

All three honor the app's `PathBase` and serve assets from `_content/…`.

## Content tag helpers

Declarative equivalents of `Component.InvokeAsync` — they delegate to the same ViewComponents, so the
output is identical. See [Tag Helper syntax](getting-started.md#tag-helper-syntax) for the full mapping
rules; this is the element/attribute reference.

| Element | Parent | Renders | Required |
|---|---|---|---|
| `<os2-select>` | — | the `Select2` component | `name` (or `config`) |
| `<os2-item>` | `<os2-select>` | a static option (turns the dropdown into a local list) | `value` |
| `<odt-table>` | — | the `DataTable` component | `ajax-url` (or `config`) |
| `<odt-column>` | `<odt-table>` | a column | `data` |
| `<odt-child-column>` | `<odt-table>` | a child-grid column (its presence turns on child rows) | `data` |
| `<odt-button>` | `<odt-table>` | a custom toolbar/row button | `text`, `on-click` |

**Attribute conventions.** Scalars are kebab-cased property names (`ajax-url`, `page-length`); enums
bind from their member name (`filter="Text"`, `filter-ui-mode="None"`). Collections are nested children
(above). Dictionaries use a per-property prefix (HTML lowercases attribute names, so keys arrive
lowercased — use `config` for case-sensitive keys):

| Element | Attribute prefix | Maps to |
|---|---|---|
| `<os2-select>` | `extra-param-{name}` | `ExtraParams` (static query params) |
| `<os2-select>` | `extra-html-param-{name}` | `ExtraParamsHTML` (live params; value is a CSS selector) |
| `<os2-select>` | `extra-attr-{name}` | `ExtraAttributes` (raw HTML attributes on the select) |
| `<odt-table>` | `custom-param-{name}` | `CustomParameters` (static request params) |
| `<odt-table>` | `dynamic-source-{name}` | `DynamicValueSources` (`#element-id` value sources) |
| `<odt-table>` | `child-param-{name}` | `ChildCustomParameters` (child-grid params; `row.col` reads the parent row) |

**Convenience attributes** on `<odt-table>` with no direct model property: `group-by="key"` wires the
RowGroup extension and sorts by the key (equivalent to `DataTableOptions["rowGroup"]`). On `<odt-column>`,
`editor="Text|Number|Date|Select2Static"` (with `editor-static-options` / `editor-options-url`) adds an
`EditorConfigs` entry, and `static-options="A,B,C"` builds a `SelectStatic` filter's options (id == text).

**Escape hatch.** `config="@model"` seeds the tag from a prebuilt `Select2ViewModel`/`DataTableViewModel`
(cloned before render, so a shared/cached template is never mutated); explicit attributes then override
the matching property and nested children replace the matching collection. Requires the
`@addTagHelper *, OpenSelect2.AspNetCore` / `@addTagHelper *, OpenDataTables.AspNetCore` directives.

## `Select2ViewModel`

| Property | Type | Default | Notes |
|---|---|---|---|
| `Id` | `string` | random | Element `id`. |
| `Name` | `string` | **required** | Element `name`. |
| `Label` | `string?` | — | Label above the control. |
| `IsMultiple` | `bool` | `false` | Allow multiple values. |
| `IsRequired` | `bool` | `false` | Adds asterisk + `required`. |
| `IsDisabled` | `bool` | `false` | Render disabled. |
| `IsReadOnly` | `bool` | `false` | Value kept but not changeable. |
| `ForceDisabled` | `bool` | `false` | Stay disabled even when the parent has a value. |
| `CanSelectAll` | `bool` | `false` | Inject a synthetic "(Select All)" first result on page 1. |
| `Items` | `List<Select2ListItem>?` | — | Static options; when set, the control is local (no AJAX). |
| `AjaxUrl` | `string` | `""` | Endpoint returning `{ items, hasMore }`. Ignored when `Items` is set. |
| `Limit` | `int` | `10` | Page size sent to the endpoint. |
| `Placeholder` | `string?` | `"Select an option"` | Empty-state text. |
| `ParentId` | `string?` | — | Parent control's `id` (cascade); child auto-disables until parent has a value. |
| `ExtraParams` | `Dictionary<string,string>?` | — | Static query params (name → literal). |
| `ExtraParamsHTML` | `Dictionary<string,string>?` | — | Live params (name → CSS selector, read at request time). |
| `EnableChildrenIfPreSelected` | `bool` | `false` | Enable children even when this parent is disabled but pre-selected. |
| `SelectedItems` | `List<Select2ListItem>?` | — | Items selected on first render. |
| `PreselectKey` | `string?` | — | Opaque key handed to a registered `ISelect2Preselector`. |
| `CssClass` | `string?` | — | Extra classes on the select. |
| `ExtraAttributes` | `Dictionary<string,string>?` | — | Raw HTML attributes on the select. |
| `Select2Options` | `Dictionary<string,object?>?` | — | Raw select2 options merged last (escape hatch). |

### `Select2QueryViewModel` (request)

| Property | Default | Notes |
|---|---|---|
| `SearchTerm` | `""` | Never null (null coerced to `""`). |
| `Page` | `1` | 1-based page index. |
| `Limit` | `10` | Requested page size (clamped to ≤ 1000 by the helpers). |
| `ParentValue` | `null` | Parent control value, for cascades. |

### `Select2ListItem` / `Select2Result`

`Select2ListItem`: `Id` (req), `Text` (req), `Selected`, `Disabled`, `Extra` (`[JsonExtensionData]`,
flattened to top-level JSON). `Select2Result`: `Items`, `HasMore`. Wire shape: `{ items, hasMore }`.

## `DataTableViewModel`

| Property | Type | Default | Notes |
|---|---|---|---|
| `TableId` | `string` | `""` → auto | Table `id`. Left blank by default so the component assigns a unique `dt_{guid}` id (so multiple tables on a page never collide); set it to target the table from host CSS/JS. |
| `AjaxUrl` | `string` | **required** | Data endpoint. |
| `SaveAjaxUrl` | `string?` | — | Save endpoint (editable tables). |
| `Columns` | `List<DataTableColumnViewModel>` | empty | Column definitions. |
| `DefaultSortColumn` / `DefaultSortDirection` | `string?` | — | Initial sort when none requested. |
| `EditorConfigs` | `List<…>` | empty | Per-column editors (editable tables). |
| `ShowAdd` / `ShowEdit` / `ShowDelete` / `ShowView` | `bool` | `true` | Built-in button visibility. |
| `OnAdd` / `OnView` / `OnEdit` / `OnDelete` / `OnSave` | `string?` | — | JS function names (resolved at click time, CSP-friendly). |
| `CustomAddText` / `CustomEditText` / `CustomDeleteText` | `string?` | — | Button label overrides. |
| `CustomButtons` | `List<DataTableButtonViewModel>?` | — | Toolbar/row buttons. |
| `RowCallback` / `ChildRowCallback` | `string?` | — | JS callbacks. |
| `CustomParameters` | `Dictionary<string,string>?` | — | Static params on every request. |
| `DynamicValueSources` | `Dictionary<string,string>?` | — | `{ "param": "#el" }` or `{ "param": "#tableId\|rowData.col" }`. |
| `LoadTrigger` | `DataTableLoadTriggerType` | `Immediate` | When to first load data. |
| `TriggerSelector` / `TriggerEvent` | `string?` | — | Used with non-immediate `LoadTrigger`. |
| `HasNumbering` | `bool` | `false` | Leading row-number column. |
| `SaveMode` | `EditableTableSaveMode` | `Manual` | `Manual` (Save/Cancel buttons), `Auto` (persist on each change), or `Custom` (host `OnSave`). |
| `UnsavedEditBehavior` | `UnsavedEditBehavior` | `Warn` | Editable grids: what happens on a page change with unsaved inline edits — `Warn` (confirm/discard), `AutoSave` (persist first), or `None` (discard, legacy). Ignored for `Auto` save mode. Tag attribute: `unsaved-edit-behavior`. |
| `FilterUiMode` | `DataTableFilterUiMode` | `FilterCard` | `FilterCard` or `None`; `Inline`/`Top`/`Mixed` throw `DataTableConfigurationException` (not implemented). |
| `HasChildRows` | `bool` | `false` | Expandable child rows. |
| `PageLength` | `int` | `50` | Page size. |
| `ChildAjaxUrl` / `ChildColumns` / `ChildCustomParameters` | — | Child row config. |
| `DataTableOptions` | `Dictionary<string,object?>?` | — | Raw datatables.net options merged last (escape hatch). |
| `Validate()` | method | — | Throws `DataTableConfigurationException` for unimplemented features (call it to fail fast). |

### `DataTableColumnViewModel`

| Property | Default | Notes |
|---|---|---|
| `Data` | **required** | Source property → JSON column key. |
| `Title` | **required** | Header text. |
| `IsVisible` | `true` | Column visibility. |
| `Filter` | `None` | `None`/`Text`/`Date`/`Select`/`SelectMultiple`/`SelectStatic` (`Range` not implemented). |
| `Format` | — | Formatter function name (`fn(value)`) or moment-style date token. |
| `Options` | — | Static string options (select filters). |
| `OptionsUrl` / `OptionValueField` / `OptionTextField` / `OptionsUrlParameters` | — | AJAX option source for `Select`. |
| `StaticOptions` | — | Options for `SelectStatic`. |
| `ParentFilterColumn` | — | Parent `Data` key for cascading filters. |
| `FilterColumn` | — | Filter param name when it differs from `Data`. |
| `FilterPlacement` | `Inline` | Where the filter renders. |
| `CustomColumnWidthClass` | — | Bootstrap grid width class for this column's filter cell (e.g. `col-md-6`); overrides the auto-computed width. |
| `FilterIndex` / `TableIndex` | — | Explicit ordering in the filter UI / table head. |
| `SelectedItems` | — | Pre-selected items for this column's select filter. |
| `IsDisabled` | `false` | Render this filter disabled. |
| `IsSortable` | `true` | Column sortable. |
| `HeaderClass`/`HeaderStyle`/`CellClass`/`CellStyle`/`Width`/`NoWrap` | — | Styling. |
| `FilterClass`/`FilterStyle`/`FilterTopClass`/`FilterTopStyle` | — | Filter input styling. |

### `DataTableQueryViewModel` (request)

| Property | Default | Notes |
|---|---|---|
| `Draw` | `"0"` | Echoed back unchanged. |
| `Start` | `0` | Zero-based first record. |
| `Length` | `10` | Page size (`-1` = all). |
| `SortColumnIndex` / `SortColumnName` / `SortDirection` | — | Primary sort (back-compat scalars). |
| `SortOrders` | empty | Full multi-column sort list (canonical). |
| `NormalizeSort()` | — | Reconciles scalars and `SortOrders` (called by the helpers). |

### `DataTableResponseViewModel<T>` (response)

Lower-case keys are the DataTables wire contract — **do not rename**: `draw`, `recordsTotal`,
`recordsFiltered`, `data`, plus optional `groupCounts` and `grandTotalSum`.

## Controller helpers

| Helper | On | Returns |
|---|---|---|
| `ControllerBase.Select2Result(result)` / `(items, hasMore)` | controller | `JsonResult` `{ items, hasMore }`. |
| `IQueryable<Select2ListItem>.ToSelect2ResultAsync(query, ct)` | query | `Select2Result` (DB-side paging). |
| `IEnumerable<Select2ListItem>.ToSelect2Result(query)` | query | `Select2Result` (in-memory). |
| `IQueryable<T>.ToDataTableResponseAsync(query, recordsTotal, columnSelectors?, defaultSortColumn?, defaultSortDirection?, recordsFiltered?, ct)` | query | `DataTableResponseViewModel<T>`. |
| `IEnumerable<T>.ToDataTableResponse(query, recordsTotal, columnSelectors?, defaultSortColumn?, defaultSortDirection?)` | query | `DataTableResponseViewModel<T>` (in-memory). |
| `IQueryable<T>.ApplySorting(query, columnSelectors?, defaultSortColumn?, defaultSortDirection?)` | query | `IOrderedQueryable<T>` (export: sort, no paging). |

## Server-side hooks

| Interface | Registration | Purpose |
|---|---|---|
| `ISelect2Preselector` | `AddScoped<ISelect2Preselector, T>()` | Populate `SelectedItems`/disabled state from server context, keyed by `Select2ViewModel.PreselectKey`. |
| `IDataTableFilterPreselector` | `AddScoped<IDataTableFilterPreselector, T>()` | Server-side pre-selection for DataTable filters. |

## Client-side hooks

| Call | Purpose |
|---|---|
| `OpenSelect2.on(id, { templateResult, templateSelection, beforeInit })` | Register function-valued select2 options. |
| `OpenDataTables.on(tableId, { beforeInit, <formatterName> })` | Register table init hook / column formatter functions. |
| `OpenDataTables.util.notify(type, message)` / `confirm({ text })` | Toast / confirm dialog (wrap SweetAlert2). |
| `window.OpenSelect2.config` / `window.OpenDataTables.config` | Emitted by the `<*-scripts />` tag helpers (login URL, locale, page sizes). |
