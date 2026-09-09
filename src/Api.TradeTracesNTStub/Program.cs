using Api.TradeTracesNTStub.Utils;
using Api.TradeTracesNTStub.Utils.Http;
using Api.TradeTracesNTStub.Utils.Mongo;
using FluentValidation;
using System.Diagnostics.CodeAnalysis;
using Api.TradeTracesNTStub.Config;
using Api.TradeTracesNTStub.Endpoints.Api;
using Api.TradeTracesNTStub.Mock.Extensions;
using Api.TradeTracesNTStub.Mock.Hosts;
using Api.TradeTracesNTStub.Simulator.Extensions;
using Api.TradeTracesNTStub.Utils.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;
using Serilog;

var app = CreateWebApplication(args);
await app.RunAsync();
return;

[ExcludeFromCodeCoverage]
static WebApplication CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    ConfigureBuilder(builder);

    var app = builder.Build();
    return SetupApplication(app);
}

[ExcludeFromCodeCoverage]
static void ConfigureBuilder(WebApplicationBuilder builder)
{
    builder.Configuration.AddEnvironmentVariables();

    // Load certificates into Trust Store - Note must happen before Mongo and Http client connections.
    builder.Services.AddCustomTrustStore();

    // Configure logging to use the CDP Platform standards.
    builder.Services.AddHttpContextAccessor();
    builder.Host.UseSerilog(CdpLogging.Configuration);

    // Default HTTP Client
    builder.Services
        .AddHttpClient("DefaultClient")
        .AddHeaderPropagation();

    // Proxy HTTP Client
    builder.Services.AddTransient<ProxyHttpMessageHandler>();
    builder.Services
        .AddHttpClient("proxy")
        .ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();

    // Propagate trace header.
    builder.Services.AddHeaderPropagation(options =>
    {
        var traceHeader = builder.Configuration.GetValue<string>("TraceHeader");
        if (!string.IsNullOrWhiteSpace(traceHeader))
        {
            options.Headers.Add(traceHeader);
        }
    });


    // Set up the MongoDB client. Config and credentials are injected automatically at runtime.
    MongoClientSettings.Extensions.AddAWSAuthentication();
    builder.Services.Configure<MongoConfig>(builder.Configuration.GetSection("Mongo"));
    builder.Services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();

    // Add healthcheck, this is required for the platform to know your service is alive.
    builder.Services.AddHealthChecks();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    
    // Set up WireMock Hosted Service
    builder.Services.AddWireMockHostedService();

    // Set up the CoreWCF TRACES NT simulator.
    builder.Services.AddTracesNtSimulator(builder.Configuration);
}

[ExcludeFromCodeCoverage]
static WebApplication SetupApplication(WebApplication app)
{
    app.UseHeaderPropagation();
    app.UseRouting();
    app.MapHealthChecks("/health");
    
    app.UseSampleEndpoints();

    // The simulator owns the five TRACES service paths; the WireMock stub owns /mock and /proxy.
    app.UseTracesNtSimulator();
    app.UseMiddleware<WireMockReverseProxyMiddleware>();

    return app;
}