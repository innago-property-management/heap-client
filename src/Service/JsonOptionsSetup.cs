namespace Innago.Shared.HeapService;

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

internal class JsonOptionsSetup : IConfigureOptions<JsonOptions>
{
    public void Configure(JsonOptions options)
    {
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
    }
}