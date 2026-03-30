using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using dotnet_aspnet_mvc.Models;

namespace dotnet_aspnet_mvc.Controllers;

public class HomeController(IConfiguration configuration) : Controller // 👈 Varlock: inject IConfiguration
{
    // Keep the UI limited to non-sensitive keys.
    private static readonly string[] Keys = ["APP_NAME", "APPSETTINGS_ONLY", "USERSECRETS_ONLY"];

    public IActionResult Index()
    {
        ViewBag.ConfigItems = Keys.Select(key => new
        {
            Key = key,
            Value = configuration[key] ?? "(null)",
        }).ToArray();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
