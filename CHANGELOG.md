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
- **DataTable validation** (#13): unsupported-but-configured features now throw a typed
  `DataTableConfigurationException` (naming the offending property) via the new `DataTableViewModel.Validate()`,
  which hosts can call early to fail fast at configuration time instead of getting a raw render-time throw.
- **JS escape hatch**: `DataTableOptions` / `Select2Options` are now merged with a prototype-pollution-safe
  deep-merge (skips `__proto__`/`constructor`/`prototype`); a non-object `ajax` value no longer throws
  during init; the built-in `ajax.data`/`ajax.error` (DataTables) and `ajax.transport` (Select2) are
  preserved across both the merge **and** a `beforeInit` replacement.
- **JS handler resolution**: named formatters / `rowCallback` / `childRowCallback` resolved from `window`
  now go through the same dangerous-global denylist as row-button handlers (no accidental `window.open`,
  `window.print`, …); the formatter is resolved once per column instead of per cell render.
- **Action buttons**: a custom button with no explicit `placement` is treated as a row button again
  (matches the C# model default); theming `icons`/`actionClasses` values are HTML-escaped.

### Changed
- **Select2 `dropdownParent`**: dropdowns inside a modal now anchor to the modal element (better stacking
  and focus); non-modal dropdowns anchor to `<body>` on both the static and AJAX paths (was the parent
  element on the static path) to avoid overflow-clipping. Hosts relying on the previous parent anchoring
  for scoped CSS should set `dropdownParent` via the `Select2Options` escape hatch.

### Notes
- `datatable-utils.js` audit recorded in [docs/DATATABLE_UTILS_AUDIT.md](docs/DATATABLE_UTILS_AUDIT.md).
  Deferred follow-ups: in-header inline/top filter rendering, multi-parent cascade, filter-state URL
  persistence, and dynamic Select2 cell editors.
