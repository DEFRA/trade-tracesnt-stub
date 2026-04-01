using System.Net;
using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap;
using WireMock.Matchers;
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
        
        // Stub calls with SOAPAction Header and valid getEuIntraCertificate request headers and body
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])
                .WithBody(Matchers.ValidGetEuIntraCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await SoapUtils.CreateItahcResponse(HttpStatusCode.OK, request)));
        
        // Stub calls with SOAPAction Header and any invalid Headers
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])
                .WithBody(Matchers.InvalidHeaders()))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.InternalServerError, "Api.TradeTracesNTStub.Samples.INTRA.UnauthenticatedException.xml")));
        
        // Stub calls with SOAPAction Header, valid getEuIntraCertificate request headers and a missing GetEuIntraCertificateRequest -> ID
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])
                .WithBody(Matchers.InvalidGetEuIntraCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.InternalServerError, "Api.TradeTracesNTStub.Samples.INTRA.GetEuIntraCertificateInvalidId.xml")));

        return server;
    }
}