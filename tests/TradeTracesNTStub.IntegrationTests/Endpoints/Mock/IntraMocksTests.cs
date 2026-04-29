using System.Net;
using System.Text;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Mock;

public class IntraMocksTests
{
    private const string InvalidCreatedSoapRequestBody = """
                                                          <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                                                          	<s:Header>
                                                          		<h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                                                          		<h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                                                          		<h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</h:WebServiceClientId>
                                                          		<Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                                                          			<wsse:UsernameToken wsu:Id="DD4769DABBF049E5A778D55B17083811" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                                                          				<wsse:Username>FOO</wsse:Username>
                                                          				<wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">FOO</wsse:Password>
                                                          				<wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">FOO</wsse:Nonce>
                                                          				<wsu:Created>{{USERNAME_TOKEN_CREATED}}</wsu:Created>
                                                          			</wsse:UsernameToken>
                                                          			<wsu:Timestamp wsu:Id="TS-DB41B4C7A5144529B653CAE9A072D1F7" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                                                          				<wsu:Created>{{CREATED}}</wsu:Created>
                                                          				<wsu:Expires>{{EXPIRED}}</wsu:Expires>
                                                          			</wsu:Timestamp>
                                                          		</Security>
                                                          	</s:Header>
                                                          	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                                                          		<GetEuIntraCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                                                          			<ID>INTRA.EU.NL.2021.0000001</ID>
                                                          		</GetEuIntraCertificateRequest>
                                                          	</s:Body>
                                                          </s:Envelope>
                                                          """;

    private readonly HttpClient _client = new() { BaseAddress = new Uri("http://localhost:8080") };

    private async Task<HttpResponseMessage> PostToEuIntraCertificateService(string soapRequestBody, string soapAction = "\"getEuIntraCertificate\"")
    {
        _client.DefaultRequestHeaders.Add("SOAPAction", soapAction);
        
        var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
        return await _client.PostAsync("/mock/tracesnt/ws/EuIntraCertificateServiceV1", httpContent, TestContext.Current.CancellationToken);
    }
    
    [Fact]
    public async Task GetEuIntraCertificate_WithValidRequest_ShouldBeOk_AndReturnITAHC()
    {
        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              		<h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              		<h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              		<h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</h:WebServiceClientId>
                              		<Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                              			<wsse:UsernameToken wsu:Id="DD4769DABBF049E5A778D55B17083811" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsse:Username>FOO</wsse:Username>
                              				<wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">FOO</wsse:Password>
                              				<wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">FOO</wsse:Nonce>
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              			</wsse:UsernameToken>
                              			<wsu:Timestamp wsu:Id="TS-DB41B4C7A5144529B653CAE9A072D1F7" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              				<wsu:Expires>{{EXPIRED}}</wsu:Expires>
                              			</wsu:Timestamp>
                              		</Security>
                              	</s:Header>
                              	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                              		<GetEuIntraCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                              			<ID>INTRA.EU.NL.2021.0000001</ID>
                              		</GetEuIntraCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }

    [Theory]
    [InlineData("", "FOO", "FOO", "FOO")]
    [InlineData("FOO", "", "FOO", "FOO")]
    [InlineData("FOO", "FOO", "", "FOO")]
    [InlineData("FOO", "FOO", "FOO", "")]
    public async Task GetEuIntraCertificateWithInvalidSecurityHeaders_ShouldBeServerError_AndReturnUnauthenticatedExceptionResponse(
        string webServiceClientId,
        string username,
        string password,
        string nonce)
    {
        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              		<h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              		<h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              		<h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">{{WEBSERVICE_CLIENT_ID}}</h:WebServiceClientId>
                              		<Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                              			<wsse:UsernameToken wsu:Id="DD4769DABBF049E5A778D55B17083811" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsse:Username>{{USERNAME}}</wsse:Username>
                              				<wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">{{PASSWORD}}</wsse:Password>
                              				<wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">{{NONCE}}</wsse:Nonce>
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              			</wsse:UsernameToken>
                              			<wsu:Timestamp wsu:Id="TS-DB41B4C7A5144529B653CAE9A072D1F7" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              				<wsu:Expires>{{EXPIRED}}</wsu:Expires>
                              			</wsu:Timestamp>
                              		</Security>
                              	</s:Header>
                              	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                              		<GetEuIntraCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                              			<ID>INTRA.EU.NL.2021.0000001</ID>
                              		</GetEuIntraCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{WEBSERVICE_CLIENT_ID}}", webServiceClientId)
            .Replace("{{USERNAME}}", username)
            .Replace("{{PASSWORD}}", password)
            .Replace("{{NONCE}}", nonce);
        
