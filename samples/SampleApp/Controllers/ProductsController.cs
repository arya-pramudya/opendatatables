using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenDataTables.AspNetCore.Extensions;
using OpenDataTables.AspNetCore.Models;
using SampleApp.Data;
using SampleApp.Models;

namespace SampleApp.Controllers;

public class ProductsController : Controller
{
    private readonly SampleDbContext _db;

    public ProductsController(SampleDbContext db) => _db = db;

    /// <summary>Server-side data endpoint. Filters arrive as <c>name</c>/<c>category</c>/<c>status</c> params.</summary>
    [HttpPost]
    public async Task<IActionResult> GetData(DataTableQueryViewModel query, string? name, int? category, string? status)
    {
        var rows =
            from p in _db.Products.AsNoTracking()
            join c in _db.Categories on p.CategoryId equals c.Id
            where string.IsNullOrEmpty(name) || p.Name.Contains(name)
            where !category.HasValue || p.CategoryId == category.Value
            where string.IsNullOrEmpty(status) || p.Status == status
            select new ProductRow
            {
                Id = p.Id,
                Name = p.Name,
                Category = c.Name,
                Status = p.Status,
                Price = p.Price,
                CreatedAt = p.CreatedAt
            };

        var total = await _db.Products.CountAsync();
        var result = await rows.ToDataTableResponseAsync(query, total, defaultSortColumn: "name");
        return Json(result);
    }

    /// <summary>Editable-table save endpoint. Accepts the JSON payload posted by the editable runtime.</summary>
    [HttpPost]
    public IActionResult SaveData([FromBody] List<Dictionary<string, object>>? rows)
    {
        // Demo: echo back a success envelope. A real app would persist the changed fields.
        return Json(new { isSuccess = true, message = $"Saved {rows?.Count ?? 0} row(s)" });
    }
}
