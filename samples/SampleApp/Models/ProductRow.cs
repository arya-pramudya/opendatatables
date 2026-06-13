namespace SampleApp.Models;

/// <summary>The row shape returned to the DataTable. Property names become the JSON column keys.</summary>
public class ProductRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}
