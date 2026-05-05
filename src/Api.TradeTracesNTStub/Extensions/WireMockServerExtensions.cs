using Api.TradeTracesNTStub.Extensions.WireMockStub;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace Api.TradeTracesNTStub.Extensions;

public static class WireMockServerExtensions
{
    public static WireMockServer CreateStubMappings(this WireMockServer server)
    {
        // All requests with /proxy prefix will be proxied through to the TracesNT Acceptance instance
        server
            .Given(Request.Create().WithPath(["/proxy", "/proxy/*"]))
            .AtPriority(1)
            .RespondWith(Response.Create().WithProxy(new ProxyAndRecordSettings 
                {
                    Url = "https://webgate.acceptance.ec.europa.eu",
                    ReplaceSettings = new ProxyUrlReplaceSettings
                    {
                        IgnoreCase = true,
                        OldValue = "/proxy",
                        NewValue = ""
                    }
                })
            );

        server.CreateIntraStubs();
        server.CreateChedStubs();
        server.CreateOperatorStubs();
        server.CreateReferenceDataStubs();

        return server;
    }
}