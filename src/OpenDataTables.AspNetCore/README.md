# OpenDataTables.AspNetCore

Server-side [jQuery DataTables](https://datatables.net/) for ASP.NET Core 8 MVC — a strongly-typed C#
config rendered by a ViewComponent, with **zero manual JS init**. Depends on
[`OpenSelect2.AspNetCore`](https://www.nuget.org/packages/OpenSelect2.AspNetCore) for select filters.

```cshtml
@await Component.InvokeAsync("DataTable", new DataTableViewModel
{
    TableId = "tblProducts",
    AjaxUrl = Url.Action("GetData", "Products")!,
    DefaultSortColumn = "name",
    Columns =
    [
        new() { Data = "name", Title = "Name", Filter = DataTableFilterType.Text },
        new() { Data = "createdAt", Title = "Created", Format = "DD MMM YYYY" },
    ],
    FilterUiMode = DataTableFilterUiMode.FilterCard,
})
```

```csharp
[HttpPost]
public async Task<IActionResult> GetData(DataTableQueryViewModel query, string? nameFilter)
{
    var source = _db.Products.AsNoTracking()
        .Where(p => nameFilter == null || p.Name.Contains(nameFilter))
        .Select(p => new ProductRowVm { /* ... */ });

    var total = await _db.Products.CountAsync();
    return Json(await source.ToDataTableResponseAsync(query, total, defaultSortColumn: "name"));
}
```

**Peer dependencies (not bundled):** jQuery 3.x, datatables.net 1.13+/2.x, Bootstrap 5,
select2 4.0.13, SweetAlert2 11 (optional).

See the [repository README](https://github.com/aryapramudya/opendatatables) for full docs. MIT licensed.
