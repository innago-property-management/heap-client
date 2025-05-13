using System.Text.Json.Serialization;

namespace Innago.Shared.HeapService;

[JsonSerializable(typeof(Payload<string>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;