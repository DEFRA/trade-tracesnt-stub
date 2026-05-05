using System.Net;
using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap.Responses;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Api.TradeTracesNTStub.Extensions.WireMockStub;

public static class WireMockServerReferenceDataExtensions
{
    public static WireMockServer CreateReferenceDataStubs(this WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getClassificationSections\""])
                .WithBody(ReferenceDataMatchers.ValidGetClassificationSectionsRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ReferenceDataResponses.CreateGetClassificationSectionsResponse(HttpStatusCode.OK, request)));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getClassificationTrees\""])
                .WithBody(ReferenceDataMatchers.ValidGetClassificationTreesRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ReferenceDataResponses.CreateGetClassificationTreesResponse(HttpStatusCode.OK, request)));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getClassificationTree\""])
                .WithBody(ReferenceDataMatchers.ValidGetClassificationTreeRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ReferenceDataResponses.CreateGetClassificationTreeResponse(HttpStatusCode.OK, request)));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getClassificationTreeNodeDetail\""])
                .WithBody(ReferenceDataMatchers.ValidGetClassificationTreeNodeDetailRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ReferenceDataResponses.CreateGetClassificationTreeNodeDetailResponse(HttpStatusCode.OK, request)));

        return server;
    }
}