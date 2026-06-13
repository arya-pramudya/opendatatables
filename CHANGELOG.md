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

### Notes
- `datatable-utils.js` audit recorded in [docs/DATATABLE_UTILS_AUDIT.md](docs/DATATABLE_UTILS_AUDIT.md).
  Deferred follow-ups: in-header inline/top filter rendering, multi-parent cascade, filter-state URL
  persistence, and dynamic Select2 cell editors.
