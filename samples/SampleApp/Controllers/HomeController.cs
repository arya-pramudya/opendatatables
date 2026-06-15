using Microsoft.AspNetCore.Mvc;
using OpenSelect2.AspNetCore.Models;

namespace SampleApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Select2() => View();

    public IActionResult DataTable() => View();

    public IActionResult Editable() => View();

    /// <summary>Renders a Select2 with a dangerous ExtraAttributes value — used by regression tests only.</summary>
    public IActionResult TestExtraAttributesXss() => View(new Select2ViewModel
    {
        Name = "testField",
        Items = new List<Select2ListItem> { new() { Id = "1", Text = "One" } },
        ExtraAttributes = new Dictionary<string, string>
        {
            ["data-x"] = "a\" onmouseover=\"alert(1)",
            // on* key — must be silently dropped by the name-validation check (A4 hardening).
            ["onclick"] = "alert('xss')"
        }
    });
}
