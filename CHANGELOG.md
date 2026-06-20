# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Monorepo bootstrap: `OpenSelect2.AspNetCore` + `OpenDataTables.AspNetCore` Razor Class Libraries,
  shared SampleApp, xUnit/Mvc.Testing tests, esbuild minification, and CI.
- **OpenSelect2.AspNetCore**: a single `Select2` ViewComponent that is AJAX-driven or static (set
  `Items`) — local/remote data on one component, like Kendo's DropDownList; slimmed `Select2ListItem`
  (`{ Id, Text, Selected, Disabled }` + `[JsonExtensionData] Extra`); `Select2QueryViewModel`;
  `ISelect2Preselector` hook (`PreselectKey`); `ToSelect2Result(Async)` paging (take-one-extra) and
  `this.Select2Result(...)` controller helpers; `openselect2.js` (scanner + cascade, no per-instance
  inline script); `<os2-scripts/>` tag helper; `AddOpenSelect2`.
- **OpenDataTables.AspNetCore**: a single `DataTable` ViewComponent (read-only or inline-editable —
  editing turns on by supplying `EditorConfigs`, like Kendo's grid `editable`) + `FilterCard`;
  namespaced models with `PreselectKey`; wire contracts preserved (`DataTableQueryViewModel` /
  `DataTableResponseViewModel<T>`); `ToDataTableResponse(Async)` / `ApplySorting` / `ToFilterCard`
  extensions; `IDataTableFilterPreselector` hook; clean decoupled JS runtime
  (`opendatatables-core/datatable/filtercard/editable/init`) with CSP-friendly delegated action
  buttons; `<odt-styles/>` / `<odt-scripts/>` tag helpers; `AddOpenDataTables`.
- **Declarative tag-helper syntax**: `<os2-select>` (with nested `<os2-item>` static options) and
  `<odt-table>` render the same components as `Component.InvokeAsync` by delegating to the existing
  ViewComponents — identical markup, JSON config, and preselector hooks. The full table feature set is
  expressible as tags: nested `<odt-column>` (columns), `<odt-child-column>` (table-in-a-table child
  grid), `<odt-button>` (custom buttons), `editor="…"` on a column (editable mode), and `group-by` (the
  RowGroup extension). Common properties map to kebab-case attributes (enums bind by name); `Dictionary`
  options use a per-property prefix (`extra-param-*` / `extra-html-param-*` / `extra-attr-*` on
  `<os2-select>`; `custom-param-*` / `dynamic-source-*` / `child-param-*` on `<odt-table>`); a
  `config="@model"` escape hatch seeds the tag from a prebuilt view-model (cloned before render, so a
  shared template is never mutated). No extra registration beyond the existing `@addTagHelper *, …`
  directives. Shown on the SampleApp **Tag Helpers** page.
- **Unsaved-edit guard** (`DataTableViewModel.UnsavedEditBehavior`, tag attribute `unsaved-edit-behavior`):
  editable grids no longer silently lose the current page's inline edits when the user changes page. Defaults
  to `Warn` (confirm, with a discard option); `AutoSave` persists the page's edits first; `None` keeps the
  legacy discard-on-redraw behavior. Applies to `Manual`/`Custom` save modes (`Auto` already saves per change).
- **Hidden columns are filterable**: a column with a `Filter` now renders a filter-card input even when it is
  hidden in the grid (`IsVisible = false`) — e.g. to filter by a hidden foreign-key column. (`FilterCardViewModel`
  `GetVisibleColumns` → `GetFilterableColumns`.)
- English + Indonesian localization presets for both packages.

### Fixed
- **Sorting (server)**: a request that sets `SortOrders` to `null` (e.g. an explicit `"SortOrders": null`
  in the body, or a hand-built query) no longer throws `NullReferenceException`; the in-memory path no
  longer sorts by the default column when the requested column is a real-but-unmapped property
  (now mirrors the `IQueryable` path); property reflection for secondary sorts is cached per type.
- **Sorting representation** (#14): added `DataTableQueryViewModel.NormalizeSort()` — `SortOrders` is now the
  single canonical source. When present, the back-compat scalars (`SortColumnName`/`SortDirection`) are
  derived from `SortOrders[0]`; when only the scalars are set, a matching `SortOrders` entry is synthesized.
  The sort/paging extensions call it, so both representations always agree downstream.
- **Sorting (default column)**: an explicit sort direction on the default column is now honored on the
  initial draw (`Draw="1"`) instead of being rewritten to `defaultSortDirection` — so a `stateSave`-restored
  or pre-seeded descending sort on the default column survives the first request.
- **DataTable validation** (#13): unsupported-but-configured features now throw a typed
  `DataTableConfigurationException` (naming the offending property) via the new `DataTableViewModel.Validate()`,
  which hosts can call early to fail fast at configuration time instead of getting a raw render-time throw.
  The check is an allow-list (`FilterCard`/`None`), so any future `FilterUiMode` value is rejected until
  implemented rather than silently rendering nothing.
- **JS escape hatch**: `DataTableOptions` / `Select2Options` are now merged with a prototype-pollution-safe
  deep-merge (skips `__proto__`/`constructor`/`prototype`) that recurses only into **plain** objects and
  assigns everything else (Date, jQuery `dropdownParent`, DOM nodes, class instances) by reference instead
  of corrupting it; a non-object `ajax` value no longer throws during init; the built-in `ajax.data`/
  `ajax.error` (DataTables) and `ajax.transport` (Select2) are preserved across both the merge **and** a
  `beforeInit` replacement, and a host-overridable `ajax.url` is restored when a `beforeInit` drops it.
- **JS handler resolution**: named formatters / `rowCallback` / `childRowCallback` resolved from `window`
  now go through the same dangerous-global denylist as row-button handlers (no accidental `window.open`,
  `window.print`, …), only match **own** `window` properties (not inherited `Object.prototype` members
  like `constructor`/`toString`), and reject native built-ins (`Date`, `parseInt`, …) so a name must
  resolve to a host-defined function; the formatter is resolved once per column instead of per cell render.
- **Action buttons**: a custom button with no explicit `placement` is treated as a row button again
  (matches the C# model default); theming `icons`/`actionClasses` values are HTML-escaped.

### Security
- **Select2 `ExtraAttributes`**: tightened the attribute-**name** allow-list (values were already
  HTML-encoded). Names are restricted to plain `data-`/`aria-`/identifier tokens — no `:` (blocks
  `xlink:href` / `xmlns:*`), no event handlers (`on*`), and an explicit denylist of URL/markup/style-bearing
  names (`style`, `src`, `srcdoc`, `href`, `formaction`, `poster`, …) that could carry an active payload
  even with an encoded value.

### Changed
- **Select2 `dropdownParent`**: dropdowns inside a modal now anchor to the modal element (better stacking
  and focus); non-modal dropdowns anchor to `<body>` on both the static and AJAX paths (was the parent
  element on the static path) to avoid overflow-clipping. Hosts relying on the previous parent anchoring
  for scoped CSS should set `dropdownParent` via the `Select2Options` escape hatch.

### Notes
- Deferred follow-ups: in-header inline/top filter rendering, multi-parent cascade, filter-state URL
  persistence, `DataTableFilterType.Range`, and dynamic Select2 cell editors.
