namespace Innago.Shared.HeapService;

using System.Diagnostics.CodeAnalysis;

using Handlers.Track;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Prometheus;

using RestSharp;

using Serilog;
using Serilog.Events;

[SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded")]
internal static class ProgramConfiguration
{
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

        builder.MapPost("/track", Track.TrackEvent)
            .WithTags("heap")
            .WithDisplayName("Track Heap Event")
            .WithDescription("Tracks an event for a specific user with associated properties using the configured Heap client.")
            .WithSummary("Forwards event to heap analytics service");

        builder.MapPost("/track2", Track.TrackEvent2);
    }

    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOpenApi(ConfigureOpenApiDocument);

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

            services.AddTransient(_ => TracerProvider.Default.GetTracer(serviceName));
        }
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

    private static OpenApiSecurityRequirement AddBearerAuthSecurityRequirement()
    {
        return new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "BearerAuth",
                    },
                },
                ["heap"]
            },
        };
    }

    private static OpenApiContact ConfigureApiContactInfo()
    {
        return new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@innago.com",
        };
    }

    private static void ConfigureOpenApiDocument(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Contact = ConfigureApiContactInfo();
            document.Info.License = ConfigureOpenApiLicense();

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
            document.Components.SecuritySchemes.Add("BearerAuth", CreateBearerAuthSecurityScheme());

            document.SecurityRequirements.Add(AddBearerAuthSecurityRequirement());

            return Task.CompletedTask;
        });
    }

    private static OpenApiLicense ConfigureOpenApiLicense()
    {
        return new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT"),
        };
    }

    private static OpenApiSecurityScheme CreateBearerAuthSecurityScheme()
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OpenIdConnect,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token in the format 'Bearer {token}' to access this API.",
            OpenIdConnectUrl = new Uri("https://my-api.innago.com/connect/token"),
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow(),
            },
            In = ParameterLocation.Header,
            Name = "Authorization",
            UnresolvedReference = true,
        };
    }

    private static LogEventLevel ToLogEventLevel(this string? logLevel)
    {
        return Enum.TryParse(logLevel, true, out LogEventLevel logEventLevel) ? logEventLevel : LogEventLevel.Error;
    }
}