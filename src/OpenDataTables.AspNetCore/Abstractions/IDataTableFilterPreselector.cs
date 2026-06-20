using Microsoft.AspNetCore.Http;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Abstractions;

/// <summary>
/// Host-implemented hook that pre-populates a DataTable's filters from server-side context (e.g. the
/// current user's claims). The <c>DataTable</c> ViewComponent resolves it null-safely, so registration
/// is optional.
/// </summary>
/// <remarks>
/// A typical implementation iterates <see cref="DataTableViewModel.Columns"/>, switches on each
/// <see cref="DataTableColumnViewModel.Data"/> (or a column key of your choosing), adds entries to
/// <see cref="DataTableColumnViewModel.SelectedItems"/>, and sets
/// <see cref="DataTableColumnViewModel.IsDisabled"/> when the value must not be changed — these are the
/// properties the filter card actually renders.
/// </remarks>
public interface IDataTableFilterPreselector
{
    /// <summary>Apply filter preselection/disabled state to <paramref name="config"/> for the current request.</summary>
    void Apply(DataTableViewModel config, HttpContext context);
}