        var response = await PostToEuIntraCertificateService(soapRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
    
    [Fact]
    public async Task GetEuIntraCertificateWithInvalidUsernameTokenCreatedHeader_ShouldBeServerError_AndReturnUnauthenticatedExceptionResponse()
    {
        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              		<h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              		<h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              		<h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</h:WebServiceClientId>
                              		<Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                              			<wsse:UsernameToken wsu:Id="DD4769DABBF049E5A778D55B17083811" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsse:Username>FOO</wsse:Username>
                              				<wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">FOO</wsse:Password>
                              				<wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">FOO</wsse:Nonce>
                              				<wsu:Created></wsu:Created>
                              			</wsse:UsernameToken>
                              			<wsu:Timestamp wsu:Id="TS-DB41B4C7A5144529B653CAE9A072D1F7" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              				<wsu:Expires>{{EXPIRED}}</wsu:Expires>
                              			</wsu:Timestamp>
                              		</Security>
                              	</s:Header>
                              	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                              		<GetEuIntraCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                              			<ID>INTRA.EU.NL.2021.0000001</ID>
                              		</GetEuIntraCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
    
    [Fact]
    public async Task GetEuIntraCertificateWithTimestampCreatedHeaderGreaterThan60SecondsInThePast_ShouldBeServerError_AndReturnUnauthenticatedExceptionResponse()
    {
        var soapRequestBody = InvalidCreatedSoapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.AddSeconds(-65).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{USERNAME_TOKEN_CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
    
    [Fact]
    public async Task GetEuIntraCertificateWithTimestampCreatedHeaderGreaterThanExpired_ShouldBeServerError_AndReturnUnauthenticatedExceptionResponse()
    {
        var soapRequestBody = InvalidCreatedSoapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.AddSeconds(65).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{USERNAME_TOKEN_CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
    
    [Fact]
    public async Task GetEuIntraCertificateWithUsernameTokenCreatedHeaderOlderThanTimestampCreated_ShouldBeServerError_AndReturnUnauthenticatedExceptionResponse()
    {
        var soapRequestBody = InvalidCreatedSoapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{USERNAME_TOKEN_CREATED}}", DateTime.UtcNow.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
    
    [Fact]
    public async Task GetEuIntraCertificateWithUsernameTokenCreatedHeaderGreaterThanTimestampExpired_ShouldBeServerError_AndReturnUnauthenticatedExceptionResponse()
    {
        var soapRequestBody = InvalidCreatedSoapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{USERNAME_TOKEN_CREATED}}", DateTime.UtcNow.AddSeconds(65).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
    
    [Fact]
    public async Task GetEuIntraCertificateWithInvalidGetEuIntraCertificateId_ShouldBeServerError_AndReturnGetEuIntraCertificateInvalidIdResponse()
    {
        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              		<h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              		<h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              		<h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</h:WebServiceClientId>
                              		<Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                              			<wsse:UsernameToken wsu:Id="DD4769DABBF049E5A778D55B17083811" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsse:Username>FOO</wsse:Username>
                              				<wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">FOO</wsse:Password>
                              				<wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">FOO</wsse:Nonce>
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              			</wsse:UsernameToken>
                              			<wsu:Timestamp wsu:Id="TS-DB41B4C7A5144529B653CAE9A072D1F7" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              				<wsu:Expires>{{EXPIRED}}</wsu:Expires>
                              			</wsu:Timestamp>
                              		</Security>
                              	</s:Header>
                              	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                              		<GetEuIntraCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                              			<ID/>
                              		</GetEuIntraCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
    
    [Fact]
    public async Task GetEuIntraPdfCertificate_WithValidRequest_ShouldBeOk_AndReturnITAHCPdf()
    {
        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              		<h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              		<h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              		<h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</h:WebServiceClientId>
                              		<Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                              			<wsse:UsernameToken wsu:Id="DD4769DABBF049E5A778D55B17083811" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsse:Username>FOO</wsse:Username>
                              				<wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">FOO</wsse:Password>
                              				<wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">FOO</wsse:Nonce>
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              			</wsse:UsernameToken>
                              			<wsu:Timestamp wsu:Id="TS-DB41B4C7A5144529B653CAE9A072D1F7" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              				<wsu:Created>{{CREATED}}</wsu:Created>
                              				<wsu:Expires>{{EXPIRED}}</wsu:Expires>
                              			</wsu:Timestamp>
                              		</Security>
                              	</s:Header>
                              	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                              		<GetEuIntraPdfCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                              			<ID>INTRA.EU.NL.2021.0000001</ID>
                              		</GetEuIntraPdfCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody, "\"getEuIntraPdfCertificate\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
    
    [Fact]
    public async Task FindEuIntraCertificate_WithValidRequest_ShouldBeOk_AndReturnListOfEuIntraCertificateResults()
    {
        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              	<h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              	<h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              	<h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</h:WebServiceClientId>
                              	<Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
                              		<wsse:UsernameToken wsu:Id="DD4769DABBF049E5A778D55B17083811" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              			<wsse:Username>FOO</wsse:Username>
                              			<wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">FOO</wsse:Password>
                              			<wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">FOO</wsse:Nonce>
                              			<wsu:Created>{{CREATED}}</wsu:Created>
                              		</wsse:UsernameToken>
                              		<wsu:Timestamp wsu:Id="TS-DB41B4C7A5144529B653CAE9A072D1F7" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                              			<wsu:Created>{{CREATED}}</wsu:Created>
                              			<wsu:Expires>{{EXPIRED}}</wsu:Expires>
                              		</wsu:Timestamp>
                              	</Security>
                              </s:Header>
                              	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                              		<FindEuIntraCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                              			<CreateDateTimeRange>
                              				<From xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">2026-04-08T13:19:21.172361+01:00</From>
                              				<To xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">2026-04-22T13:19:21.172446+01:00</To>
                              			</CreateDateTimeRange>
                              		</FindEuIntraCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToEuIntraCertificateService(soapRequestBody, "\"findEuIntraCertificate\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await VerifyXml(responseBody);
    }
}
