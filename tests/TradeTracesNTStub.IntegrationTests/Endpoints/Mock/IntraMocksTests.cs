using System.Net;
using System.Text;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Mock;

public class IntraMocksTests
{
    [Fact]
    public async Task GetEuIntraCertificate_ShouldBeOk_AndReturnITAHC()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8085") };
        client.DefaultRequestHeaders.Add("SOAPAction", "\"getEuIntraCertificate\"");

        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              		<h:Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:h="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
                              		<h:Attributes xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              		<h:LanguageCode xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              		<h:WebServiceClientId xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4">TradeTracesPoc</h:WebServiceClientId>
                              	</s:Header>
                              	<s:Body xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                              		<GetEuIntraCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                              			<ID>INTRA.EU.NL.2021.0000001</ID>
                              		</GetEuIntraCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
        var response = await client.PostAsync("/mock", httpContent, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Contain("GetEuIntraCertificateResponse").And.Contain("INTRA.EU.NL.2021.0000001");
    }
    
    [Fact]
    public async Task GetEuIntraCertificateWithNoWebServiceClientIdHeader_ShouldBeServerError_AndReturnUnauthenticatedExceptionResponse()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8085") };
        client.DefaultRequestHeaders.Add("SOAPAction", "\"getEuIntraCertificate\"");

        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              		<h:Security xmlns="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:h="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
                              		<h:Attributes xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              		<h:LanguageCode xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              		<h:WebServiceClientId xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4"></h:WebServiceClientId>
                              	</s:Header>
                              	<s:Body xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                              		<GetEuIntraCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/euintra/v1">
                              			<ID>INTRA.EU.NL.2021.0000001</ID>
                              		</GetEuIntraCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
        var response = await client.PostAsync("/mock", httpContent, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be("""
                                <?xml version='1.0' encoding='UTF-8'?>
                                <env:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/">
                                  <env:Header/>
                                  <env:Body>
                                    <env:Fault>
                                      <faultcode>env:Client</faultcode>
                                      <faultstring>UnauthenticatedException</faultstring>
                                    </env:Fault>
                                  </env:Body>
                                </env:Envelope>
                                """);
    }
}