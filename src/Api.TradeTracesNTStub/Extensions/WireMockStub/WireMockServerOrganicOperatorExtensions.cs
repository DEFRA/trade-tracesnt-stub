using System.Net;
using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap.Responses;
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
            .RespondWith(Response.Create().WithCallback(async request => await OrganicOperatorResponses.CreateGetOrganicOperatorResponse(HttpStatusCode.OK, request)));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"organicOperator\""])
                .WithBody(OrganicOperatorMatchers.ValidFindOrganicOperatorCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await OrganicOperatorResponses.CreateFindOrganicOperatorResponse(HttpStatusCode.OK, request)));

        return server;
    }
}