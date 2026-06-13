# Contributing

Thanks for your interest! This is a monorepo with two packages that ship in lockstep during `0.x`.

## Prerequisites

- .NET 8 SDK
- Node 18+ (only for the JS minify step, `esbuild`)

## Workflow

1. `dotnet build OpenDataTables.sln` and `dotnet test OpenDataTables.sln` must stay green.
2. Run `dotnet format` before committing.
3. The two packages version together: **`OpenSelect2.AspNetCore` publishes first**, then
   `OpenDataTables.AspNetCore` (which depends on it).
4. Front-end behavior changes must be demoed in `samples/SampleApp` and covered by a Playwright test.

## Coupling rules (important)

These packages are deliberately **generic**. Do not introduce domain concepts (auth claims,
business-entity fields, hard-coded URLs, or non-English UI strings) into the library code. Host
apps plug those in via the documented hooks:

- `ISelect2Preselector` / `IDataTableFilterPreselector` for server-side pre-selection.
- `window.OpenSelect2.config` / `window.OpenDataTables.config` for client-side URLs/locale/callbacks.
- `Select2ListItem.Extra` (`[JsonExtensionData]`) for arbitrary per-item metadata.
