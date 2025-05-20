using System.Text.Json.Serialization;

namespace Innago.Shared.HeapService;

using Handlers.Track;

[JsonSerializable(typeof(TrackEventParameters))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Payload<string>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;