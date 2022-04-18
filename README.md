# SampleApp


## Table of contents
  1. [Introduction](#introduction)
  2. [Repository preparation](#repository-preparation)
  3. [Log tracers](#log-tracers)
  * [Basic logging](#basic-logging)
  * [MediatR pipeline behavior](#mediatr-pipeline-behavior)
  4. [Run application](#run-application)


## Introduction
Instructions for setting up OpenTelemetry tracking.
Solution has already configured otel collector, jaeger service and sql server, see docker-compose.yml.

## Repository preparation
First install the required packages:
```dotnetcli
$ dotnet add package OpenTelemetry --prerelease
$ dotnet add package OpenTelemetry.Api --prerelease
$ dotnet add package OpenTelemetry.Extensions.Hosting --prerelease
$ dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol --prerelease
$ dotnet add package OpenTelemetry.Instrumentation.AspNetCore --prerelease
$ dotnet add package OpenTelemetry.Instrumentation.GrpcNetClient --prerelease
$ dotnet add package OpenTelemetry.Instrumentation.Http --prerelease
$ dotnet add package OpenTelemetry.Instrumentation.SqlClient --prerelease
```

Next create shared folder with Consts.cs file:
```dotnetcli
mkdir Shared
```

Inside Consts.cs paste:
```dotnetcli
namespace SampleApp.WebUI.Shared;

public sealed class Consts
{
    internal const string SERVICE_NAME = "SampleApp";
}
```

Next, add the following code into your Program.cs file:
```dotnetcli
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceName = Consts.SERVICE_NAME;
var serviceVersion = "1.0.0";

.
.
.

// Add services to the container.
builder.Services.AddOpenTelemetryTracing((builder) => builder
    .AddSource(serviceName)
    .SetResourceBuilder(ResourceBuilder
        .CreateDefault()
        .AddTelemetrySdk()
        .AddEnvironmentVariableDetector()
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .SetSampler(new AlwaysOnSampler())
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddSqlClientInstrumentation()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://otel-collector:4317");
        options.Protocol = OtlpExportProtocol.Grpc;
    }));

builder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));
```

## Log tracers

### Basic logging

Inject the TracerProvider controller into the controllers.
```dotnetcli
...
private readonly Tracer tracer;
private const string SERVICE_NAME = Consts.SERVICE_NAME;

public HomeController(..., TracerProvider tracerProvider)
{
    ...
    this.tracer = tracerProvider.GetTracer(SERVICE_NAME);
}
```

Next create spans where you need them, ex:
```dotnetcli
public async Task<IActionResult> Index()
{
    using (var span = this.tracer.StartActiveSpan($"{nameof(Index)}"))
    {
        span.SetAttribute("pagename", "index");
        this.logger.LogInformation("{Action}", nameof(Index));
    }

    using var span1 = this.tracer.StartActiveSpan($"{nameof(Index)}");
    span1.SetAttribute("pagename", "index after");

    ...

    return View();
}

public IActionResult Privacy()
{
    using var span = this.tracer.StartActiveSpan($"{nameof(Privacy)}");
    span.SetAttribute("pagename", "privacy");

    using var span1 = this.tracer.StartActiveSpan($"{nameof(Privacy)}");
    span1.SetAttribute("pagename", "privacy after");

    this.logger.LogInformation("{Action}", nameof(Privacy));

    ...

    return View();
}
```

### MediatR pipeline behavior
Traces can be injected and recorded wherever they are needed, such as inside mediatr's handlers and pipeline behaviors.

First, install mediatr packages:
```dotnetcli
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
```

Add global usings in csproj file:
```dotnetcli
<ItemGroup>
    <Using Include="MediatR" />
    <Using Include="System.ComponentModel.DataAnnotations" />
    <Using Include="System.Text.Json.Serialization" />
</ItemGroup>
```

Create folder for commands, command handlers and behaviors:
```dotnetcli
mkdir Commands
mkdir CommandHandlers
mkdir Behaviors
```

Create SendEcho.cs inside Commands folder:
```dotnetcli
namespace SampleApp.WebUI.Commands;

public sealed record SendEcho : IRequest
{
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}
```

Create SendEchoHandler.cs inside CommandHandlers folder:
```dotnetcli
using OpenTelemetry.Trace;
using SampleApp.WebUI.Commands;
using SampleApp.WebUI.Shared;

namespace SampleApp.WebUI.CommandHandlers;

internal sealed class SendEchoHandler : IRequestHandler<SendEcho>
{
    private readonly ILogger<SendEchoHandler> logger;
    private readonly Tracer tracer;
    private const string SERVICE_NAME = Consts.SERVICE_NAME;

    public SendEchoHandler(ILogger<SendEchoHandler> logger, TracerProvider tracerProvider)
    {
        this.tracer = tracerProvider.GetTracer(SERVICE_NAME);
        this.logger = logger;
    }

    public async Task<Unit> Handle(SendEcho request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received message: {Message}", request.Message);
        return await Task.FromResult(Unit.Value);
    }
}
```

Create span in the handler command:
```dotnetcli
    public async Task<Unit> Handle(SendEcho request, CancellationToken cancellationToken)
    {
        using var span = this.tracer.StartActiveSpan($"{nameof(SendEchoHandler)}");
        span.SetAttribute("message", request.Message);

        ...
    }
```

Create OpenTelemetryBehavior.cs with injected TracerProvider inside Behaviors folder:
```dotnetcli
using SampleApp.WebUI.Shared;

namespace SampleApp.WebUI.Behaviors;

using OpenTelemetry.Trace;

internal sealed class OpenTelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private const string SERVICE_NAME = Consts.SERVICE_NAME;

    private readonly Tracer tracer;

    public OpenTelemetryBehavior(TracerProvider tracerProvider) => this.tracer = tracerProvider.GetTracer(SERVICE_NAME);

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        return await next();
    }
}
```

Create span in behavior's handler method:
```dotnetcli
public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
{
    using var span = this.tracer.StartActiveSpan(typeof(TRequest).Name);
    span.SetAttribute("request", request.ToString());

    ...
}
```

In the controller, send the command via mediatr:
```dotnetcli
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
```

## Run application
Go to repository main folder. Run in termial:
```dotnetcli
docker-compose build
```

Next start docker-compose. Run in termial:
```dotnetcli
docker-compose up -d
```

All services should start, then open browser and go to https://127.0.0.1:5443/ and http://localhost:16686/search
The first link will show application page and the second the Jaeger UI.
Click on Home and Privacy links on the application page.
Go to Jaeger UI and refresh it. From services dropdown choose "SampleApp" and click "Find Traces".
You should see a similar view:
![image](https://user-images.githubusercontent.com/26749839/163362162-17756122-c527-4902-a8ac-8d31d0d5b444.png)

Click on "SampleApp: /", and you should see the tree of spans:
![image](https://user-images.githubusercontent.com/26749839/163362429-d1a40187-0c1f-49a8-8bfa-33017e59c4d7.png)
