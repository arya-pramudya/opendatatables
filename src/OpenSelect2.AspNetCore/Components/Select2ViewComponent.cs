using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenSelect2.AspNetCore.Abstractions;
using OpenSelect2.AspNetCore.Models;

namespace OpenSelect2.AspNetCore.Components;

/// <summary>
/// Renders an AJAX-driven Select2 dropdown from a <see cref="Select2ViewModel"/>.
/// Invoke with <c>@await Component.InvokeAsync("Select2", model)</c>.
/// </summary>
public class Select2ViewComponent : ViewComponent
{
    /// <summary>Renders the component.</summary>
    public IViewComponentResult Invoke(Select2ViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Generic, host-implemented preselection hook (replaces the old claim-based logic).
        if (!string.IsNullOrEmpty(model.PreselectKey))
        {
            var preselector = HttpContext.RequestServices.GetService<ISelect2Preselector>();
            preselector?.Apply(model, HttpContext);
        }

        return View("Default", model);
    }
}
