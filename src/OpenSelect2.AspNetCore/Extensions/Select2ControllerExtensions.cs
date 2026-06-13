using Microsoft.AspNetCore.Mvc;
using OpenSelect2.AspNetCore.Models;

namespace OpenSelect2.AspNetCore.Extensions;

/// <summary>
/// Controller helpers that emit the Select2 wire response <c>{ items, hasMore }</c>.
/// </summary>
public static class Select2ControllerExtensions
{
    /// <summary>Returns a Select2 JSON response from a <see cref="Models.Select2Result"/>.</summary>
    public static JsonResult Select2Result(this ControllerBase controller, Select2Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new JsonResult(new { items = result.Items, hasMore = result.HasMore });
    }

    /// <summary>Returns a Select2 JSON response from a page of items and a <paramref name="hasMore"/> flag.</summary>
    public static JsonResult Select2Result(
        this ControllerBase controller,
        IEnumerable<Select2ListItem> items,
        bool hasMore = false)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new JsonResult(new { items, hasMore });
    }
}
