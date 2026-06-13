using Microsoft.AspNetCore.Mvc;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Components;

/// <summary>
/// Renders a configurable filter card for a DataTable from a <see cref="FilterCardViewModel"/>.
/// Usually created for you by the <c>DataTable</c> component when <see cref="DataTableFilterUiMode.FilterCard"/>
/// is selected; can also be invoked directly with <c>@await Component.InvokeAsync("FilterCard", model)</c>.
/// </summary>
public class FilterCardViewComponent : ViewComponent
{
    /// <summary>Renders the component.</summary>
    public IViewComponentResult Invoke(FilterCardViewModel config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return View("Default", config);
    }
}
