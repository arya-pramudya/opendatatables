namespace OpenSelect2.AspNetCore.Models;

/// <summary>
/// The query contract the Select2 client sends to AJAX endpoints. Bind it directly in your action
/// instead of declaring loose <c>searchTerm</c>/<c>page</c>/<c>limit</c> parameters.
/// </summary>
public class Select2QueryViewModel
{
    /// <summary>
    /// The user's search text. Always at least an empty string — the setter coerces null to <c>""</c> so
    /// it stays non-null even though MVC model binding converts an empty query value (<c>searchTerm=</c>)
    /// to null by default. Lets endpoints safely write <c>query.SearchTerm == "" || x.Name.Contains(query.SearchTerm)</c>.
    /// </summary>
    public string SearchTerm
    {
        get => _searchTerm;
        set => _searchTerm = value ?? "";
    }

    private string _searchTerm = "";

    /// <summary>1-based page index for infinite scroll.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Page size requested by the client.</summary>
    public int Limit { get; set; } = 10;

    /// <summary>For cascading dropdowns: the current value of the parent control, if any.</summary>
    public string? ParentValue { get; set; }
}
