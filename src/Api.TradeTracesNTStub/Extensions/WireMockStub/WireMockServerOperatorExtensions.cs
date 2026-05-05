using System.Net;
using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap.Responses;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Api.TradeTracesNTStub.Extensions.WireMockStub;

public static class WireMockServerOperatorExtensions
{
    public static WireMockServer CreateOperatorStubs(this WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"createOperator\""])
                .WithBody(OperatorMatchers.ValidCreateOperatorRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await OperatorResponses.CreateOperatorCreatedResponse(HttpStatusCode.OK, request)));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"findOperator\""])
                .WithBody(OperatorMatchers.ValidFindOperatorRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await OperatorResponses.CreateFindOperatorResponse(HttpStatusCode.OK, request)));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getOperator\""])
                .WithBody(OperatorMatchers.ValidGetOperatorByIdRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await OperatorResponses.CreateGetOperatorByIdResponse(HttpStatusCode.OK, request)));

        return server;
    }
}