using Microsoft.EntityFrameworkCore;
using OpenSelect2.AspNetCore.Extensions;
using OpenSelect2.AspNetCore.Models;
using SampleApp.Data;
using Xunit;

namespace OpenDataTables.Tests.Select2;

public class Select2QueryExtensionsTests
{
    private static List<Select2ListItem> Items(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Select2ListItem { Id = i.ToString(), Text = $"Item {i}" })
            .ToList();

    [Fact]
    public void ToSelect2Result_full_page_reports_hasMore_via_take_one_extra()
    {
        // 8 items, limit 3 → first page returns exactly 3 with hasMore = true (a 4th existed).
        var result = Items(8).ToSelect2Result(new Select2QueryViewModel { Page = 1, Limit = 3 });

        Assert.Equal(3, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.Equal(new[] { "1", "2", "3" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public void ToSelect2Result_last_partial_page_reports_no_more()
    {
        // page 3 of 8 @ limit 3 → items 7,8 and hasMore = false.
        var result = Items(8).ToSelect2Result(new Select2QueryViewModel { Page = 3, Limit = 3 });

        Assert.Equal(new[] { "7", "8" }, result.Items.Select(i => i.Id));
        Assert.False(result.HasMore);
    }

    [Fact]
    public void ToSelect2Result_exact_multiple_last_page_has_no_more()
    {
        // 6 items, limit 3, page 2 → items 4,5,6 and hasMore = false (no 7th).
        var result = Items(6).ToSelect2Result(new Select2QueryViewModel { Page = 2, Limit = 3 });

        Assert.Equal(new[] { "4", "5", "6" }, result.Items.Select(i => i.Id));
        Assert.False(result.HasMore);
    }

    [Fact]
    public void ToSelect2Result_empty_source_is_empty_and_no_more()
    {
        var result = Items(0).ToSelect2Result(new Select2QueryViewModel { Page = 1, Limit = 10 });

        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ToSelect2Result_non_positive_limit_falls_back_to_ten(int limit)
    {
        var result = Items(25).ToSelect2Result(new Select2QueryViewModel { Page = 1, Limit = limit });

        Assert.Equal(10, result.Items.Count);
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task ToSelect2ResultAsync_pages_over_EF_IQueryable()
    {
        // Exercises the real EF Core async materializer (ToListAsync), not LINQ-to-Objects.
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseInMemoryDatabase($"s2-async-{Guid.NewGuid():N}")
            .Options;
        await using var db = new SampleDbContext(options);
        SampleDataSeeder.Seed(db); // 8 categories

        var query = db.Categories.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new Select2ListItem { Id = c.Id.ToString(), Text = c.Name });

        var result = await query.ToSelect2ResultAsync(new Select2QueryViewModel { Page = 1, Limit = 3 });

        Assert.Equal(3, result.Items.Count);
        Assert.True(result.HasMore);
    }
}
