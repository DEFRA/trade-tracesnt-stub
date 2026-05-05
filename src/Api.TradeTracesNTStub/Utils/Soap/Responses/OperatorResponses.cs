using System.Net;
using WireMock;

namespace Api.TradeTracesNTStub.Utils.Soap.Responses;

public static class OperatorResponses
{
    public static async Task<ResponseMessage> CreateOperatorCreatedResponse(HttpStatusCode statusCode)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.OPERATOR.Operator.Created.TEMPLATE.xml");

        var operatorId = new Random().Next(1, 999999);  
        resourceContent = resourceContent?.Replace("{{OPERATOR_ID}}", operatorId.ToString());
        
        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }

    public static async Task<ResponseMessage> CreateGetOperatorByIdResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.OPERATOR.GetOperatorByIdResponse.TEMPLATE.xml");

        var requestedId = SoapUtils.GetRequestedId(request) ?? throw new NullReferenceException("Invalid request - Operator Id was not provided");
        resourceContent = resourceContent?.Replace("{{OPERATOR_ID}}", requestedId);

        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
}