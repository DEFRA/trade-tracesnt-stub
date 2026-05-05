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

    public static Task<string?> GetEmbeddedResource(string resourceName)
    {
        using var stream = s_assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
            return Task.FromResult<string?>(null);

        using var reader = new StreamReader(stream);
        return Task.FromResult<string?>(reader.ReadToEnd());
    }
    
    public static string? GetRequestedId(IRequestMessage request)
    {
        var requestBody = XElement.Parse(request.Body!);
        return requestBody.XPathSelectElement("//*[local-name()='ID']")?.Value;
    }

    public static string GenerateCentralEuropeanTime()
    {
        var currentCentralEuropeanTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
        return currentCentralEuropeanTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
    }

    public static async Task<ResponseMessage> CreateResponseFromResource(HttpStatusCode statusCode, string resourceName)
    {
        var resourceContent = await GetEmbeddedResource(resourceName);
        
        return StubResponseMessage(statusCode, resourceContent);
    }

    public static ResponseMessage StubResponseMessage(HttpStatusCode statusCode, string? resourceContent)
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