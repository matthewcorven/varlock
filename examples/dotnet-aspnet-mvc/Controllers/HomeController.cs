using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using dotnet_aspnet_mvc.Models;

namespace dotnet_aspnet_mvc.Controllers;

public class HomeController(IConfiguration configuration) : Controller // 👈 Varlock: inject IConfiguration
{
    // 👈 Varlock: known keys from .env.schema and which are sensitive
    private static readonly (string Key, bool Sensitive)[] VarlockKeys =
    [
        ("APP_NAME", false),
        ("APP_PORT", false),
        ("FEATURE_ENABLED", false),
        ("SECRET_TOKEN", true),
    ];

    public IActionResult Index()
    {
        // 👈 Varlock: read config values and pass to view
        ViewBag.ConfigItems = VarlockKeys.Select(k => new
        {
            k.Key,
            Display = k.Sensitive ? "***" : configuration[k.Key] ?? "(null)",
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
