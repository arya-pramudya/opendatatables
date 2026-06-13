using Microsoft.AspNetCore.Http;
using OpenSelect2.AspNetCore.Models;

namespace OpenSelect2.AspNetCore.Abstractions;

/// <summary>
/// Host-implemented hook that pre-populates a <see cref="Select2ViewModel"/> from server-side context
/// (e.g. the current user's claims) when the model carries a non-empty
/// <see cref="Select2ViewModel.PreselectKey"/>.
/// </summary>
/// <remarks>
/// Register an implementation in DI (e.g. <c>services.AddScoped&lt;ISelect2Preselector, MyPreselector&gt;()</c>).
/// The <c>Select2</c> ViewComponent resolves it null-safely, so registration is optional. A typical
/// implementation switches on <see cref="Select2ViewModel.PreselectKey"/>, adds entries to
/// <see cref="Select2ViewModel.SelectedItems"/>, and sets <see cref="Select2ViewModel.IsDisabled"/> /
/// <see cref="Select2ViewModel.ForceDisabled"/> when the value must not be changed.
/// </remarks>
public interface ISelect2Preselector
{
    /// <summary>Apply preselection/disabled state to <paramref name="model"/> for the current request.</summary>
    void Apply(Select2ViewModel model, HttpContext context);
}
