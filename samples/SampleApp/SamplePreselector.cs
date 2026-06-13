using Microsoft.AspNetCore.Http;
using OpenSelect2.AspNetCore.Abstractions;
using OpenSelect2.AspNetCore.Models;

namespace SampleApp;

/// <summary>
/// Demonstrates the generic <see cref="ISelect2Preselector"/> hook. A real app would read the current
/// user's claims here; the sample just keys off a string to keep the package free of domain concepts.
/// </summary>
public class SamplePreselector : ISelect2Preselector
{
    public void Apply(Select2ViewModel model, HttpContext context)
    {
        if (model.PreselectKey == "first-category")
        {
            model.SelectedItems ??= new List<Select2ListItem>();
            if (!model.SelectedItems.Any(x => x.Id == "1"))
                model.SelectedItems.Add(new Select2ListItem { Id = "1", Text = "Electronics" });

            // Lock it: pretend this user may not change their category.
            model.IsDisabled = true;
            model.ForceDisabled = true;
            model.EnableChildrenIfPreSelected = true;
        }
    }
}
