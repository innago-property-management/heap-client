namespace Innago.Shared.HeapService;

internal static partial class LoggerMessages
{
    [LoggerMessage(LogLevel.Information, "{Json} - {Response}")]
    public static partial void LogHeapCall(this ILogger logger, string json, string? response);
}