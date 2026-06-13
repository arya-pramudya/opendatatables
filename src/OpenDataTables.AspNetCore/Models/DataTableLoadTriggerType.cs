namespace OpenDataTables.AspNetCore.Models;

/// <summary>Defines when a DataTable should load its data.</summary>
public enum DataTableLoadTriggerType
{
    /// <summary>Load data immediately when the component is initialized (default).</summary>
    Immediate = 0,

    /// <summary>Load data when a specific element is clicked.</summary>
    OnClick = 1,

    /// <summary>Load data when a modal is shown.</summary>
    Modal = 2,

    /// <summary>Load data when a custom event is triggered.</summary>
    Custom = 3
}
