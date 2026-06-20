using Microsoft.AspNetCore.Razor.TagHelpers;
using OpenSelect2.AspNetCore.Models;

namespace OpenSelect2.AspNetCore.TagHelpers;

/// <summary>
/// A single static option inside an <c>&lt;os2-select&gt;</c>. Presence of any <c>&lt;os2-item&gt;</c>
/// turns the dropdown into a local (non-AJAX) list. Renders nothing itself — it registers the option
/// on the parent via <see cref="TagHelperContext.Items"/>.
/// </summary>
/// <remarks>Usage: <c>&lt;os2-item value="Active" text="Active" selected /&gt;</c>.</remarks>
[HtmlTargetElement("os2-item", ParentTag = "os2-select")]
public class Select2ItemTagHelper : TagHelper
{
    /// <summary>The option value (Select2 <c>id</c>; required).</summary>
    [HtmlAttributeName("value")] public string? Value { get; set; }

    /// <summary>The display text (defaults to <see cref="Value"/> when omitted).</summary>
    [HtmlAttributeName("text")] public string? Text { get; set; }

    /// <summary>Whether this option is selected.</summary>
    [HtmlAttributeName("selected")] public bool Selected { get; set; }

    /// <summary>Whether this option is disabled.</summary>
    [HtmlAttributeName("disabled")] public bool Disabled { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrEmpty(Value))
            throw new InvalidOperationException("<os2-item> requires a 'value' attribute.");

        if (context.Items.TryGetValue(typeof(Select2TagHelper.ItemBag), out var bag)
            && bag is List<Select2ListItem> items)
        {
            items.Add(new Select2ListItem
            {
                Id = Value,
                Text = Text ?? Value,
                Selected = Selected,
                Disabled = Disabled
            });
        }

        output.SuppressOutput(); // the option is rendered by the parent's ViewComponent, not here
    }
}
