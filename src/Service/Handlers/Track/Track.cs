namespace Innago.Shared.HeapService.Handlers.Track;

using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using OpenTelemetry.Trace;

using RestSharp;

/// <summary>
/// Provides methods for tracking events and sending them to the heap analytics service.
/// </summary>
public static class Track
{
    /// <summary>
    /// Tracks an event for a specific user with associated properties using the configured Heap client.
    /// </summary>
    /// <param name="parameters">An instance of <see cref="Innago.Shared.HeapService.Handlers.Track.TrackEventParameters"/> containing the email address, event name, timestamp, and any additional properties for the event.</param>
    /// <param name="client">The <see cref="RestClient"/> instance configured for connecting to Heap services.</param>
    /// <param name="loggerFactory"></param>
    /// <param name="tracer">The open telemetry tracer.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete, allowing for cancellation.</param>
    /// <returns>An <see cref="IResult"/> indicating the result of the request. This may be an OK, BadRequest, or an empty result based on the response status.</returns>
    public static async Task<IResult> TrackEvent(
        TrackEventParameters parameters,
        [FromKeyedServices("heap")] RestClient client,
        ILoggerFactory loggerFactory,
        Tracer tracer,
        CancellationToken cancellationToken)
    {
        using TelemetrySpan span = tracer.StartSpan(nameof(TrackEvent));
        
        Dictionary<string, string> additionalProperties = parameters.AdditionalProperties ?? new Dictionary<string, string>();
        RestRequest request = new(string.Empty);
        request.AddHeader("accept", "application/json");

        var jsonString = $$"""
                           {
                               "app_id": "{{Registry.EnvironmentId}}",
                               "identity": "{{parameters.EmailAddress.ToLowerInvariant()}}",
                               "event": "{{parameters.EventName.ToLowerInvariant()}}",
                               "timestamp": "{{parameters.Timestamp:O}}",
                               "properties" : {{JsonSerializer.Serialize(additionalProperties, typeof(Dictionary<string, string>), AppJsonSerializerContext.Default)}}
                           }
                           """;

        request.AddJsonBody(jsonString, false);

        span.SetAttribute("identity", parameters.EmailAddress.ToLowerInvariant());

        RestResponse response = await client.PostAsync(request, cancellationToken).ConfigureAwait(false);

        IResult result = response.StatusCode switch
        {
            HttpStatusCode.OK => TypedResults.Ok(),
            HttpStatusCode.BadRequest => TypedResults.BadRequest(),
            _ => TypedResults.Empty,
        };

        ILogger logger = loggerFactory.CreateLogger(nameof(TrackEvent));
        logger.LogHeapCall(jsonString, response.Content);
        
        span.SetStatus(response.StatusCode == HttpStatusCode.OK ? Status.Ok : Status.Error);

        MyMetrics.MyCounter.WithLabels("test2").Inc();
        
        return result;
    }

    internal static Task<IResult> TrackEvent2(
        TrackEventParameters parameters,
        [FromKeyedServices("heap")] RestClient client,
        ILoggerFactory loggerFactory,
        Tracer tracer,
        [FromHeader(Name = "x-test")] string? testHeaderValue = null,
        CancellationToken cancellationToken = default)
    {
        ILogger l = loggerFactory.CreateLogger("test");
        l.LogDebug("{Message}", testHeaderValue);

        return TrackEvent(parameters, client, loggerFactory, tracer, cancellationToken);
    }
}