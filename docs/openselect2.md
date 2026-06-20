# OpenSelect2

Server-configured AJAX **Select2** dropdowns. You describe the control with a `Select2ViewModel` in a
Razor view; the matching controller action returns a page of items with the bundled helpers. No inline
JS init.

> Assumes you've completed [Getting started](getting-started.md) (DI, view imports, `<os2-scripts />`).

## The two halves

1. **View** — render the control:

   ```cshtml
   @await Component.InvokeAsync("Select2", new Select2ViewModel
   {
       Id = "basicCategory", Name = "BasicCategory", Label = "Category",
       AjaxUrl = Url.Action("Categories", "Lookup")!,
       Placeholder = "Search categories…",
   })
   ```

   Or the declarative tag form (identical output — see
   [Tag Helper syntax](getting-started.md#tag-helper-syntax)):

   ```cshtml
   <os2-select id="basicCategory" name="BasicCategory" label="Category"
               ajax-url="@Url.Action("Categories", "Lookup")" placeholder="Search categories…" />
   ```

2. **Controller** — return the data. Bind the request to `Select2QueryViewModel`, project to
   `Select2ListItem`, page with `ToSelect2ResultAsync`, and emit with `this.Select2Result(...)`. Both
   helpers are extension methods, so add `using OpenSelect2.AspNetCore.Extensions;` to the controller:

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

   The wire response is `{ items, hasMore }`. `hasMore` drives infinite scroll and is computed with a
   take-one-extra trick (it fetches `Limit + 1` rows) — no second `COUNT` round-trip.

### The request contract — `Select2QueryViewModel`

Bind it directly instead of declaring loose parameters.

| Property | Meaning |
|---|---|
| `SearchTerm` | User's search text. Never null — coerced to `""`, so `query.SearchTerm == "" || …` is safe. |
| `Page` | 1-based page index for infinite scroll. |
| `Limit` | Page size requested by the client. |
| `ParentValue` | For cascades: the current value of the parent control (see below). |

`ToSelect2ResultAsync` clamps `Limit` to a max of 1000 so a hostile/buggy client can't request the
whole table. There's an in-memory sibling, `ToSelect2Result`, for already-materialized sequences.

### The item shape — `Select2ListItem`

| Property | JSON | Notes |
|---|---|---|
| `Id` (required) | `id` | The option value. |
| `Text` (required) | `text` | The display text. |
| `Selected` | `selected` | Server-rendered selection. |
| `Disabled` | `disabled` | Disable an individual option. |
| `Extra` | *(flattened)* | `[JsonExtensionData]` — arbitrary metadata flattened to top-level JSON (e.g. `{ id, text, currency }`), readable by Select2 templates. Keys emit verbatim, so use the casing the client expects. |

## Recipes

### Multiple select

```cshtml
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Id = "multiProducts", Name = "MultiProducts", Label = "Products",
    AjaxUrl = Url.Action("Products", "Lookup")!,
    IsMultiple = true, Placeholder = "Pick several products…",
})
```

### Cascade (parent → child)

Set the child's `ParentId` to the parent's element `id`. The child auto-disables until the parent has a
value, and the parent's current value arrives in the child action's `query.ParentValue`.

```cshtml
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Id = "cascadeCategory", Name = "CascadeCategory", Label = "Category",
    AjaxUrl = Url.Action("Categories", "Lookup")!,
})
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Id = "cascadeSub", Name = "CascadeSub", Label = "Subcategory",
    AjaxUrl = Url.Action("SubCategories", "Lookup")!,
    ParentId = "cascadeCategory", Placeholder = "Choose a category first…",
})
```

```csharp
[HttpGet]
public async Task<IActionResult> SubCategories(Select2QueryViewModel query)
{
    var parentId = int.TryParse(query.ParentValue, out var cid) ? cid : 0;

    var q = _db.SubCategories.AsNoTracking()
        .Where(s => s.CategoryId == parentId)
        .Where(s => query.SearchTerm == "" || s.Name.Contains(query.SearchTerm))
        .OrderBy(s => s.Name)
        .Select(s => new Select2ListItem { Id = s.Id.ToString(), Text = s.Name });

    return this.Select2Result(await q.ToSelect2ResultAsync(query));
}
```

### Static list (no AJAX)

Set `Items` and leave `AjaxUrl` empty — same component, rendered as a local list. `AjaxUrl` and the
other AJAX-only fields are ignored when `Items` is non-empty.

```cshtml
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Id = "staticStatus", Name = "StaticStatus", Label = "Status",
    Items =
    [
        new() { Id = "Active", Text = "Active" },
        new() { Id = "Inactive", Text = "Inactive" },
    ],
    SelectedItems = [ new() { Id = "Active", Text = "Active" } ],
})
```

### Read-only and disabled

- `IsReadOnly` — value is kept and posted but can't be changed.
- `IsDisabled` — rendered disabled.
- `ForceDisabled` — stay disabled even when a parent has a value (overrides the cascade enabling rule).

```cshtml
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Id = "roCategory", Name = "RoCategory", Label = "Category",
    AjaxUrl = Url.Action("Categories", "Lookup")!,
    IsReadOnly = true,
    SelectedItems = [ new() { Id = "2", Text = "Groceries" } ],
})
```

### "Select All" option

`CanSelectAll = true` injects a synthetic "(Select All)" entry as the first result on page 1.

### Server-side pre-selection (`PreselectKey` + `ISelect2Preselector`)

To pre-select from server context (e.g. the current user) without baking domain logic into the
package, set an opaque `PreselectKey` and implement `ISelect2Preselector` in your app:

```csharp
// Program.cs
builder.Services.AddScoped<ISelect2Preselector, MyPreselector>();
```

```csharp
public class MyPreselector : ISelect2Preselector
{
    public void Apply(Select2ViewModel model, HttpContext context)
    {
        if (model.PreselectKey == "current-user-category")
        {
            model.SelectedItems ??= new();
            model.SelectedItems.Add(new Select2ListItem { Id = "1", Text = "Electronics" });
            model.IsDisabled = true;        // lock it
            model.ForceDisabled = true;
            model.EnableChildrenIfPreSelected = true; // let child cascades still enable
        }
    }
}
```

```cshtml
@await Component.InvokeAsync("Select2", new Select2ViewModel
{
    Name = "Category", AjaxUrl = Url.Action("Categories", "Lookup")!,
    PreselectKey = "current-user-category",
})
```

### Extra request parameters

- `ExtraParams` — static `name → literal value` sent with every AJAX request.
- `ExtraParamsHTML` — `name → CSS selector`; the selector's **current** value is read and sent at
  request time (live DOM-sourced).

In tag form these are dictionary-prefix attributes: `extra-param-{name}="value"` (static) and
`extra-html-param-{name}="#selector"` (live). The live prefix is `extra-html-param-`, *not*
`extra-param-html-`, so it doesn't collide with the static `extra-param-` prefix.

### Escape hatch — raw select2 options

Anything the C# model doesn't expose yet goes in `Select2Options` (a `Dictionary<string, object?>`),
merged last over the computed settings. Nested objects are deep-merged; array values replace the
computed array wholesale. **Function-valued** options (e.g. `templateResult`) can't live in C# — register
them from host JS:

```js
OpenSelect2.on("basicCategory", {
    templateResult: function (item) { /* … */ },
    templateSelection: function (item) { /* … */ },
    beforeInit: function (settings) { /* … */ },
});
```

## Full property list

See the [configuration reference](configuration.md#select2viewmodel) for every `Select2ViewModel`
property.
