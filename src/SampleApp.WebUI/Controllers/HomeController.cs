using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using SampleApp.WebUI.Commands;
using SampleApp.WebUI.Models;
using SampleApp.WebUI.Shared;

namespace SampleApp.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> logger;
    private readonly IMediator mediator;
    private readonly Tracer tracer;
    private const string SERVICE_NAME = Consts.SERVICE_NAME;

    public HomeController(ILogger<HomeController> logger, IMediator mediator, TracerProvider tracerProvider)
    {
        this.logger = logger;
        this.mediator = mediator;
        this.tracer = tracerProvider.GetTracer(SERVICE_NAME);
    }

    public async Task<IActionResult> Index()
    {
        using (var span = this.tracer.StartActiveSpan($"{nameof(Index)}"))
        {
            span.SetAttribute("pagename", "index");
            this.logger.LogInformation("{Action}", nameof(Index));
        }

        using var span1 = this.tracer.StartActiveSpan($"{nameof(Index)}");
        span1.SetAttribute("pagename", "index after");

        _ = await mediator.Send(new SendEcho { Message = "message test", });

        return View();
    }

    public IActionResult Privacy()
    {
        using var span = this.tracer.StartActiveSpan($"{nameof(Privacy)}");
        span.SetAttribute("pagename", "privacy");

        using var span1 = this.tracer.StartActiveSpan($"{nameof(Privacy)}");
        span1.SetAttribute("pagename", "privacy after");

        this.logger.LogInformation("{Action}", nameof(Privacy));
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
