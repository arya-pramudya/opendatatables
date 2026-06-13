# OpenSelect2.AspNetCore

Server-configured AJAX [Select2](https://select2.org/) dropdowns for ASP.NET Core 8 MVC — rendered
by a ViewComponent with **zero manual JS init**.

```cshtml
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Name = "CategoryId", Label = "Category",
    AjaxUrl = Url.Action("Categories", "Lookup")!,
})
```

```csharp
[HttpGet]
public async Task<IActionResult> Categories(Select2QueryViewModel query)
{
    var q = _db.Categories.AsNoTracking()
        .Where(c => query.SearchTerm == "" || c.Name.Contains(query.SearchTerm))
        .OrderBy(c => c.Name)
        .Select(c => new Select2ListItem { Id = c.Id.ToString(), Text = c.Name });

    return this.Select2Result(await q.ToSelect2ResultAsync(query));
}
```

**Peer dependencies (not bundled):** jQuery 3.x, select2 4.0.13, Bootstrap 5 (theme optional),
SweetAlert2 11 (optional).

See the [repository README](https://github.com/aryapramudya/opendatatables) for full docs. MIT licensed.
