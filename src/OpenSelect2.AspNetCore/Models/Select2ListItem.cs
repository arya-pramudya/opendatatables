using System.Text.Json.Serialization;

namespace OpenSelect2.AspNetCore.Models;

/// <summary>
/// A single option in a Select2 dropdown. This is the wire shape returned by AJAX endpoints
/// (serialized as <c>{ id, text, ... }</c>) and the type used for pre-selected items.
/// </summary>
/// <remarks>
/// The type is deliberately generic. To attach domain-specific metadata (which Select2 templates
/// can read), put it in <see cref="Extra"/> — those entries are flattened to top-level JSON
/// properties on the wire via <see cref="JsonExtensionDataAttribute"/>.
/// </remarks>
public class Select2ListItem
{
    /// <summary>The option value (Select2 <c>id</c>).</summary>
    public required string Id { get; set; }

    /// <summary>The display text (Select2 <c>text</c>).</summary>
    public required string Text { get; set; }

    /// <summary>Whether this item is selected. Server-rendered; defaults to <c>false</c>.</summary>
    public bool Selected { get; set; }

    /// <summary>Whether this individual option is disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Arbitrary extra metadata flattened to top-level JSON properties on the wire
    /// (e.g. <c>{ "id": "1", "text": "Cash", "currency": "USD" }</c>). Keys are emitted verbatim,
    /// so provide them in the casing the client expects.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extra { get; set; }
}
