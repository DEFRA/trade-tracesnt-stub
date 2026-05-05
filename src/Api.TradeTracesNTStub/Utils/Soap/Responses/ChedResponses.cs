using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using WireMock;

namespace Api.TradeTracesNTStub.Utils.Soap.Responses;

public static class ChedResponses
{
    private static string GenerateChedId(XElement requestBody)
    {
        var includedSpsNote = requestBody.XPathSelectElement("//*[local-name()='IncludedSPSNote']/*[local-name()='SubjectCode' and text()='CHED_TYPE']")?.Parent;
        var chedType = includedSpsNote?.Elements().FirstOrDefault(e => e.Name.LocalName == "ContentCode")?.Value ?? "A";
        
        var random = new Random().Next(1, 9999999);
        return $"CHED{chedType}.XI.2026.{random.ToString().PadLeft(7, '0')}";
    }
    
    public static async Task<ResponseMessage> CreateChedAResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.CHEDA.TEMPLATE.xml");
        
        var requestedChedId = SoapUtils.GetRequestedId(request);
        resourceContent = resourceContent?.Replace("{{CHED_ID}}", requestedChedId);

        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
    
    public static async Task<ResponseMessage> CreateSubmitCertificateAttachmentResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.SubmitCertificateAttachmentResponse.TEMPLATE.xml");
        
        var requestBody = XElement.Parse(request.Body!);
        var filename = requestBody.XPathSelectElement("//*[local-name()='SubmitCertificateAttachmentRequest']")?.Attribute("fileName")?.Value;
        resourceContent = resourceContent?.Replace("{{FILENAME}}", filename);

        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
    
    public static async Task<ResponseMessage> CreateGetCertificateAttachmentResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.GetCertificateAttachmentResponse.TEMPLATE.xml");
        
        var requestBody = XElement.Parse(request.Body!);
        var filename = requestBody.XPathSelectElement("//*[local-name()='GetCertificateAttachmentRequest']/*[local-name()='FileName']")?.Value;
        resourceContent = resourceContent?.Replace("{{FILENAME}}", filename);

        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }

    public static async Task<ResponseMessage> CreateChedSubmittedResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await SoapUtils.GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.CHED.Submitted.TEMPLATE.xml");

        var requestBody = XElement.Parse(request.Body!);
        var issueDateTime = SoapUtils.GenerateCentralEuropeanTime();
        var chedId = GenerateChedId(requestBody);
        var referenceSpsReferencedDocument = requestBody.XPathSelectElement("//*[local-name()='ReferenceSPSReferencedDocument']");
        var schemeAgencyId = referenceSpsReferencedDocument?.Elements().Single(e => e.Name.LocalName == "ID" && e.Attribute("schemeAgencyID") is not null).Value;
        var documentId = referenceSpsReferencedDocument?.Elements().Single(e => e.Name.LocalName == "AttachmentBinaryObject" && e.Attribute("uri") is not null).Attribute("uri")!.Value.Split(':').Last();
        var filename = referenceSpsReferencedDocument?.Elements().Single(e => e.Name.LocalName == "AttachmentBinaryObject" && e.Attribute("filename") is not null).Attribute("filename")!.Value;

        resourceContent = resourceContent?.Replace("{{ISSUE_DATETIME}}", issueDateTime);
        resourceContent = resourceContent?.Replace("{{CHED_ID}}", chedId);
        resourceContent = resourceContent?.Replace("{{SCHEME_AGENCY_ID}}", schemeAgencyId);
        resourceContent = resourceContent?.Replace("{{DOCUMENT_ID}}", documentId);
        resourceContent = resourceContent?.Replace("{{FILENAME}}", filename);

        return SoapUtils.StubResponseMessage(statusCode, resourceContent);
    }
}