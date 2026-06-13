using Microsoft.AspNetCore.Http;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Abstractions;

/// <summary>
/// Host-implemented hook that pre-populates a DataTable's filters from server-side context (e.g. the
/// current user's claims). The <c>DataTable</c> ViewComponent resolves it null-safely, so registration
/// is optional.
/// </summary>
/// <remarks>
/// A typical implementation iterates <see cref="DataTableViewModel.FilterConfigs"/>, switches on each
/// <see cref="DataTableColumnFilterConfig.PreselectKey"/>, adds entries to
/// <see cref="DataTableColumnFilterConfig.SelectedItems"/>, and sets
/// <see cref="DataTableColumnFilterConfig.IsDisabled"/> when the value must not be changed.
/// </remarks>
public interface IDataTableFilterPreselector
{
    /// <summary>Apply filter preselection/disabled state to <paramref name="config"/> for the current request.</summary>
    void Apply(DataTableViewModel config, HttpContext context);
}
