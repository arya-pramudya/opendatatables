using System.Reflection;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Extensions;

/// <summary>Builds a <see cref="FilterCardViewModel"/> from a <see cref="DataTableViewModel"/>.</summary>
public static class DataTableFilterCardExtensions
{
    /// <summary>
    /// Creates a <see cref="FilterCardViewModel"/> mirroring the table's columns so the filter card and
    /// table stay consistent.
    /// </summary>
    public static FilterCardViewModel ToFilterCard(
        this DataTableViewModel dataTableModel,
        string? title = null,
        int? columnsPerRow = null,
        string customColumnWidthClass = "",
        Dictionary<string, string>? customParameters = null)
    {
        ArgumentNullException.ThrowIfNull(dataTableModel);

        var effectiveColumnsPerRow = columnsPerRow ?? GetFilterColumnsPerRow(dataTableModel) ?? 3;

        return new FilterCardViewModel
        {
            FormId = $"{dataTableModel.TableId}-filter-form",
            TableId = dataTableModel.TableId,
            Title = title ?? "Filter Data",
            Columns = dataTableModel.Columns,
            ColumnsPerRow = effectiveColumnsPerRow,
            CustomColumnWidthClass = customColumnWidthClass,
            CustomParameters = customParameters ?? dataTableModel.CustomParameters
        };
    }

    // Lets a host subclass of DataTableViewModel expose a FilterColumnsPerRow property without the
    // package depending on it.
    private static int? GetFilterColumnsPerRow(DataTableViewModel model)
    {
        PropertyInfo? property = model.GetType().GetProperty("FilterColumnsPerRow");
        return property != null ? (int?)property.GetValue(model) : null;
    }
}
