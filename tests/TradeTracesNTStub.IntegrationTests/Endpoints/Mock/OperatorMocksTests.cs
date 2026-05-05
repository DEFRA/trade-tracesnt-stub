using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Mock;

public class OperatorMocksTests : IntegrationTestBase
{
   
    private async Task<HttpResponseMessage> PostToOperatorService(string soapRequestBody, string soapAction)
    {
        Client.DefaultRequestHeaders.Add("SOAPAction", soapAction);
        
        var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
        return await Client.PostAsync("/mock/tracesnt/ws/OperatorDirectoryServiceV1", httpContent, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateOperator_WithValidRequest_ShouldBeOk_AndReturnOperatorId()
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
                                  <CreateOperatorRequest xmlns="http://ec.europa.eu/tracesnt/directory/operator/v1">
                                      <Operator xmlns="http://ec.europa.eu/tracesnt/directory/operator/v1">
                                          <Name>Traces Test Corp</Name>
                                          <OperatorAddress main="true">
                                              <Address>
                                                  <Street xmlns="http://ec.europa.eu/tracesnt/directory/common/v1">47 Donegall Place</Street>
                                                      <City xmlns="http://ec.europa.eu/tracesnt/directory/common/v1">
                                                      <Name languageID="EN" languageLocaleID="EN" xmlns="http://ec.europa.eu/tracesnt/directory/geo/city/v1">Belfast</Name>
                                                      <PostalCode xmlns="http://ec.europa.eu/tracesnt/directory/geo/city/v1">BT1</PostalCode>
                                                      <CountryID xmlns="http://ec.europa.eu/tracesnt/directory/geo/city/v1">XI</CountryID>
                                                  </City>
                                              </Address>
                                          </OperatorAddress>
                                          <Activity>
                                              <ActivityType>
                                                  <Chapter>veterinary</Chapter>
                                                  <Section>OTH-OPER</Section>
                                                  <Type>importer</Type>
                                              </ActivityType>
                                          </Activity>
                                      </Operator>
                                  </CreateOperatorRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToOperatorService(soapRequestBody, "\"createOperator\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var verifyResponseSettings = new VerifySettings();
        verifyResponseSettings.ScrubLinesWithReplace(line => line.Contains("OperatorInternalID") ? Regex.Replace(line, @"\d{4,8}", "0000000") : line);
        await VerifyXml(responseBody, verifyResponseSettings);
    }

    [Fact]
    public async Task GetOperatorById_WithValidRequest_ShouldBeOk_AndReturnOperator()
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
                                  <GetOperatorRequest xmlns="http://ec.europa.eu/tracesnt/directory/operator/v1">
                                      <ID>363203</ID>
                                  </GetOperatorRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToOperatorService(soapRequestBody, "\"getOperator\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var verifyResponseSettings = new VerifySettings();
        verifyResponseSettings.ScrubLinesWithReplace(line => line.Contains("internalID") ? Regex.Replace(line, @"\d{4,8}", "0000000") : line);
        await VerifyXml(responseBody, verifyResponseSettings);
    }

    [Fact]
    public async Task FindOperator_WithValidRequest_ShouldBeOk_AndReturnOperatorList()
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
                                  <FindOperatorRequest pageSize="30" xmlns="http://ec.europa.eu/tracesnt/directory/operator/v1">
                                      <Name xmlns="http://ec.europa.eu/tracesnt/directory/operator/v1">test</Name>
                                      <CountryID xmlns="http://ec.europa.eu/tracesnt/directory/operator/v1">GB</CountryID>
                                  </FindOperatorRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToOperatorService(soapRequestBody, "\"findOperator\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }
}