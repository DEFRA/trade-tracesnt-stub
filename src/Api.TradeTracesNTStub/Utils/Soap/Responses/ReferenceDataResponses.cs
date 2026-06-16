using System.Net;

using WireMock;

namespace Api.TradeTracesNTStub.Utils.Soap.Responses
{
    public class ReferenceDataResponses
    {
        public static async Task<ResponseMessage?> CreateClassificationTreeResponse(HttpStatusCode statusCode, IRequestMessage request)
        {
            var treeId = SoapUtils.GetTreeId(request);

            var resourceContent = await SoapUtils.GetEmbeddedResource($"Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationTreeResponse.{treeId?.ToUpperInvariant()}.xml");

            if (resourceContent == null)
            {
                return null;
            }

            return SoapUtils.StubResponseMessage(statusCode, resourceContent);
        }

        public static async Task<ResponseMessage?> CreateClassificationTreeNodeDetailResponse(HttpStatusCode statusCode, IRequestMessage request)
        {
            var treeId = SoapUtils.GetTreeId(request);
            var nodePath = SoapUtils.GetNodePath(request)?.Replace("/", "_");

            var resourceContent = await SoapUtils.GetEmbeddedResource($"Api.TradeTracesNTStub.Samples.REFERENCE_DATA.GetClassificationTreeNodeDetailResponse.{treeId?.ToUpperInvariant()}.{nodePath?.ToUpperInvariant()}.xml");

            if (resourceContent == null)
            {
                return null;
            }

            return SoapUtils.StubResponseMessage(statusCode, resourceContent);
        }
    }
}
