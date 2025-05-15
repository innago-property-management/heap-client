namespace Innago.Shared.HeapService.Handlers;

/// <summary>
/// Represents the parameters required to track an event in the system.
/// </summary>
public record TrackEventParameters(
    string EmailAddress,
    string EventName,
    DateTimeOffset Timestamp,
    Dictionary<string, string>? AdditionalProperties
);