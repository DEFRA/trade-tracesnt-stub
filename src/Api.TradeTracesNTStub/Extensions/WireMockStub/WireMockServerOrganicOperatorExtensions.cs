using Api.TradeTracesNTStub.Utils.Soap;
using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Api.TradeTracesNTStub.Extensions.WireMockStub;

public static class WireMockServerOrganicOperatorExtensions
{
    public static WireMockServer CreateOrganicOperatorStubs(this WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getOrganicOperatorCertificate\""])
                .WithBody(OrganicOperatorMatchers.ValidGetOrganicOperatorCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.ORGANIC_OPERATOR.GetOrganicOperatorResponse.xml")));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"organicOperator\""])
                .WithBody(OrganicOperatorMatchers.ValidFindOrganicOperatorCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.ORGANIC_OPERATOR.FindOrganicOperatorResponse.xml")));

        return server;
    }
}