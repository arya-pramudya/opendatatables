namespace OpenDataTables.AspNetCore.Models;

/// <summary>
/// Thrown when a <see cref="DataTableViewModel"/> requests a feature that is configured but not yet
/// supported (e.g. an unimplemented <see cref="DataTableFilterUiMode"/> or a
/// <see cref="DataTableFilterType.Range"/> filter). Carries the offending property name so the host can
/// pinpoint the misconfiguration. Prefer calling <see cref="DataTableViewModel.Validate"/> early (at
/// configuration time) to surface this before the Razor render pipeline.
/// </summary>
public sealed class DataTableConfigurationException : Exception
{
    /// <summary>The configuration property whose value is unsupported.</summary>
    public string PropertyName { get; }

    /// <summary>Creates the exception for the given property with an actionable message.</summary>
    public DataTableConfigurationException(string propertyName, string message) : base(message)
        => PropertyName = propertyName;
}
