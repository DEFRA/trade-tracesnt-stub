using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap.Responses;
using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Api.TradeTracesNTStub.Utils.Soap;

namespace Api.TradeTracesNTStub.Extensions.WireMockStub;

public static class WireMockServerChedExtensions
{
    public static WireMockServer CreateChedStubs(this WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getChedCertificate\""])
                .WithBody(ChedMatchers.ValidGetChedCertificateRequestRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ChedResponses.CreateChedAResponse(HttpStatusCode.OK, request)));
        
         server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getChedCertificate\""])
                .WithBody(ChedMatchers.PermissionDeniedErrorFromTraces(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.InternalServerError,
            "Api.TradeTracesNTStub.Samples.CHED.GetChedCertificateResponse.PERMISSION_DENIED.xml")));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"createAndSubmitForDecision\""])
                .WithBody(ChedMatchers.ValidCreateAndSubmitChedForDecisionRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ChedResponses.CreateChedSubmittedResponse(HttpStatusCode.OK, request)));

        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"findChedCertificate\""])
                .WithBody(ChedMatchers.ValidFindChedCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.CHED.FindChedCertificateResponse.xml")));

        return server;
    }
}