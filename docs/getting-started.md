# Getting started

This walks through wiring both packages into an ASP.NET Core **MVC** app (the `Component.InvokeAsync`
ViewComponent flow). Everything here mirrors the runnable [`samples/SampleApp`](../samples/SampleApp).

## 1. Peer dependencies (you provide these)

The packages deliberately **do not** bundle front-end libraries — your app already has opinions about
how to serve them. Reference them however you like (CDN, LibMan, npm). Tested versions:

| Library | Version | Needed by |
|---|---|---|
| jQuery | 3.x | both |
| select2 | 4.0.13 | OpenSelect2 (and DataTable *select* filters) |
| datatables.net | 1.13+ / 2.x | OpenDataTables |
| Bootstrap | 5 | both (markup/styling) |
| SweetAlert2 | 11 | optional — `OpenDataTables.util.confirm` / `notify` |

Load order in the layout matters: **jQuery → select2 → datatables.net → Bootstrap → SweetAlert2**.

## 2. Install the packages

```bash
dotnet add package OpenSelect2.AspNetCore
dotnet add package OpenDataTables.AspNetCore
```

`OpenDataTables.AspNetCore` depends on `OpenSelect2.AspNetCore`, so adding the table package pulls in
the dropdown package too. (Add only `OpenSelect2.AspNetCore` if you just want dropdowns.)

## 3. Register in `Program.cs`

Call the `Add…` methods **after** `AddControllersWithViews()`. `AddOpenDataTables()` calls
`AddOpenSelect2()` internally (its select filters render Select2), so you only need the second call if
you want to configure Select2 options or register a preselector.

```csharp
builder.Services.AddControllersWithViews();

// Optional — defaults apply without the configure lambda.
builder.Services.AddOpenSelect2(options =>
{
    options.DefaultLimit = 10;        // default AJAX page size
    options.LoginUrl     = "/Account/Login"; // where the client redirects on HTTP 401
    options.AjaxDelayMs  = 250;       // search debounce
    options.Localization = Select2Localization.English;
});

builder.Services.AddOpenDataTables(options =>
{
    options.DefaultPageLength = 50;
    options.LoginUrl          = "/Account/Login";
    options.Localization      = DataTableLocalization.English;
    // options.ReinitEvents.Add("my:after-swap"); // extra DOM events that re-scan for tables
});
```

> **Why a manual registration?** These are Razor Class Libraries that reference MVC through the shared
> framework, which hides them from the default ViewComponent/view discovery. `AddOpenSelect2()` /
> `AddOpenDataTables()` register the application part that makes the `Select2`, `DataTable`, and
> `FilterCard` components and their compiled views discoverable.

## 4. Add the tag-helper directives

In `Views/_ViewImports.cshtml`:

```cshtml
@using OpenSelect2.AspNetCore
@using OpenSelect2.AspNetCore.Models
@using OpenDataTables.AspNetCore
@using OpenDataTables.AspNetCore.Models
@addTagHelper *, OpenSelect2.AspNetCore
@addTagHelper *, OpenDataTables.AspNetCore
```

## 5. Wire styles and scripts into your layout

In `_Layout.cshtml`. The `<*-styles />` / `<*-scripts />` tag helpers emit only the package's own
assets (served from `_content/…`) plus an inline config block — the peer libraries are still yours to
include.

```cshtml
<head>
    @* peer CSS *@
    <link rel="stylesheet" href="…/bootstrap.min.css" />
    <link rel="stylesheet" href="…/select2.min.css" />
    <link rel="stylesheet" href="…/dataTables.bootstrap5.min.css" />

    <odt-styles />            @* OpenDataTables stylesheet *@
</head>
<body>
    @RenderBody()

    @* peer JS, in order *@
    <script src="…/jquery.min.js"></script>
    <script src="…/select2.min.js"></script>
    <script src="…/jquery.dataTables.min.js"></script>
    <script src="…/dataTables.bootstrap5.min.js"></script>
    <script src="…/bootstrap.bundle.min.js"></script>
    <script src="…/sweetalert2@11"></script>   @* optional *@

    @* OpenSelect2 runtime (also needed by DataTable select filters), then OpenDataTables runtime *@
    <os2-scripts />
    <odt-scripts />

    @await RenderSectionAsync("Scripts", required: false)
</body>
```

Notes:

- Place `<os2-scripts />` / `<odt-scripts />` **once**, near the end of `<body>`, after the peer JS.
- They reference the **minified** assets by default. Pass `minified="false"` to load the readable
  builds while debugging: `<odt-scripts minified="false" />`.
