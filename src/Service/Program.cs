using System.Diagnostics.CodeAnalysis;

using Innago.Shared.HeapService;

using Serilog;

AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, Innago.Shared.HeapService.AppJsonSerializerContext.Default);
});

builder.Services.ConfigureServices(builder.Configuration, builder.Environment);

WebApplication app = builder.Build();
app.ConfigureApplicationBuilder();
app.ConfigureRoutes(app.Configuration);

await app.RunAsync();

[ExcludeFromCodeCoverage]
internal static partial class Program;