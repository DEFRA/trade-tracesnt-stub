using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap.Responses;
using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Api.TradeTracesNTStub.Extensions.WireMockStub;

public static class WireMockServerCertificateAttachmentExtensions
{
    public static WireMockServer CreateCertificateAttachmentStubs(this WireMockServer server)
    {
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"submitCertificateAttachment\""])
                .WithBody(CertificateAttachmentMatchers.ValidSubmitCertificateAttachmentRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await CertificateAttachmentResponses.CreateSubmitCertificateAttachmentResponse(HttpStatusCode.OK, request)));
        
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getCertificateAttachment\""])
                .WithBody(CertificateAttachmentMatchers.ValidGetCertificateAttachmentRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await CertificateAttachmentResponses.CreateGetCertificateAttachmentResponse(HttpStatusCode.OK, request)));

        return server;
    }
}