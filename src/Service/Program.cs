using System.Diagnostics.CodeAnalysis;

using Innago.Shared.HeapService;

using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;

AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
    .SetLogLevelsFromConfig(builder.Configuration)
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.OpenTelemetry(options =>
    {
        string authority = new Uri(builder.Configuration["openTelemetry:endpoint"] ?? throw new InvalidOperationException()).GetLeftPart(UriPartial.Authority);
        options.Endpoint = $"{authority}/v1/logs";
        options.Protocol = OtlpProtocol.HttpProtobuf;
    })
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentVariable("POD_NAME")
    .Enrich.WithClientIp()
    .Enrich.WithCorrelationId()
    .Enrich.WithRequestHeader("x-user-id")
    .Enrich.WithRequestHeader("x-organization-id")
    .Enrich.WithRequestHeader("x-user-email-address")
    .Enrich.WithRequestHeader("X-Forwarded-For")
    .Enrich.WithRequestHeader("X-B3-TraceId")
    .Enrich.WithRequestHeader("X-B3-SpanId")
    .Enrich.WithRequestHeader("X-B3-ParentSpanId");

Log.Logger = loggerConfiguration.CreateLogger();

builder.Services.ConfigureHttpJsonOptions(options => { options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default); });

builder.Services.ConfigureServices(builder.Configuration, builder.Environment);

WebApplication app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.ConfigureApplicationBuilder();
app.ConfigureRoutes();

await app.RunAsync();

[ExcludeFromCodeCoverage]
internal static partial class Program;