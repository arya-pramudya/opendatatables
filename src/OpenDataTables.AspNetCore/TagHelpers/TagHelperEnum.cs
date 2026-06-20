namespace OpenDataTables.AspNetCore.TagHelpers;

/// <summary>
/// Parses a case-insensitive enum name from a tag-helper string attribute. Tag-helper attributes bound to
/// a <c>Nullable&lt;TEnum&gt;</c> are compiled by Razor as C# <em>expressions</em>, so a bare
/// <c>foo="None"</c> fails to compile; binding such attributes as <see cref="string"/> and parsing here
/// lets the markup use plain, case-insensitive enum names. Returns <c>null</c> when the attribute is unset;
/// throws a clear, value-listing error on an unknown name.
/// </summary>
internal static class TagHelperEnum
{
    public static TEnum? Parse<TEnum>(string? value, string tag, string attribute) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) && Enum.IsDefined(result))
            return result;
        throw new InvalidOperationException(
            $"<{tag}> {attribute}=\"{value}\" is not a valid {typeof(TEnum).Name}. " +
            $"Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
    }
}
