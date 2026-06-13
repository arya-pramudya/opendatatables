using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenSelect2.AspNetCore.Extensions;
using OpenSelect2.AspNetCore.Models;
using SampleApp.Data;

namespace SampleApp.Controllers;

/// <summary>AJAX endpoints that feed the Select2 dropdowns. Each is ~5 lines thanks to the helpers.</summary>
public class LookupController : Controller
{
    private readonly SampleDbContext _db;

    public LookupController(SampleDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Categories(Select2QueryViewModel query)
    {
        var q = _db.Categories.AsNoTracking()
            .Where(c => query.SearchTerm == "" || c.Name.Contains(query.SearchTerm))
            .OrderBy(c => c.Name)
            .Select(c => new Select2ListItem { Id = c.Id.ToString(), Text = c.Name });

        return this.Select2Result(await q.ToSelect2ResultAsync(query));
    }

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

    [HttpGet]
    public async Task<IActionResult> Products(Select2QueryViewModel query)
    {
        var q = _db.Products.AsNoTracking()
            .Where(p => query.SearchTerm == "" || p.Name.Contains(query.SearchTerm))
            .OrderBy(p => p.Name)
            .Select(p => new Select2ListItem { Id = p.Id.ToString(), Text = p.Name });

        return this.Select2Result(await q.ToSelect2ResultAsync(query));
    }
}
