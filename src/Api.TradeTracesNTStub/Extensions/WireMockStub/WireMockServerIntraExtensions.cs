using System.Net;
using Api.TradeTracesNTStub.Utils.Soap.Matchers;
using Api.TradeTracesNTStub.Utils.Soap;
using Api.TradeTracesNTStub.Utils.Soap.Responses;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Api.TradeTracesNTStub.Extensions.WireMockStub;

public static class WireMockServerIntraExtensions
{
    public static WireMockServer CreateIntraStubs(this WireMockServer server)
    {
        // Stub calls with SOAPAction Header and valid getEuIntraCertificate request headers and body
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])
                .WithBody(IntraMatchers.ValidGetEuIntraCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await IntraResponses.CreateItahcResponse(HttpStatusCode.OK, request)));
        
        // Stub calls with SOAPAction Header and any invalid Headers
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])
                .WithBody(MessageMatchers.InvalidHeaders()))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.InternalServerError, "Api.TradeTracesNTStub.Samples.INTRA.UnauthenticatedException.xml")));
        
        // Stub calls with SOAPAction Header, valid getEuIntraCertificate request headers and a missing GetEuIntraCertificateRequest -> ID
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraCertificate\""])
                .WithBody(IntraMatchers.InvalidGetEuIntraCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.InternalServerError, "Api.TradeTracesNTStub.Samples.INTRA.GetEuIntraCertificateInvalidId.xml")));

        // Stub calls with SOAPAction Header and valid getEuIntraPdfCertificate request headers and body
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"getEuIntraPdfCertificate\""])
                .WithBody(IntraMatchers.ValidGetEuIntraPdfCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async request => await IntraResponses.CreateItahcPdfResponse(HttpStatusCode.OK, request)));
        
        // Stub calls with SOAPAction Header and valid findEuIntraCertificate request headers and body
        server
            .Given(Request.Create()
                .WithHeader("SOAPAction", ["\"findEuIntraCertificate\""])
                .WithBody(IntraMatchers.ValidFindEuIntraCertificateRequest(), MatchOperator.And))
            .AtPriority(2)
            .RespondWith(Response.Create().WithCallback(async _ => await SoapUtils.CreateResponseFromResource(HttpStatusCode.OK, "Api.TradeTracesNTStub.Samples.INTRA.FindEuIntraCertificateResponse.xml")));

        return server;
    }
}