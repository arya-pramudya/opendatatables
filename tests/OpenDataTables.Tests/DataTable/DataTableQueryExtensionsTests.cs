using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenDataTables.AspNetCore.Extensions;
using OpenDataTables.AspNetCore.Models;
using SampleApp.Data;
using Xunit;

namespace OpenDataTables.Tests.DataTable;

public class DataTableQueryExtensionsTests
{
    private sealed record Row(int Id, string Name, decimal Price);

    private static List<Row> Rows() =>
    [
        new(1, "Charlie", 30m),
        new(2, "alpha", 10m),
        new(3, "Bravo", 20m),
        new(4, "delta", 40m),
    ];

    private static DataTableQueryViewModel Query(string sortCol = "", string dir = "asc", int start = 0, int len = 10, string draw = "1") =>
        new() { Draw = draw, Start = start, Length = len, SortColumnName = sortCol, SortDirection = dir };

    [Fact]
    public void ToDataTableResponse_sorts_ascending_and_echoes_counts()
    {
        var result = Rows().ToDataTableResponse(Query("name", "asc"), recordsTotal: 4);

        Assert.Equal("1", result.draw);
        Assert.Equal(4, result.recordsTotal);
        Assert.Equal(4, result.recordsFiltered);
        Assert.Equal(new[] { "alpha", "Bravo", "Charlie", "delta" }, result.data!.Select(r => r.Name));
    }

    [Fact]
    public void ToDataTableResponse_sorts_descending()
    {
        var result = Rows().ToDataTableResponse(Query("price", "desc"), 4);
        Assert.Equal(new[] { 40m, 30m, 20m, 10m }, result.data!.Select(r => r.Price));
    }

    [Fact]
    public void ToDataTableResponse_pages_with_start_and_length()
    {
        var result = Rows().ToDataTableResponse(Query("id", "asc", start: 1, len: 2), 4, defaultSortColumn: "id");
        Assert.Equal(new[] { 2, 3 }, result.data!.Select(r => r.Id));
    }

    [Fact]
    public void ToDataTableResponse_length_negative_returns_all_rows()
    {
        // DataTables "All" sends Length = -1; must return every row, not Take(-1) (empty).
        var result = Rows().ToDataTableResponse(Query("id", "asc", start: 0, len: -1), 4, defaultSortColumn: "id");
        Assert.Equal(4, result.data!.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, result.data!.Select(r => r.Id));
    }

    [Fact]
    public async Task ToDataTableResponseAsync_length_negative_returns_all_rows()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseInMemoryDatabase($"dt-all-{Guid.NewGuid():N}").Options;
        await using var db = new SampleDbContext(options);
        SampleDataSeeder.Seed(db);

        var source = db.Categories.AsNoTracking().Select(c => new { c.Id, c.Name });
        var result = await source.ToDataTableResponseAsync(Query("name", "asc", len: -1), 8, defaultSortColumn: "name");

