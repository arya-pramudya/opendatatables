namespace SampleApp.Data;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class SubCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public int SubCategoryId { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Active";
}