- The config they emit (`window.OpenSelect2.config` / `window.OpenDataTables.config`) carries your
  `LoginUrl`, page sizes, and localized strings to the browser. They honor the app's `PathBase`.
- The package CSS/JS are static web assets under `_content/…`, so your app must call
  `app.UseStaticFiles()` (the default ASP.NET Core MVC template already does).

## 6. Render a control

A dropdown:

```cshtml
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Name = "CategoryId", Label = "Category",
    AjaxUrl = Url.Action("Categories", "Lookup")!,
})
```

A table:

```cshtml
@await Component.InvokeAsync("DataTable", new DataTableViewModel
{
    TableId = "tblProducts",
    AjaxUrl = Url.Action("GetData", "Products")!,
    Columns =
    [
        new() { Data = "name",   Title = "Name",   Filter = DataTableFilterType.Text },
        new() { Data = "status", Title = "Status" },
    ],
})
```

Then feed each from a controller action — see [OpenSelect2](openselect2.md) and
[OpenDataTables](opendatatables.md).

## Tag Helper syntax

Every control above can also be written as a declarative tag instead of `Component.InvokeAsync`.
The tags delegate to the same ViewComponents, so the rendered markup, JSON config block, and
runtime behavior are identical — pick whichever reads better in a given view. No extra setup is
needed beyond the `@addTagHelper` directives from step 4.

A dropdown:

```cshtml
@* equivalent to Component.InvokeAsync("Select2", …) *@
<os2-select name="CategoryId" label="Category" ajax-url="@Url.Action("Categories", "Lookup")" />
```

A table — columns are nested `<odt-column>` children:

```cshtml
<odt-table table-id="tblProducts" ajax-url="@Url.Action("GetData", "Products")"
           default-sort-column="name" page-length="10">
    <odt-column data="name"   title="Name"   filter="Text" />
    <odt-column data="status" title="Status" filter="SelectStatic" static-options="Active,Inactive" />
</odt-table>
```

How options map to attributes:

- **Scalars** use kebab-case attribute names — `ajax-url`, `page-length`, `multiple`, `required`,
  `parent-id`, `filter-ui-mode`, `show-add`, … Enums bind from their name (e.g. `filter="Text"`).
- **Dictionaries** use a prefix: e.g. `extra-param-tenantId="5"` on `<os2-select>` →
  `ExtraParams["tenantid"]`. On `<os2-select>`: `extra-param-…` (static), `extra-html-param-…` (live,
  value is a CSS selector), `extra-attr-…` (raw select attributes). On `<odt-table>`: `custom-param-…`,
  `dynamic-source-…`, and `child-param-…` (child-grid params). (HTML lowercases attribute names, so
  dictionary keys arrive lowercased — use `config` below for case-sensitive keys.)
- **Collections** use nested children: `<os2-item value="Active" text="Active" selected />` inside
  `<os2-select>` (a static list); inside `<odt-table>`, `<odt-column …/>` (columns),
  `<odt-child-column …/>` (the expand/child grid), and `<odt-button …/>` (custom toolbar/row buttons).
- **Editable, grouped, and nested tables** are declarative too — no `config` model needed:
  - editable: add `editor="Number"` (or `Text`/`Date`/`Select2Static` with `editor-static-options`)
    to an `<odt-column>`, plus `save-ajax-url`;
  - grouped: `group-by="category"` on `<odt-table>` wires the RowGroup extension and sorts by the key;
  - nested (table-in-a-table): `child-ajax-url` + `<odt-child-column>` children, with optional
    `child-param-{name}="row.col"` to forward parent-row values.
- **Escape hatch** — for raw `Select2Options` / `DataTableOptions`, or to reuse a prebuilt model, pass
  `config="@model"`; explicit attributes then override the matching property and nested children
  replace the matching collection:

  ```cshtml
  <odt-table config="@Model.ProductGrid" page-length="25" />
  ```

`<os2-select>` requires `name`, and `<odt-table>` requires `ajax-url` (unless supplied via `config`);
omitting them throws a clear error naming the tag and attribute. The live `/Home/TagHelpers` page in
the sample app shows each form.

## Run the sample

```bash
dotnet run --project samples/SampleApp
```

It demos every feature (basic/multiple/cascade/preselect/static/select-all/read-only dropdowns, a
filtered sortable table, and an editable table) against an in-memory EF Core database. The
**Tag Helpers** page renders the same controls using the declarative `<os2-select>` / `<odt-table>`
syntax.
