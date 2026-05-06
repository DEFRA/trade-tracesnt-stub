using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using WireMock;

namespace Api.TradeTracesNTStub.Utils.Soap.Responses;

public static class CertificateAttachmentResponses
{
    public static async Task<ResponseMessage> CreateSubmitCertificateAttachmentResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CERTIFICATE_ATTACHMENT.SubmitCertificateAttachmentResponse.TEMPLATE.xml");
        
        var requestBody = XElement.Parse(request.Body!);
        var filename = requestBody.XPathSelectElement("//*[local-name()='SubmitCertificateAttachmentRequest']")?.Attribute("fileName")?.Value;
        resourceContent = resourceContent?.Replace("{{FILENAME}}", filename);

        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
    
    public static async Task<ResponseMessage> CreateGetCertificateAttachmentResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CERTIFICATE_ATTACHMENT.GetCertificateAttachmentResponse.xml");
        
        var requestBody = XElement.Parse(request.Body!);
        var filename = requestBody.XPathSelectElement("//*[local-name()='GetCertificateAttachmentRequest']/*[local-name()='FileName']")?.Value;
        resourceContent = resourceContent?.Replace("{{FILENAME}}", filename);

        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
}