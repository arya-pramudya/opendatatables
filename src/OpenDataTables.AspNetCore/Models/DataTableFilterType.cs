namespace OpenDataTables.AspNetCore.Models;

/// <summary>Defines the type of filter to use for a DataTable column.</summary>
public enum DataTableFilterType
{
    /// <summary>No filter for this column.</summary>
    None,

    /// <summary>Text input filter for free-form text search.</summary>
    Text,

    /// <summary>Date picker filter for date fields.</summary>
    Date,

    /// <summary>Single-select dropdown with options loaded via AJAX.</summary>
    Select,

    /// <summary>Range filter with min/max values.</summary>
    Range,

    /// <summary>Multi-select dropdown with options loaded via AJAX.</summary>
    SelectMultiple,

    /// <summary>Single-select dropdown with static options provided in the model.</summary>
    SelectStatic
}
