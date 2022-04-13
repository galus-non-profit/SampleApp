namespace SampleApp.WebUI.Commands;

public sealed record SendEcho : IRequest
{
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}
