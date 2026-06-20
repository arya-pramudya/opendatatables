using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OpenDataTables.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Extensions;

/// <summary>
/// Server-side sorting and paging helpers that turn a query plus a <see cref="DataTableQueryViewModel"/>
/// into a <see cref="DataTableResponseViewModel{T}"/>. An explicit <c>columnSelectors</c> map takes
/// priority; otherwise the sort column is resolved by reflection (with an implicit <c>Id</c> fallback).
/// </summary>
public static class DataTableQueryExtensions
{
    // Compiled in-memory sort accessors, keyed by (type, resolved property name) so the cache stays
    // bounded by the type's property count rather than by arbitrary client-supplied sort names.
    private static readonly ConcurrentDictionary<(Type Type, string Property), Delegate> SelectorCache = new();

    // Reflected property lists, cached per type so sort resolution doesn't call GetProperties() per request.
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private static PropertyInfo[] GetProps<T>() =>
        PropertyCache.GetOrAdd(typeof(T), static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    /// <summary>
    /// Applies sorting and paging to an <see cref="IQueryable{T}"/> (database-side) and returns the
    /// DataTables response. Uses EF Core's async materializers.
    /// </summary>
    public static async Task<DataTableResponseViewModel<T>> ToDataTableResponseAsync<T>(
        this IQueryable<T> source,
        DataTableQueryViewModel query,
        int recordsTotal,
        IDictionary<string, Expression<Func<T, object>>>? columnSelectors = null,
        string? defaultSortColumn = null,
        string? defaultSortDirection = null,
        int? recordsFiltered = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);
        query.NormalizeSort();

        var ordered = ApplyFullSort(source, query, columnSelectors, defaultSortColumn, defaultSortDirection);

        var finalRecordsFiltered = recordsFiltered ?? await ordered.CountAsync(cancellationToken).ConfigureAwait(false);

        // DataTables sends Length = -1 for "All" — return every row from Start instead of Take(-1) (empty).
        var paged = ordered.Skip(query.Start);
        if (query.Length >= 0) paged = paged.Take(query.Length);
        var pagedData = await paged.ToListAsync(cancellationToken).ConfigureAwait(false);

        return new DataTableResponseViewModel<T>
        {
            draw = query.Draw,
            recordsTotal = recordsTotal,
            recordsFiltered = finalRecordsFiltered,
            data = pagedData
        };
    }

    /// <summary>
    /// Applies sorting and paging to an in-memory <see cref="IEnumerable{T}"/> (already filtered/projected).
    /// </summary>
    public static DataTableResponseViewModel<T> ToDataTableResponse<T>(
        this IEnumerable<T> source,
        DataTableQueryViewModel query,
        int recordsTotal,
        IDictionary<string, Func<T, object?>>? columnSelectors = null,
        string? defaultSortColumn = null,
        string? defaultSortDirection = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);
        query.NormalizeSort();

        var list = source.ToList();
        var (sortName, sortDir) = ResolveSortPlan(query, defaultSortColumn, defaultSortDirection);
        var orderingFunc = ResolveSelector(sortName, columnSelectors);

        if (orderingFunc != null)
        {
            var orderedEnum = IsAscending(sortDir)
                ? list.OrderBy(orderingFunc)
                : list.OrderByDescending(orderingFunc);

            // B5: apply secondary ThenBy sorts from the multi-column sort list.
            // Skip(1) because SortOrders[0] is the primary OrderBy above (a no-op for 0/1 entries).
            foreach (var sd in query.SortOrders.Skip(1))
            {
                // Skip unknown columns rather than silently falling back to the Id property.
                if (!TryResolveThenSelector(sd.Column, columnSelectors, out var thenFn)) continue;
                orderedEnum = IsAscending(sd.Direction)
                    ? orderedEnum.ThenBy(thenFn)
                    : orderedEnum.ThenByDescending(thenFn);
            }

            list = orderedEnum.ToList();
        }

        var recordsFiltered = list.Count;

        // DataTables sends Length = -1 for "All" — return every row from Start instead of Take(-1) (empty).
        IEnumerable<T> paged = list.Skip(query.Start);
        if (query.Length >= 0) paged = paged.Take(query.Length);
        var pagedData = paged.ToList();

