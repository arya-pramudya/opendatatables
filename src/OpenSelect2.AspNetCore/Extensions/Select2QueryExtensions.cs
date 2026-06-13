using Microsoft.EntityFrameworkCore;
using OpenSelect2.AspNetCore.Models;

namespace OpenSelect2.AspNetCore.Extensions;

/// <summary>
/// Paging helpers that turn an ordered sequence of <see cref="Select2ListItem"/> into a
/// <see cref="Select2Result"/>. <see cref="Select2Result.HasMore"/> is computed with the
/// take-one-extra trick (fetch <c>Limit + 1</c> rows), avoiding a second <c>Count()</c> round-trip.
/// </summary>
public static class Select2QueryExtensions
{
    /// <summary>Pages an in-memory sequence (already filtered/ordered) into a <see cref="Select2Result"/>.</summary>
    public static Select2Result ToSelect2Result(this IEnumerable<Select2ListItem> source, Select2QueryViewModel query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var (skip, take) = Paging(query);
        var page = source.Skip(skip).Take(take + 1).ToList();
        return Build(page, take);
    }

    /// <summary>Pages an <see cref="IQueryable{T}"/> (database-side) into a <see cref="Select2Result"/>.</summary>
    public static async Task<Select2Result> ToSelect2ResultAsync(
        this IQueryable<Select2ListItem> source,
        Select2QueryViewModel query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var (skip, take) = Paging(query);
        var page = await source.Skip(skip).Take(take + 1).ToListAsync(cancellationToken).ConfigureAwait(false);
        return Build(page, take);
    }

    // Upper bound so a hostile/buggy client can't request the whole table (and so Take(take + 1) and
    // (page - 1) * take can't overflow Int32).
    private const int MaxLimit = 1000;

    private static (int skip, int take) Paging(Select2QueryViewModel query)
    {
        var take = query.Limit > 0 ? Math.Min(query.Limit, MaxLimit) : 10;
        var page = query.Page > 0 ? query.Page : 1;
        var skip = (long)(page - 1) * take;
        return ((int)Math.Min(skip, int.MaxValue), take);
    }

    private static Select2Result Build(List<Select2ListItem> page, int take)
    {
        var hasMore = page.Count > take;
        if (hasMore)
            page.RemoveAt(page.Count - 1);

        return new Select2Result { Items = page, HasMore = hasMore };
    }
}
