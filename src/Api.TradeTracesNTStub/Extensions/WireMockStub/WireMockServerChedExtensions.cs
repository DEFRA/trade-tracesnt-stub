using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap.Responses;
using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

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
                .WithHeader("SOAPAction", ["\"submitCertificateAttachment\""])
                .WithBody(ChedMatchers.ValidSubmitCertificateAttachmentRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ChedResponses.CreateSubmitCertificateAttachmentResponse(HttpStatusCode.OK, request)));
        
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getCertificateAttachment\""])
                .WithBody(ChedMatchers.ValidGetCertificateAttachmentRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ChedResponses.CreateGetCertificateAttachmentResponse(HttpStatusCode.OK, request)));
        
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"createAndSubmitForDecision\""])
                .WithBody(ChedMatchers.ValidCreateAndSubmitChedForDecisionRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await ChedResponses.CreateChedSubmittedResponse(HttpStatusCode.OK, request)));
        
        return server;
    }
}