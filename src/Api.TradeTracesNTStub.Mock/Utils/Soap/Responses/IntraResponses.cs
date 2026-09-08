using System.Net;
using WireMock;

namespace Api.TradeTracesNTStub.Mock.Utils.Soap.Responses;

public static class IntraResponses
{
    public static async Task<ResponseMessage> CreateItahcResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Mock.Samples.INTRA.ITAHC.TEMPLATE.xml");
        
        var requestedItahcId = SoapUtils.GetRequestedId(request);
        resourceContent = resourceContent?.Replace("{{ID}}", requestedItahcId);
        
        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
    
    public static async Task<ResponseMessage> CreateItahcPdfResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Mock.Samples.INTRA.ITAHC.PDF.TEMPLATE.xml");
        
        var requestedItahcId = SoapUtils.GetRequestedId(request);
        resourceContent = resourceContent?.Replace("{{ID}}", requestedItahcId);

        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
}