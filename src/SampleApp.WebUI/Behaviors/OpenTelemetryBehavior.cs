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
        using var span = this.tracer.StartActiveSpan(typeof(TRequest).Name);
        span.SetAttribute("request", request.ToString());

        return await next();
    }
}
