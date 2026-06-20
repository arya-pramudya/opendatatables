# Documentation

Guides for **OpenDataTables.AspNetCore** and **OpenSelect2.AspNetCore** — two MIT-licensed Razor
Class Libraries that turn a strongly-typed C# config into a working server-side jQuery DataTable or
AJAX Select2, with zero hand-written JS init.

| Guide | Read it when you want to… |
|---|---|
| [Getting started](getting-started.md) | Install the packages, wire up DI, and add the scripts/styles to your layout. |
| [OpenSelect2](openselect2.md) | Render AJAX dropdowns: infinite scroll, cascading parent→child, pre-selection, static lists. |
| [OpenDataTables](opendatatables.md) | Render server-side tables: filters, sorting/paging, action buttons, child rows, editable mode. |
| [Configuration reference](configuration.md) | Look up every option, view-model property, and DI setting in one place. |

> **Status:** pre-release (`0.1.0-preview.1`). APIs may change until `1.0.0`. The fastest way to see
> every feature working end-to-end is the runnable [`samples/SampleApp`](../samples/SampleApp) —
> these guides quote it throughout.

## The 30-second picture

Both packages follow the same shape:

1. **Register** the package in `Program.cs` (`AddOpenSelect2()` / `AddOpenDataTables()`).
2. **Render** a control from a Razor view — either a single `Component.InvokeAsync(...)` call with a C#
   view-model, or the equivalent declarative tag (`<os2-select>` / `<odt-table>`) — no `<script>` to write.
3. **Feed** it data from a controller action using the bundled query helpers (a Select2 endpoint is
   ~5 lines; a DataTable endpoint is one `ToDataTableResponseAsync(...)` call).

The packages **do not bundle** front-end libraries — your app provides jQuery, datatables.net,
Bootstrap 5, select2, and (optionally) SweetAlert2. See [Getting started](getting-started.md) for the
tested versions.
