using System.Net;
using Api.TradeTracesNTStub.Utils.Soap;
using WireMock.Matchers;
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
            .Given(Request.Create().WithPath(["/proxy", "/proxy/*"]))
            .AtPriority(1)
            .RespondWith(Response.Create().WithProxy(new ProxyAndRecordSettings 
                {
                    Url = "https://webgate.ec.europa.eu", // TODO: need the TracesNT URL
                    ReplaceSettings = new ProxyUrlReplaceSettings
                    {
                        IgnoreCase = true,
                        OldValue = "/proxy",
                        NewValue = ""
                    }
                })
            );
        
        // Stub calls with SOAPAction Header and SOAP Body containing Security & WebServiceClientId Headers
        _server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])
                .WithBody([
                    // new XPathMatcher("//*[local-name() = 'Security' and text()]"), // TODO: don't know what the SOAP Body Security Header looks like yet. Need credentials for TracesNT so we can record the request/response
                    new XPathMatcher("//*[local-name() = 'WebServiceClientId' and text()]")
                ], MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.INTRA.INTRA.EU.NL.2021.0000001.xml")));
        
        _server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])
                .WithBody([
                    // new XPathMatcher("//*[local-name() = 'Security' and not(text())]"),
                    new XPathMatcher("//*[local-name() = 'WebServiceClientId' and not(text())]")
                ]))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.MethodNotAllowed, "Api.TradeTracesNTStub.Samples.INTRA.MethodNotAllowed.html", false)));
        
        // TODO: don't know what the response is for a INTRA request with missing ID. Need credentials for TracesNT so we can record the request/response
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Stop();
        return Task.CompletedTask;
    }
}