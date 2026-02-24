using Api.TradeTracesNTStub.Utils.Soap;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace Api.TradeTracesNTStub.Hosts;

public class WireMockHostedService : IHostedService
{
    private WireMockServer? _server;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server = WireMockServer.StartWithAdminInterface(1080);
        
        // All requests with /proxy prefix will be proxied though to a TraceNT instance
        _server
            .Given(Request.Create().WithPath("/proxy/*"))
            .RespondWith(Response.Create().WithProxy(new ProxyAndRecordSettings 
                {
                    Url = "https://webgate.ec.europa.eu", // TODO: need the TracesNT URL
                    ReplaceSettings = new ProxyUrlReplaceSettings
                    {
                        IgnoreCase = true,
                        OldValue = "/proxy",
                        NewValue = ""
                    }
                }) );
        
        // Stub calls with SOAPAction Header
        _server
            .Given(Request.Create().WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])) // TODO: add Security Header matching
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateSuccessResponse("Api.TradeTracesNTStub.Samples.INTRA.INTRA.EU.NL.2021.0000001.xml")));
        
        // TODO: Stub requests with missing Security SOAP Header and missing WebServiceClientId SOAP Header to simulate auth errors
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Stop();
        return Task.CompletedTask;
    }
}