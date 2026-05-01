using System.Net;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using WireMock;
using WireMock.Types;
using WireMock.Util;

namespace Api.TradeTracesNTStub.Utils.Soap;

public static class SoapUtils
{
    private static readonly Assembly s_assembly = Assembly.GetExecutingAssembly();

    private static Task<string?> GetEmbeddedResource(string resourceName)
    {
        using var stream = s_assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
            return Task.FromResult<string?>(null);

        using var reader = new StreamReader(stream);
        return Task.FromResult<string?>(reader.ReadToEnd());
    }
    
    private static string? GetRequestedId(IRequestMessage request)
    {
        var requestBody = XElement.Parse(request.Body!);
        return requestBody.XPathSelectElement("//*[local-name()='ID']")?.Value;
    }

    private static string GenerateChedId(XElement requestBody)
    {
        var includedSpsNote = requestBody.XPathSelectElement("//*[local-name()='IncludedSPSNote']/*[local-name()='SubjectCode' and text()='CHED_TYPE']")?.Parent;
        var chedType = includedSpsNote?.Elements().FirstOrDefault(e => e.Name.LocalName == "ContentCode")?.Value ?? "A";
        
        var random = new Random().Next(1, 9999999);
        return $"CHED{chedType}.XI.2026.{random.ToString().PadLeft(7, '0')}";
    }
    
    private static string GenerateCentralEuropeanTime()
    {
        var currentCentralEuropeanTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
        return currentCentralEuropeanTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
    }

    public static async Task<ResponseMessage> CreateResponseFromResource(HttpStatusCode statusCode, string resourceName)
    {
        var resourceContent = await GetEmbeddedResource(resourceName);
        
        return StubResponseMessage(statusCode, resourceContent);
    }

    public static async Task<ResponseMessage> CreateItahcResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await GetEmbeddedResource("Api.TradeTracesNTStub.Samples.INTRA.ITAHC.TEMPLATE.xml");
        
        var requestedItahcId = GetRequestedId(request);
        resourceContent = resourceContent?.Replace("{{ID}}", requestedItahcId);
        
        return StubResponseMessage(statusCode, resourceContent);
    }
    
    public static async Task<ResponseMessage> CreateItahcPdfResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await GetEmbeddedResource("Api.TradeTracesNTStub.Samples.INTRA.ITAHC.PDF.TEMPLATE.xml");
        
        var requestedItahcId = GetRequestedId(request);
        resourceContent = resourceContent?.Replace("{{ID}}", requestedItahcId);

        return StubResponseMessage(statusCode, resourceContent);
    }
    
    public static async Task<ResponseMessage> CreateChedAResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.CHEDA.TEMPLATE.xml");
        
        var requestedChedId = GetRequestedId(request);
        resourceContent = resourceContent?.Replace("{{CHED_ID}}", requestedChedId);

        return StubResponseMessage(statusCode, resourceContent);
    }
    
    public static async Task<ResponseMessage> CreateSubmitCertificateAttachmentResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.SubmitCertificateAttachmentResponse.TEMPLATE.xml");
        
        var requestBody = XElement.Parse(request.Body!);
        var filename = requestBody.XPathSelectElement("//*[local-name()='SubmitCertificateAttachmentRequest']")?.Attribute("fileName")?.Value;
        resourceContent = resourceContent?.Replace("{{FILENAME}}", filename);

        return StubResponseMessage(statusCode, resourceContent);
    }
    
    public static async Task<ResponseMessage> CreateGetCertificateAttachmentResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.GetCertificateAttachmentResponse.TEMPLATE.xml");
        
        var requestBody = XElement.Parse(request.Body!);
        var filename = requestBody.XPathSelectElement("//*[local-name()='GetCertificateAttachmentRequest']/*[local-name()='FileName']")?.Value;
        resourceContent = resourceContent?.Replace("{{FILENAME}}", filename);

        return StubResponseMessage(statusCode, resourceContent);
    }

    public static async Task<ResponseMessage> CreateChedSubmittedResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.CHED.Submitted.TEMPLATE.xml");

        var requestBody = XElement.Parse(request.Body!);
        var issueDateTime = GenerateCentralEuropeanTime();
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

        return StubResponseMessage(statusCode, resourceContent);
    }

    public static async Task<ResponseMessage> CreateOperatorCreatedResponse(HttpStatusCode statusCode, IRequestMessage request)
    {
        var resourceContent = await GetEmbeddedResource("Api.TradeTracesNTStub.Samples.OPERATOR.Operator.Created.TEMPLATE.xml");
        var operatorId = new Random().Next(1, 9999999);  
       
        resourceContent = resourceContent?.Replace("{{OPERATOR_ID}}", operatorId.ToString());
        
        return StubResponseMessage(statusCode, resourceContent);
    }

    private static ResponseMessage StubResponseMessage(HttpStatusCode statusCode, string? resourceContent)
    {
        return new ResponseMessage
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, WireMockList<string>>
            {
                ["Content-Type"] = new("text/xml; charset=utf-8"),
            },
            BodyData = new BodyData
            {
                BodyAsString = resourceContent,
                DetectedBodyType = BodyType.String,
            },
        };
    }
}