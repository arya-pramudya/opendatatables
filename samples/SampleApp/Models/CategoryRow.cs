namespace SampleApp.Models;

/// <summary>
/// Parent-row shape for the nested ("table inside a table") demo: one row per category, each expandable
/// to its products. Property names become the JSON column keys (matching each column's <c>Data</c>).
/// </summary>
public class CategoryRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ProductCount { get; set; }
    public decimal AvgPrice { get; set; }
}