        Assert.Equal(8, result.data!.Count);
    }

    [Fact]
    public void ToDataTableResponse_falls_back_to_default_sort_when_no_column()
    {
        var result = Rows().ToDataTableResponse(Query(""), 4, defaultSortColumn: "price", defaultSortDirection: "desc");
        Assert.Equal(40m, result.data!.First().Price);
    }

    [Fact]
    public void ToDataTableResponse_treats_implicit_id_sort_as_default_fallback()
    {
        // SortColumnName "id" with a different default column is treated as "no explicit sort".
        var result = Rows().ToDataTableResponse(Query("id", "asc"), 4, defaultSortColumn: "name", defaultSortDirection: "asc");
        Assert.Equal("alpha", result.data!.First().Name);
    }

    private sealed record NullableRow(int Id, DateTime? When);

    [Fact]
    public void ToDataTableResponse_sorts_nullable_value_column_with_nulls_without_throwing()
    {
        var rows = new List<NullableRow>
        {
            new(1, new DateTime(2024, 1, 2)),
            new(2, null),
            new(3, new DateTime(2024, 1, 1)),
        };

        // A nullable value-type column with nulls must not crash the in-memory sort: the key selector
        // must not coalesce null to "" (which mixes DateTime and string and throws in Comparer<object>).
        var result = rows.ToDataTableResponse(Query("when", "asc"), recordsTotal: 3);

        // Nulls sort first in ascending order, then the real dates in chronological order.
        Assert.Equal(new[] { 2, 3, 1 }, result.data!.Select(r => r.Id));
    }

    [Fact]
    public void ToDataTableResponse_honors_explicit_id_primary_in_multi_column_sort()
    {
        // A genuine 2-column sort starting on Id is an explicit user choice: it must NOT be rewritten to
        // the default column the way a lone implicit Id sort is. Id is unique here, so Id-desc fully
        // determines the order (the secondary Name sort is a tie-breaker only).
        var query = Query("id", "asc");
        query.SortOrders =
        [
            new SortDescriptor { Column = "Id", Direction = "desc" },
            new SortDescriptor { Column = "Name", Direction = "asc" },
        ];

        var result = Rows().ToDataTableResponse(query, 4, defaultSortColumn: "name", defaultSortDirection: "asc");

        Assert.Equal(new[] { 4, 3, 2, 1 }, result.data!.Select(r => r.Id));
    }

    [Fact]
    public void ToDataTableResponse_sorts_explicit_real_column_not_in_selectors_by_that_column()
    {
        // A columnSelectors map that maps the DEFAULT column must not hijack the sort of a different,
        // explicitly-requested real property. Regression: the in-memory path used to fall back to the
        // default column's selector (unlike the IQueryable path) whenever the requested column had no key.
        var rows = new List<Row>
        {
            new(1, "alpha", 40m),
            new(2, "bravo", 30m),
            new(3, "charlie", 20m),
            new(4, "delta", 10m),
        };
        var selectors = new Dictionary<string, Func<Row, object?>> { ["name"] = r => r.Name };

        // Sort by "price" (a real property, not a selector key) with the default column mapped to "name".
        var result = rows.ToDataTableResponse(Query("price", "asc"), 4, selectors, defaultSortColumn: "name");

        // Price ascending → 10,20,30,40 → Ids 4,3,2,1. (The old fallback would sort by name → 1,2,3,4.)
        Assert.Equal(new[] { 4, 3, 2, 1 }, result.data!.Select(r => r.Id));
    }

    [Fact]
    public void ToDataTableResponse_uses_column_selector_override()
    {
        var selectors = new Dictionary<string, Func<Row, object?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["display"] = r => r.Price // a virtual column that maps to Price
        };
        var result = Rows().ToDataTableResponse(Query("display", "asc"), 4, selectors);
        Assert.Equal(new[] { 10m, 20m, 30m, 40m }, result.data!.Select(r => r.Price));
    }

    [Fact]
    public async Task ToDataTableResponseAsync_orders_and_pages_over_EF()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseInMemoryDatabase($"dt-async-{Guid.NewGuid():N}")
            .Options;
        await using var db = new SampleDbContext(options);
        SampleDataSeeder.Seed(db);

        var source = db.Categories.AsNoTracking().Select(c => new { c.Id, c.Name });
        var total = await db.Categories.CountAsync();

        var result = await source.ToDataTableResponseAsync(Query("name", "asc", len: 3), total, defaultSortColumn: "name");

        Assert.Equal(8, result.recordsTotal);
        Assert.Equal(8, result.recordsFiltered);
        Assert.Equal(3, result.data!.Count);
        Assert.Equal("Automotive", result.data!.First().Name);
    }

    [Fact]
    public void ApplySorting_orders_without_paging()
    {
        var selectors = new Dictionary<string, Expression<Func<Row, object>>>();
        var ordered = Rows().AsQueryable().ApplySorting(Query("name", "asc"), selectors).ToList();
        Assert.Equal(4, ordered.Count);
        Assert.Equal("alpha", ordered.First().Name);
    }
}
