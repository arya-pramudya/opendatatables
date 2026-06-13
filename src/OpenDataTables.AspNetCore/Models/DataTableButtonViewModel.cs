namespace OpenDataTables.AspNetCore.Models;

/// <summary>Configuration for a custom button rendered in a DataTable.</summary>
public class DataTableButtonViewModel
{
    /// <summary>Unique button id (auto-generated if not set).</summary>
    public string Id { get; set; } = $"btn_{Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>Button text.</summary>
    public required string Text { get; set; }

    /// <summary>CSS class(es) for the button (e.g. <c>"btn-primary"</c>).</summary>
    public string CssClass { get; set; } = "btn-secondary";

    /// <summary>JS handler name to call on click. For row buttons, use <c>ROW_ID</c> as the row id placeholder.</summary>
    public required string OnClick { get; set; }

    /// <summary>Optional icon class (e.g. <c>"fas fa-plus"</c>).</summary>
    public string? Icon { get; set; }

    /// <summary>Placement: <c>"top"</c> (above the table) or <c>"row"</c> (in the action cell).</summary>
    public string Placement { get; set; } = "row";

    /// <summary>Optional inline style.</summary>
    public string? Style { get; set; }

    /// <summary>Optional tooltip title.</summary>
    public string? Title { get; set; }
}
