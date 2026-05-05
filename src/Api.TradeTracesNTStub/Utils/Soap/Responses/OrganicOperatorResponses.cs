using System.Net;
using WireMock;

namespace Api.TradeTracesNTStub.Utils.Soap.Responses;

public static class OrganicOperatorResponses
{
    public static async Task<ResponseMessage> CreateFindOrganicOperatorResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.ORGANIC_OPERATOR.FindOrganicOperatorResponse.xml");
        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }

    public static async Task<ResponseMessage> CreateGetOrganicOperatorResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.ORGANIC_OPERATOR.GetOrganicOperatorResponse.xml");
        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
}