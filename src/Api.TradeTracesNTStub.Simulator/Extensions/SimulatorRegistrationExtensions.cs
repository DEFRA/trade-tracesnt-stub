using Api.TradeTracesNTStub.Simulator.Ports;
using Api.TradeTracesNTStub.Simulator.WsSecurity;
using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Extensions;

public static class SimulatorRegistrationExtensions
{
    public static IServiceCollection AddTracesNtSimulator(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var credentials = SimulatorCredentialKeys.All.ToDictionary(
            key => key,
            key => configuration.GetSection($"Simulator:Credentials:{key}").Get<SimulatorCredentials>() ?? new()
        );

        services.AddSingleton(new WsSecurityValidator(credentials));

        services.AddServiceModelServices();

        return services;
    }

    public static WebApplication UseTracesNtSimulator(this WebApplication app)
    {
        app.UseMiddleware<WsSecurityMiddleware>();

        // CoreWCF refuses to start an endpoint whose scheme Kestrel is not listening on, so only bind
        // the schemes actually configured. On CDP that is http alone — TLS terminates at the load balancer.
        var https = ListenUrls(app).Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        app.UseServiceModel(builder =>
        {
            builder.AddPort<ChedCertificateSimulator, ChedCertificatePort>(TracesNtServices.Ched, https);
            builder.AddPort<EuIntraCertificateSimulator, EuIntraCertificatePort>(TracesNtServices.EuIntra, https);
            builder.AddPort<DocomCertificateRetrievalSimulator, DocomCertificateRetrievalPort>(
                TracesNtServices.Docom,
                https
            );
            builder.AddPort<ReferenceDataSimulator, ReferenceDataPort>(TracesNtServices.ReferenceData, https);
            builder.AddPort<CustomsCertexChedSimulator, CustomsCertexChedPort>(
                TracesNtServices.CustomsCertexChed,
                https
            );
        });

        return app;
    }

    private static IEnumerable<string> ListenUrls(WebApplication app) =>
        app
            .Urls.Concat((app.Configuration["urls"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Concat(app.Configuration.GetSection("Kestrel:Endpoints").GetChildren().Select(e => e["Url"] ?? ""));

    /// <summary>
    /// Hosts one port. Message size limits match the gateway's client bindings — anything smaller
    /// truncates large certificate responses.
    /// </summary>
    private static IServiceBuilder AddPort<TService, TContract>(
        this IServiceBuilder builder,
        string servicePath,
        bool https
    )
        where TService : class
        where TContract : class
    {
        builder
            .AddService<TService>(options => options.DebugBehavior.IncludeExceptionDetailInFaults = true)
            .AddServiceEndpoint<TService, TContract>(Binding(BasicHttpSecurityMode.None), $"/{servicePath}");

        return https
            ? builder.AddServiceEndpoint<TService, TContract>(
                Binding(BasicHttpSecurityMode.Transport),
                $"/{servicePath}"
            )
            : builder;
    }

    private static BasicHttpBinding Binding(BasicHttpSecurityMode securityMode) =>
        new(securityMode)
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferSize = int.MaxValue,
            ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
        };
}
