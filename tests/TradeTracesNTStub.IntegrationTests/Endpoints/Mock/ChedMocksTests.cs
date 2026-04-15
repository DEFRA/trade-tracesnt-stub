using System.Net;
using System.Text;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Mock;

public class ChedMocksTests
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri("http://localhost:8080") };
    
    private async Task<HttpResponseMessage> PostToChedCertificateService(string soapRequestBody, string soapAction)
    {
        _client.DefaultRequestHeaders.Add("SOAPAction", soapAction);
        
        var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
        return await _client.PostAsync("/mock/tracesnt/ws/ChedCertificateServiceV2", httpContent, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetChedCertificate_WithValidRequest_ShouldBeOk_AndReturnChed()
    {
        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              		<h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              		<h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              		<h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</h:WebServiceClientId>
                              		<Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                              			<wsse:UsernameToken wsu:Id="20E0E1B0C5764059A3F84975AE421E55" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsse:Username>FOO</wsse:Username>
                              				<wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">FOO</wsse:Password>
                              				<wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">FOO</wsse:Nonce>
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              			</wsse:UsernameToken>
                              			<wsu:Timestamp wsu:Id="TS-C7215B1871604A45B3AD072621CC75E7" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              				<wsu:Expires>{{EXPIRED}}</wsu:Expires>
                              			</wsu:Timestamp>
                              		</Security>
                              	</s:Header>
                              	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                              		<GetChedCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/ched/v2">
                              			<ID>CHEDA.XI.2026.0000012</ID>
                              		</GetChedCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToChedCertificateService(soapRequestBody, "\"getChedCertificate\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
}