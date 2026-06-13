using OpenSelect2.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Models;

/// <summary>Resolved filter configuration for a single DataTable column (consumed by the client JS).</summary>
public class DataTableColumnFilterConfig
{
    /// <summary>The column this filter applies to.</summary>
    public required string Column { get; set; }

    /// <summary>The filter input type.</summary>
    public DataTableFilterType Type { get; set; }

    /// <summary>Where the filter is placed.</summary>
    public DataTableFilterPlacement Placement { get; set; }

    /// <summary>AJAX options endpoint for select filters.</summary>
    public string OptionsUrl { get; set; } = "";

    /// <summary>Field name for the option value in AJAX results.</summary>
    public string OptionValueField { get; set; } = "value";

    /// <summary>Field name for the option text in AJAX results.</summary>
    public string OptionTextField { get; set; } = "text";

    /// <summary>Static options for static-select filters.</summary>
    public List<Select2ListItem>? StaticOptions { get; set; }

    /// <summary>Render this filter disabled.</summary>
    public bool IsDisabled { get; set; }

    /// <summary>Single parent column this filter cascades from.</summary>
    public string? ParentFilterColumn { get; set; }

    /// <summary>Multiple parent columns this filter cascades from.</summary>
    public List<string>? ParentFilterColumns { get; set; }

    /// <summary>Whether this filter participates in a cascade.</summary>
    public bool IsCascade { get; set; }

    /// <summary>Extra CSS classes for the inline filter input.</summary>
    public string FilterClass { get; set; } = "";

    /// <summary>Inline style for the inline filter input.</summary>
    public string FilterStyle { get; set; } = "";

    /// <summary>Extra CSS classes for the top filter input.</summary>
    public string FilterTopClass { get; set; } = "";

    /// <summary>Inline style for the top filter input.</summary>
    public string FilterTopStyle { get; set; } = "";

    /// <summary>Optional label/placeholder text; falls back to the column title when null.</summary>
    public string? FilterTitle { get; set; }

    /// <summary>
    /// Extra parameters for this filter's AJAX requests. Values may be literals or tokens like
    /// <c>{filter:columnName}</c>.
    /// </summary>
    public Dictionary<string, string>? AdditionalParams { get; set; }

    /// <summary>
    /// Opaque key handed to the host's <see cref="Abstractions.IDataTableFilterPreselector"/> (if registered)
    /// to populate <see cref="SelectedItems"/> / disabled state from server-side context. Replaces the old
    /// domain-specific <c>PreSelectCurrentUser*</c> flags.
    /// </summary>
    public string? PreselectKey { get; set; }

    /// <summary>Pre-selected items for the filter (set by a preselector or by the host directly).</summary>
    public List<Select2ListItem>? SelectedItems { get; set; }

    /// <summary>Enable child filters even when this parent is disabled but has a pre-selected value.</summary>
    public bool EnableChildrenIfPreSelected { get; set; }
}