        return new DataTableResponseViewModel<T>
        {
            draw = query.Draw,
            recordsTotal = recordsTotal,
            recordsFiltered = recordsFiltered,
            data = pagedData
        };
    }

    /// <summary>
    /// Applies ordering (no paging) to an <see cref="IQueryable{T}"/> using the current DataTables sort —
    /// intended for export flows that keep the UI sort but return all rows. Honors the full multi-column
    /// (B5) sort, so an export matches the ordering shown in a shift-sorted grid.
    /// </summary>
    public static IOrderedQueryable<T> ApplySorting<T>(
        this IQueryable<T> source,
        DataTableQueryViewModel query,
        IDictionary<string, Expression<Func<T, object>>>? columnSelectors = null,
        string? defaultSortColumn = null,
        string? defaultSortDirection = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);
        query.NormalizeSort();

        return ApplyFullSort(source, query, columnSelectors, defaultSortColumn, defaultSortDirection);
    }

    // Applies the primary OrderBy plus every secondary ThenBy from the multi-column sort list. Shared by
    // the async paging path and the export (ApplySorting) path so both order rows identically.
    private static IOrderedQueryable<T> ApplyFullSort<T>(
        IQueryable<T> source,
        DataTableQueryViewModel query,
        IDictionary<string, Expression<Func<T, object>>>? columnSelectors,
        string? defaultSortColumn,
        string? defaultSortDirection)
    {
        var (sortName, sortDir) = ResolveSortPlan(query, defaultSortColumn, defaultSortDirection);
        var orderingExpr = ResolveExpression(sortName, columnSelectors);
        var ordered = IsAscending(sortDir) ? source.OrderBy(orderingExpr) : source.OrderByDescending(orderingExpr);

        // B5: apply secondary ThenBy sorts from the multi-column sort list.
        // Skip(1) because SortOrders[0] is the primary OrderBy above (a no-op for 0/1 entries).
        foreach (var sd in query.SortOrders.Skip(1))
        {
            // Skip unknown columns rather than silently falling back to the Id property.
            if (!TryResolveThenExpression(sd.Column, columnSelectors, out var thenExpr)) continue;
            ordered = IsAscending(sd.Direction) ? ordered.ThenBy(thenExpr) : ordered.ThenByDescending(thenExpr);
        }

        return ordered;
    }

    private static bool IsAscending(string sortDir) => string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

    // Resolves the effective sort column name and direction from raw DataTables query params.
    private static (string sortName, string sortDir) ResolveSortPlan(
        DataTableQueryViewModel query,
        string? defaultSortColumn,
        string? defaultSortDirection)
    {
        // NormalizeSort has already reconciled SortOrders[0] and the scalar SortColumnName/SortDirection,
        // so they agree here; read the canonical SortOrders[0] (falling back to the scalar only if a caller
        // invoked ResolveSortPlan without normalizing).
        var primary = query.SortOrders.Count > 0 ? query.SortOrders[0] : null;
        var rawName = primary != null && !string.IsNullOrWhiteSpace(primary.Column)
            ? primary.Column
            : (query.SortColumnName ?? string.Empty);
        var sortName = rawName.Trim();

        // Treat a sort on the implicit ID column (often the hidden primary key, which DataTables echoes as
        // the default order) as "no explicit sort" and fall back to the provided default column — unless
        // the default itself is ID. Skip this for a genuine multi-column sort (2+ entries), where an Id
        // primary is an explicit user choice that must be honored rather than rewritten to the default.
        if (query.SortOrders.Count <= 1
            && string.Equals(sortName, "id", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(defaultSortColumn, "id", StringComparison.OrdinalIgnoreCase))
        {
            sortName = string.Empty;
        }

        var usingDefaultSort = false;
        if (string.IsNullOrEmpty(sortName) && !string.IsNullOrEmpty(defaultSortColumn))
        {
            sortName = defaultSortColumn;
            usingDefaultSort = true;
        }

        // Prefer SortOrders[0].Direction for the same reason as the column name above.
        var rawDir = primary != null && !string.IsNullOrWhiteSpace(primary.Direction)
            ? primary.Direction
            : (query.SortDirection ?? string.Empty);
        var sortDir = rawDir.Trim();

        // Apply the configured default direction only when we are actually falling back to the default
        // column, or when the request carried no direction at all. An explicit direction on the default
        // column (e.g. a stateSave-restored or pre-seeded descending sort) is the user's choice and must
        // be honored — do not rewrite it to defaultSortDirection just because it is the first draw.
        if (usingDefaultSort && !string.IsNullOrWhiteSpace(defaultSortDirection))
            sortDir = defaultSortDirection.Trim();
        else if (string.IsNullOrWhiteSpace(sortDir) && !string.IsNullOrWhiteSpace(defaultSortDirection))
            sortDir = defaultSortDirection.Trim();

        return (sortName, sortDir);
    }

    private static PropertyInfo? FindProperty<T>(string sortName)
    {
        var props = GetProps<T>();

        if (!string.IsNullOrWhiteSpace(sortName))
        {
            var match = props.FirstOrDefault(p => string.Equals(p.Name, sortName, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        return props.FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase))
            ?? props.FirstOrDefault();
    }

    // Exact (case-insensitive) property match with no Id/first-property fallback — used by the
    // secondary-sort helpers, which must skip unknown columns rather than fall back to Id.
    private static PropertyInfo? FindPropertyExact<T>(string name) =>
        GetProps<T>().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    // Looks up a selector by exact key, then case-insensitively. Shared by the IQueryable and IEnumerable paths.
    private static bool TryResolve<TVal>(IDictionary<string, TVal>? map, string? name, out TVal value)
    {
        value = default!;
        if (string.IsNullOrEmpty(name) || map is null || map.Count == 0) return false;
        if (map.TryGetValue(name, out value!)) return true;
        var key = map.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
        if (key == null) return false;
        value = map[key];
        return true;
    }

    private static Expression<Func<T, object>> ResolveExpression<T>(
        string sortName,
        IDictionary<string, Expression<Func<T, object>>>? columnSelectors)
        => TryResolve(columnSelectors, sortName, out var expr) ? expr : BuildPropertyExpression<T>(sortName);

    // Mirrors ResolveExpression (the IQueryable path): an explicit selector wins, else resolve by
    // reflection. No defaultSortColumn fallback here — ResolveSortPlan has already substituted the default
    // into sortName when the request had no explicit column, so falling back again would wrongly sort an
    // explicitly-requested-but-unmapped real property by the default column instead.
    private static Func<T, object?>? ResolveSelector<T>(
        string sortName,
        IDictionary<string, Func<T, object?>>? columnSelectors)
    {
        if (TryResolve(columnSelectors, sortName, out var fn)) return fn;
        return BuildPropertySelector<T>(sortName);
    }

    private static Func<T, object?> BuildPropertySelector<T>(string sortName)
    {
        var prop = FindProperty<T>(sortName);
        if (prop == null) return x => x!;
        return BuildPropertySelector<T>(prop);
    }

    private static Func<T, object?> BuildPropertySelector<T>(PropertyInfo prop)
    {
        // Compile the accessor once per (type, property) instead of reflecting per row inside OrderBy.
        return (Func<T, object?>)SelectorCache.GetOrAdd((typeof(T), prop.Name), static (_, p) =>
        {
            var param = Expression.Parameter(typeof(T), "x");
            // (object?)x.Prop — do NOT coalesce null to "". OrderBy/ThenBy handle null keys natively
            // (nulls sort first); coalescing to "" mixes string and value-type keys and makes
            // Comparer<object> throw on nullable value-type columns (int?/DateTime?/etc.) that contain nulls.
            var body = Expression.Convert(Expression.Property(param, p), typeof(object));
            return Expression.Lambda<Func<T, object?>>(body, param).Compile();
        }, prop);
    }

    private static Expression<Func<T, object>> BuildPropertyExpression<T>(string sortName)
    {
        var prop = FindProperty<T>(sortName);
        if (prop != null) return BuildPropertyExpression<T>(prop);

        var param = Expression.Parameter(typeof(T), "x");
        return Expression.Lambda<Func<T, object>>(Expression.Convert(param, typeof(object)), param);
    }

    private static Expression<Func<T, object>> BuildPropertyExpression<T>(PropertyInfo prop)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.Convert(Expression.Property(param, prop), typeof(object));
        return Expression.Lambda<Func<T, object>>(body, param);
    }

    // Secondary-sort helpers: return false (skip the ThenBy) when the column name is not an explicit
    // selector key AND does not map to a real property — prevents the FindProperty Id-fallback from
    // silently injecting a ThenBy(Id) on unknown column names.

    private static bool TryResolveThenExpression<T>(
        string column,
        IDictionary<string, Expression<Func<T, object>>>? columnSelectors,
        out Expression<Func<T, object>> expr)
    {
        expr = default!;
        if (string.IsNullOrWhiteSpace(column)) return false;
        if (TryResolve(columnSelectors, column, out expr)) return true;
        var prop = FindPropertyExact<T>(column);
        if (prop == null) return false;
        expr = BuildPropertyExpression<T>(prop);
        return true;
    }

    private static bool TryResolveThenSelector<T>(
        string column,
        IDictionary<string, Func<T, object?>>? columnSelectors,
        out Func<T, object?> fn)
    {
        fn = default!;
        if (string.IsNullOrWhiteSpace(column)) return false;
        if (TryResolve(columnSelectors, column, out fn)) return true;
        var prop = FindPropertyExact<T>(column);
        if (prop == null) return false;
        fn = BuildPropertySelector<T>(prop);
        return true;
    }
}
