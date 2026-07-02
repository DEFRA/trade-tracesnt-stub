using Api.TradeTracesNTStub.Utils.Soap;
using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap.Responses;
using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Api.TradeTracesNTStub.Extensions.WireMockStub;

public static class WireMockServerCertexExtensions
{
    public static WireMockServer CreateCertexStubs(this WireMockServer server)
    {
        // Stub calls with SOAPAction Header and valid getEuIntraCertificate request headers and body
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"processedChedRequest\""])
                .WithBody(CertexMatchers.ValidGetProcessedChedRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.CERTEX.GetProcessedChedResponse.xml")));

        return server;
    }
}