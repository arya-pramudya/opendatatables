namespace SampleApp.Data;

/// <summary>Seeds enough rows that Select2 paging and DataTable sorting/filtering are demonstrable.</summary>
public static class SampleDataSeeder
{
    private static readonly string[] CategoryNames =
        { "Electronics", "Groceries", "Clothing", "Home & Garden", "Toys", "Books", "Sports", "Automotive" };

    private static readonly string[] Statuses = { "Active", "Inactive", "Discontinued" };

    // Guards against concurrent seeding when multiple WebApplicationFactory hosts (parallel tests)
    // share the same in-memory store.
    private static readonly object SeedLock = new();

    public static void Seed(SampleDbContext db)
    {
        lock (SeedLock)
        {
            SeedCore(db);
        }
    }

    private static void SeedCore(SampleDbContext db)
    {
        if (db.Categories.Any()) return;

        var rnd = new Random(42);

        var categories = CategoryNames
            .Select((name, i) => new Category { Id = i + 1, Name = name })
            .ToList();
        db.Categories.AddRange(categories);

        var subCategories = new List<SubCategory>();
        var subId = 1;
        foreach (var cat in categories)
        {
            // 6 subcategories per category so the cascade child has multiple pages.
            for (var s = 1; s <= 6; s++)
                subCategories.Add(new SubCategory { Id = subId++, Name = $"{cat.Name} Sub {s}", CategoryId = cat.Id });
        }
        db.SubCategories.AddRange(subCategories);

        var products = new List<Product>();
        for (var p = 1; p <= 120; p++)
        {
            var cat = categories[rnd.Next(categories.Count)];
            var sub = subCategories.Where(x => x.CategoryId == cat.Id).ElementAt(rnd.Next(6));
            products.Add(new Product
            {
                Id = p,
                Name = $"{cat.Name} Item {p:000}",
                CategoryId = cat.Id,
                SubCategoryId = sub.Id,
                Price = Math.Round((decimal)(rnd.NextDouble() * 500 + 5), 2),
                CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(0, 365)),
                Status = Statuses[rnd.Next(Statuses.Length)]
            });
        }
        db.Products.AddRange(products);

        db.SaveChanges();
    }
}
