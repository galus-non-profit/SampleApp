using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SampleApp.WebUI.Models;

namespace SampleApp.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> logger;

    public HomeController(ILogger<HomeController> logger) => this.logger = logger;

    public IActionResult Index()
    {
        this.logger.LogInformation("{Action}", nameof(Index));
        return View();
    }

    public IActionResult Privacy()
    {
        this.logger.LogInformation("{Action}", nameof(Privacy));
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
