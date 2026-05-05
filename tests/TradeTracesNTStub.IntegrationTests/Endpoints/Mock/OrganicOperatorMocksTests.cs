using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Mock;

public class OrganicOperatorMocksTests : IntegrationTestBase
{
   
    private async Task<HttpResponseMessage> PostToOperatorService(string soapRequestBody, string soapAction)
    {
        Client.DefaultRequestHeaders.Add("SOAPAction", soapAction);
        
        var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
        return await Client.PostAsync("/mock/tracesnt/ws/OrganicOperatorCertificateRetrievalServiceV1", httpContent, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FindOrganicOperator_WithValidRequest_ShouldBeOk_AndReturnOrganicOperatorList()
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
                                  <FindOrganicOperatorCertificateRequest pageSize="30" xmlns="http://ec.europa.eu/tracesnt/certificate/organicoperator/retrieval/v1">
                                      <Status listID="4405" listAgencyID="6" listVersionID="D16B" xmlns="http://ec.europa.eu/tracesnt/certificate/organicoperator/retrieval/v1">49</Status>
                                      <CreateDateTimeRange xmlns="http://ec.europa.eu/tracesnt/certificate/organicoperator/retrieval/v1">
                                          <From xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">2020-01-04T17:38:09Z</From>
                                          <To xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">2026-04-04T17:38:09Z</To>
                                      </CreateDateTimeRange>
                                      <UpdateDateTimeRange xmlns="http://ec.europa.eu/tracesnt/certificate/organicoperator/retrieval/v1">
                                          <From xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">2020-01-04T17:38:09Z</From>
                                          <To xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">2026-04-04T17:38:09Z</To>
                                      </UpdateDateTimeRange>
                                  </FindOrganicOperatorCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToOperatorService(soapRequestBody, "\"organicOperator\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }

    [Fact]
    public async Task GetOrganicOperator_WithValidRequest_ShouldBeOk_AndReturnOrganicOperator()
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
                                  <GetOrganicOperatorCertificateRequest xmlns="http://ec.europa.eu/tracesnt/certificate/organicoperator/retrieval/v1">
                                      <ID xmlns="http://ec.europa.eu/tracesnt/certificate/organicoperator/retrieval/v1">GB-ORG-07.998-0000052.2024.001</ID>
                                  </GetOrganicOperatorCertificateRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToOperatorService(soapRequestBody, "\"getOrganicOperatorCertificate\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }
}