using System.Net;
using WireMock;

namespace Api.TradeTracesNTStub.Utils.Soap.Responses;

public static class ReferenceDataResponses
{
    public static async Task<ResponseMessage> CreateGetClassificationSectionsResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationSectionsResponse.xml");
        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }

    public static async Task<ResponseMessage> CreateGetClassificationTreesResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationTreesResponse.xml");
        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }

    public static async Task<ResponseMessage> CreateGetClassificationTreeResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationTreeResponse.xml");
        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }


    public static async Task<ResponseMessage> CreateGetClassificationTreeNodeDetailResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationTreeNodeDetailResponse.xml");
        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
}