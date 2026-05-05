using FluentAssertions;
using System.Net;
using System.Text;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Mock;

public class ReferenceDataMocksTests : IntegrationTestBase
{
   
    private async Task<HttpResponseMessage> PostToReferenceDataService(string soapRequestBody, string soapAction)
    {
        Client.DefaultRequestHeaders.Add("SOAPAction", soapAction);
        
        var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
        return await Client.PostAsync("/mock/tracesnt/ws/ReferenceDataServiceV1", httpContent, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetClassificationSections_WithValidRequest_ShouldBeOk_AndReturnClassificationSections()
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
                                    <GetClassificationSectionsRequest xmlns="http://ec.europa.eu/tracesnt/referencedata/v1"/>
                                </s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToReferenceDataService(soapRequestBody, "\"getClassificationSections\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }

    [Fact]
    public async Task GetClassificationTrees_WithValidRequest_ShouldBeOk_AndReturnClassificationTrees()
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
                                    <GetClassificationTreesRequest xmlns="http://ec.europa.eu/tracesnt/referencedata/v1"/>
                                </s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToReferenceDataService(soapRequestBody, "\"getClassificationTrees\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }

    [Fact]
    public async Task GetClassificationTree_WithValidRequest_ShouldBeOk_AndReturnClassificationTree()
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
                                    <GetClassificationTreeRequest xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">
                                        <TreeID xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">cheda</TreeID>
                                    </GetClassificationTreeRequest>
                                </s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToReferenceDataService(soapRequestBody, "\"getClassificationTree\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }

    [Fact]
    public async Task GetClassificationTreeNodeDetail_ByCNCode_WithValidRequest_ShouldBeOk_AndReturnClassificationTreeDetail()
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
                              		<GetClassificationTreeNodeDetailRequest xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">
                                        <TreeID xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">cheda</TreeID>
                                        <CNCode xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">03019985</CNCode>
                                    </GetClassificationTreeNodeDetailRequest>
                                </s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToReferenceDataService(soapRequestBody, "\"getClassificationTreeNodeDetail\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }

    [Fact]
    public async Task GetClassificationTreeNodeDetail_ByPath_WithValidRequest_ShouldBeOk_AndReturnClassificationTreeDetail()
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
                              		<GetClassificationTreeNodeDetailRequest xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">
                                        <TreeID xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">cheda</TreeID>
                                        <Path xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">R/N-10002/N-10081/N-10445/N-19996/N-11890</Path>
                                    </GetClassificationTreeNodeDetailRequest>
                                </s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToReferenceDataService(soapRequestBody, "\"getClassificationTreeNodeDetail\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }
}