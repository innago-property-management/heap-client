using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Prometheus;

using Serilog;

namespace Innago.Shared.HeapService;

using RestSharp;

internal static class ProgramConfiguration
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOpenApi();
        services.AddOpenTelemetry().WithTracing(ConfigureTracing);
        services.AddSerilog();
        services.AddLogging();
        services.AddHealthChecks().ForwardToPrometheus();

        services.AddKeyedScoped<RestClient>("heap", (_, _) => 
        {
            RestClientOptions options = new("https://heapanalytics.com/api/track");
            RestClient client = new(options);
            return client;
        });

        void ConfigureTracing(TracerProviderBuilder providerBuilder)
        {
            string serviceName = configuration["opentelemetry:serviceName"] ??
                                 throw new InvalidOperationException("missing service name: set environment variable opentelemetry__serviceName");

            providerBuilder.AddSource(serviceName);
            providerBuilder.ConfigureResource(resourceBuilder => resourceBuilder.AddService(serviceName));
            providerBuilder.AddHttpClientInstrumentation();
            providerBuilder.AddAspNetCoreInstrumentation();

            if (environment.IsDevelopment())
            {
                providerBuilder.AddConsoleExporter();
            }

            providerBuilder.AddOtlpExporter(options =>
            {
                bool isGoodUri = Uri.TryCreate(configuration["opentelemetry:endpoint"], UriKind.Absolute, out Uri? uri);

                if (!isGoodUri)
                {
                    return;
                }

                options.Endpoint = uri!;
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }
    }

    public static void ConfigureApplicationBuilder(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseHttpMetrics();
        Registry.EnvironmentId = app.Configuration["heapEnvironmentId"] ?? throw new InvalidOperationException("missing env id");
    }

    public static void ConfigureRoutes(this IEndpointRouteBuilder builder)
    {
        builder.MapOpenApi("/openapi.json").CacheOutput();
        builder.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("live") });
        builder.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
        builder.MapMetrics("/metricsz");

        builder.MapPost("/track", Handlers.Track.Track.TrackEvent).WithTags("heap");
    }
}