using OpenSelect2.AspNetCore.Models;

namespace OpenDataTables.AspNetCore.Models;

/// <summary>Configuration for an editable DataTable column.</summary>
public class DataTableColumnEditorConfig
{
    /// <summary>The data/save key for the column.</summary>
    public required string Column { get; set; }

    /// <summary>The editor control type.</summary>
    public DataTableEditorType EditorType { get; set; } = DataTableEditorType.Text;

    /// <summary>AJAX options endpoint (select editors).</summary>
    public string OptionsUrl { get; set; } = string.Empty;

    /// <summary>AJAX endpoint (alias of <see cref="OptionsUrl"/> for select editors).</summary>
    public string AjaxUrl { get; set; } = string.Empty;

    /// <summary>Field name for the option value in AJAX results.</summary>
    public string OptionValueField { get; set; } = "id";

    /// <summary>Field name for the option text in AJAX results.</summary>
    public string OptionTextField { get; set; } = "text";

    /// <summary>Static options for static-select editors.</summary>
    public List<Select2ListItem>? StaticOptions { get; set; }

    /// <summary>Parent column to cascade from.</summary>
    public string? ParentColumn { get; set; }

    /// <summary>Literal parent value.</summary>
    public string? ParentValue { get; set; }

    /// <summary>CSS selector whose value supplies the parent value at edit time.</summary>
    public string? ParentValueSelector { get; set; }

    /// <summary>Query parameter name used to send the parent value.</summary>
    public string? ParentParamName { get; set; } = "parentValue";

    /// <summary>Extra AJAX parameters.</summary>
    public Dictionary<string, string>? AdditionalParams { get; set; }

    /// <summary>Optional dropdown parent selector for the editor's Select2.</summary>
    public string? DropdownParentSelector { get; set; }

    /// <summary>
    /// When the visible column data key differs from the save field (<see cref="Column"/>), set this to
    /// the display column name so the editor activates on that cell while <see cref="Column"/> is the save key.
    /// </summary>
    public string? DisplayDataColumn { get; set; }
}
