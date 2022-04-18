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
        using var span = this.tracer.StartActiveSpan($"{nameof(SendEchoHandler)}");
        span.SetAttribute("message", request.Message);

        logger.LogInformation("Received message: {Message}", request.Message);
        return await Task.FromResult(Unit.Value);
    }
}
