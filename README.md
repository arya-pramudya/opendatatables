# OpenDataTables &amp; OpenSelect2 for ASP.NET Core MVC

Two small, MIT-licensed Razor Class Libraries that turn a strongly-typed C# config into a fully
working, server-side **jQuery DataTable** or AJAX **Select2** dropdown — with **zero manual JS init**.

| Package | What it gives you |
|---|---|
| [`OpenSelect2.AspNetCore`](src/OpenSelect2.AspNetCore) | Server-configured AJAX Select2 with infinite-scroll paging, cascading parent→child chains, pre-selection, read-only/disabled semantics, and a 5-line controller response helper. |
| [`OpenDataTables.AspNetCore`](src/OpenDataTables.AspNetCore) | Server-side DataTable with filters (text/select/cascading/date), a filter card, child rows, custom buttons, an editable mode, and sort/page helpers. Depends on `OpenSelect2.AspNetCore`. |

```cshtml
@* one line renders an AJAX dropdown — no <script> to write *@
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Name = "CategoryId", Label = "Category",
    AjaxUrl = Url.Action("Categories", "Lookup")!,
})
```

## Status

🚧 **Pre-release.** Extracted from a production app and being decoupled into generic packages.
First preview targets `0.1.0-preview.1`. APIs may change until `1.0.0`.

## Peer dependencies (you provide these)

The packages **do not** bundle front-end libraries — your app already has opinions about how to
serve them. Tested versions: **jQuery** 3.x, **datatables.net** 1.13+/2.x, **Bootstrap** 5,
**select2** 4.0.13, **SweetAlert2** 11 (optional).

## Repository layout

```
src/      the two NuGet packages (Razor Class Libraries)
samples/  SampleApp — minimal MVC app demoing every feature
tests/    xUnit unit tests + ASP.NET Core MVC render tests (Mvc.Testing)
```

## Building

```bash
dotnet build OpenDataTables.sln
dotnet test  OpenDataTables.sln
dotnet run --project samples/SampleApp
```

## License

[MIT](LICENSE) © 2026 Arya Pramudya
