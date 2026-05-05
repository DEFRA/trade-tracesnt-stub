using Api.TradeTracesNTStub.Utils.Soap;
using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using System.Net;
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
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationSectionsResponse.xml")));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getClassificationTrees\""])
                .WithBody(ReferenceDataMatchers.ValidGetClassificationTreesRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationTreesResponse.xml")));


        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getClassificationTree\""])
                .WithBody(ReferenceDataMatchers.ValidGetClassificationTreeRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationTreeResponse.xml")));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getClassificationTreeNodeDetail\""])
                .WithBody(ReferenceDataMatchers.ValidGetClassificationTreeNodeDetailRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationTreeNodeDetailResponse.xml")));

        return server;
    }
}