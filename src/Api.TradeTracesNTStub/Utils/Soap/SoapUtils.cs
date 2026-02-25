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
        if (request is not null)
        {
            var requestedId = GetRequestedId(request!);
            resourceContent = resourceContent?.Replace("{{ID}}", requestedId); // TODO: implement a more generic way of transforming and refactor all this
        }

        var body = includeEnvelope ? $"""
                                      <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                                        <soap:Body>
                                          {resourceContent}
                                        </soap:Body>
                                      </soap:Envelope>
                                      """ : resourceContent;
        
        return new ResponseMessage
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, WireMockList<string>>
            {
                ["Content-Type"] = new("text/xml; charset=utf-8"),
            },
            BodyData = new BodyData
            {
                BodyAsString = body,
                DetectedBodyType = BodyType.String,
            },
        };
    }
}