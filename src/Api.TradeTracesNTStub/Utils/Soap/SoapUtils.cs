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

    public static async Task<ResponseMessage> CreateResponseFromResource(HttpStatusCode statusCode, string resourceName, bool includeEnvelope = false, IRequestMessage? request = null)
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
        // TODO: don't know what the Traces response is yet
        var resourceContent = await GetEmbeddedResource("Api.TradeTracesNTStub.Samples.CHED.CHED.Submitted.TEMPLATE.xml");

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