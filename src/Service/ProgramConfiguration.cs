using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Prometheus;

using Serilog;

namespace Innago.Shared.HeapService;

using Microsoft.AspNetCore.HttpOverrides;

using RestSharp;

using Serilog.Events;

internal static class ProgramConfiguration
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOpenApi();

        services.AddOpenTelemetry().WithTracing(ConfigureTracing);

        services.AddSerilog();

        services.AddHealthChecks().ForwardToPrometheus();

        services.AddKeyedScoped<RestClient>("heap",
            (_, _) =>
            {
                RestClientOptions options = new("https://heapanalytics.com/api/track");
                RestClient client = new(options);
                return client;
            });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        services.AddHttpContextAccessor();

        // ReSharper disable once SeparateLocalFunctionsWithJumpStatement
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

    internal static LoggerConfiguration SetLogLevelsFromConfig(this LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        IConfigurationSection minimumLevelSection = configuration.GetSection("Serilog:MinimumLevel");

        loggerConfiguration.MinimumLevel.Is(minimumLevelSection["default"].ToLogEventLevel());

        IConfigurationSection overrideSection = minimumLevelSection.GetSection("Override");

        foreach (IConfigurationSection overrideEntry in overrideSection.GetChildren())
        {
            loggerConfiguration.MinimumLevel.Override(overrideEntry.Key, overrideEntry.Value.ToLogEventLevel());
        }

        return loggerConfiguration;
    }

    private static LogEventLevel ToLogEventLevel(this string? logLevel)
    {
        return Enum.TryParse(logLevel, true, out LogEventLevel logEventLevel) ? logEventLevel : LogEventLevel.Error;
    }
}