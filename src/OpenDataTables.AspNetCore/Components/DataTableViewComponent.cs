using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenDataTables.AspNetCore.Abstractions;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Components;

/// <summary>
/// Renders a server-side DataTable from a <see cref="DataTableViewModel"/>.
/// Invoke with <c>@await Component.InvokeAsync("DataTable", config)</c>.
/// </summary>
public class DataTableViewComponent : ViewComponent
{
    /// <summary>Renders the component.</summary>
    public IViewComponentResult Invoke(DataTableViewModel config)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Guard before any use below derefs Columns (the view, ToFilterCard).
        config.Columns ??= new List<DataTableColumnViewModel>();

        // Reject unsupported feature combinations with a clear, typed, property-named error. Hosts can call
        // config.Validate() earlier (at configuration time) to surface this before the render pipeline.
        config.Validate();

        if (string.IsNullOrWhiteSpace(config.TableId))
            config.TableId = $"dt_{Guid.NewGuid().ToString("N")[..8]}";

        // Generic, host-implemented filter preselection (replaces the old claim-based logic).
        var preselector = HttpContext.RequestServices.GetService<IDataTableFilterPreselector>();
        preselector?.Apply(config, HttpContext);

        return View("Default", config);
    }
}
