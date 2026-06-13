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

        var (sortName, sortDir) = ResolveSortPlan(query, defaultSortColumn, defaultSortDirection);
        var orderingExpr = ResolveExpression(sortName, columnSelectors);

        var ordered = IsAscending(sortDir) ? source.OrderBy(orderingExpr) : source.OrderByDescending(orderingExpr);

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

        var list = source.ToList();
        var (sortName, sortDir) = ResolveSortPlan(query, defaultSortColumn, defaultSortDirection);
        var orderingFunc = ResolveSelector(sortName, columnSelectors, defaultSortColumn);

        if (orderingFunc != null)
        {
            list = IsAscending(sortDir)
                ? list.OrderBy(orderingFunc).ToList()
                : list.OrderByDescending(orderingFunc).ToList();
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
    /// intended for export flows that keep the UI sort but return all rows.
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

        var (sortName, sortDir) = ResolveSortPlan(query, defaultSortColumn, defaultSortDirection);
        var orderingExpr = ResolveExpression(sortName, columnSelectors);

        return IsAscending(sortDir) ? source.OrderBy(orderingExpr) : source.OrderByDescending(orderingExpr);
    }

    private static bool IsAscending(string sortDir) => string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

    // Resolves the effective sort column name and direction from raw DataTables query params.
    private static (string sortName, string sortDir) ResolveSortPlan(
        DataTableQueryViewModel query,
        string? defaultSortColumn,
        string? defaultSortDirection)
    {
        var sortName = (query.SortColumnName ?? string.Empty).Trim();

        // Treat an initial sort on the implicit ID column (often the hidden primary key) as "no explicit
        // sort" and fall back to the provided default column — unless the default itself is ID.
        if (string.Equals(sortName, "id", StringComparison.OrdinalIgnoreCase)
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

        var isInitialDraw = !int.TryParse(query.Draw, out var drawNum) || drawNum <= 1;
        var sortDir = (query.SortDirection ?? string.Empty).Trim();

        if (usingDefaultSort && !string.IsNullOrWhiteSpace(defaultSortDirection))
            sortDir = defaultSortDirection.Trim();
        else if (isInitialDraw
            && !string.IsNullOrWhiteSpace(defaultSortDirection)
            && !string.IsNullOrWhiteSpace(defaultSortColumn)
            && string.Equals(sortName, defaultSortColumn, StringComparison.OrdinalIgnoreCase))
            sortDir = defaultSortDirection.Trim();
        else if (string.IsNullOrWhiteSpace(sortDir) && !string.IsNullOrWhiteSpace(defaultSortDirection))
            sortDir = defaultSortDirection.Trim();

        return (sortName, sortDir);
    }

    private static PropertyInfo? FindProperty<T>(string sortName)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (!string.IsNullOrWhiteSpace(sortName))
        {
            var match = props.FirstOrDefault(p => string.Equals(p.Name, sortName, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        return props.FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase))
            ?? props.FirstOrDefault();
    }

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

    private static Func<T, object?>? ResolveSelector<T>(
        string sortName,
        IDictionary<string, Func<T, object?>>? columnSelectors,
        string? defaultSortColumn)
    {
        if (TryResolve(columnSelectors, sortName, out var fn)) return fn;
        if (TryResolve(columnSelectors, defaultSortColumn, out var defaultFn)) return defaultFn;
        return BuildPropertySelector<T>(sortName);
    }

    private static Func<T, object?> BuildPropertySelector<T>(string sortName)
    {
        var prop = FindProperty<T>(sortName);
        if (prop == null) return x => x!;

        // Compile the accessor once per (type, property) instead of reflecting per row inside OrderBy.
        return (Func<T, object?>)SelectorCache.GetOrAdd((typeof(T), prop.Name), static (_, p) =>
        {
            var param = Expression.Parameter(typeof(T), "x");
            // (object)x.Prop ?? "" — preserves the prior null-coalescing so null keys still sort consistently.
            var body = Expression.Coalesce(
                Expression.Convert(Expression.Property(param, p), typeof(object)),
                Expression.Constant(string.Empty));
            return Expression.Lambda<Func<T, object?>>(body, param).Compile();
        }, prop);
    }

    private static Expression<Func<T, object>> BuildPropertyExpression<T>(string sortName)
    {
        var prop = FindProperty<T>(sortName);
        var param = Expression.Parameter(typeof(T), "x");

        if (prop == null)
            return Expression.Lambda<Func<T, object>>(Expression.Convert(param, typeof(object)), param);

        var body = Expression.Convert(Expression.Property(param, prop), typeof(object));
        return Expression.Lambda<Func<T, object>>(body, param);
    }
}
