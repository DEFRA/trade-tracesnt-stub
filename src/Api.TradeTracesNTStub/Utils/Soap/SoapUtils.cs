using System.Net;
using System.Reflection;
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

    public static async Task<WireMock.ResponseMessage> CreateResponseFromResource(HttpStatusCode statusCode, string resourceName, bool includeEnvelope = true)
    {
        var resourceContent = await GetEmbeddedResource(resourceName);
        var body = includeEnvelope ? $"""
                                      <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                                        <soap:Body>
                                          {resourceContent}
                                        </soap:Body>
                                      </soap:Envelope>
                                      """ : resourceContent;
        
        return new()
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