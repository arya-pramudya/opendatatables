using Microsoft.AspNetCore.Mvc;

namespace SampleApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Select2() => View();

    public IActionResult DataTable() => View();

    public IActionResult Editable() => View();
}
