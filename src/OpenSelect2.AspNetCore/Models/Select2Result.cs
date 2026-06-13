namespace OpenSelect2.AspNetCore.Models;

/// <summary>
/// The result of a Select2 AJAX query: a page of <see cref="Items"/> plus a <see cref="HasMore"/>
/// flag that drives Select2's infinite-scroll paging. Serializes to <c>{ items, hasMore }</c>.
/// </summary>
public class Select2Result
{
    /// <summary>The items for the current page.</summary>
    public List<Select2ListItem> Items { get; set; } = new();

    /// <summary>True when more pages are available (Select2 keeps requesting the next page).</summary>
    public bool HasMore { get; set; }
}
