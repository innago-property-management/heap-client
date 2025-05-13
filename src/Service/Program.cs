using System.Diagnostics.CodeAnalysis;

using Innago.Shared.HeapService;

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;

AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
    .MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Information)
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://opentelemetry-collector.observability.svc/v1/logs";
        options.Protocol = OtlpProtocol.HttpProtobuf;
    })
    .Enrich.FromLogContext().Enrich.WithMachineName();

Log.Logger = loggerConfiguration.CreateLogger();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.ConfigureServices(builder.Configuration, builder.Environment);

WebApplication app = builder.Build();
app.ConfigureApplicationBuilder();
app.ConfigureRoutes(app.Configuration);

await app.RunAsync();

[ExcludeFromCodeCoverage]
internal static partial class Program;