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
    public async Task GetClassificationTree_WithInvalidTreeId_ShouldReturnErrorResponse()
    {
        var soapRequestBody = $$$"""
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
                                        <TreeID xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">invalid_treeId</TreeID>
                                    </GetClassificationTreeRequest>
                                </s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToReferenceDataService(soapRequestBody, "\"getClassificationTree\"");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }

    [Theory]
    [InlineData("cheda")]
    [InlineData("intra_trade")]
    public async Task GetClassificationTree_WithValidRequest_ShouldBeOk_AndReturnClassificationTree(string treeId)
    {
        var soapRequestBody = $$$"""
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
                                        <TreeID xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">{{{treeId}}}</TreeID>
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

    [Theory]
    [InlineData("intra_trade", "R/N-10000/N-10065", "intra_N-10065")]
    [InlineData("intra_trade", "R/N-10000/N-10066/C-12151", "intra_c-12151")]
    [InlineData("cheda", "R/N-10000/N-10065", "cheda_N-10065")]
    [InlineData("cheda", "R/N-10002/N-10081/N-10446/N-10685/N-11907", "ched_N-11907")]
    public async Task GetClassificationTreeNodeDetail_WithValidRequest_ShouldBeOk_AndReturnNodeDetail(string treeId, string path, string testcaseMame)
    {
        var soapRequestBody = $$$"""
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
                                        <TreeID xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">{{{treeId}}}</TreeID>
                                        <Path xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">{{{path}}}</Path>
                                    </GetClassificationTreeNodeDetailRequest>
                                </s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToReferenceDataService(soapRequestBody, "\"getClassificationTreeNodeDetail\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody).UseParameters(testcaseMame);
    }

    [Fact]
    public async Task GetClassificationTreeNodeDetail_WithInvalidRequest_ShouldReturnError()
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
                                        <Path xmlns="http://ec.europa.eu/tracesnt/referencedata/v1">invalid_path</Path>
                                    </GetClassificationTreeNodeDetailRequest>
                                </s:Body>
                              </s:Envelope>
                              """;

        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        var response = await PostToReferenceDataService(soapRequestBody, "\"getClassificationTreeNodeDetail\"");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await VerifyXml(responseBody);
    }
}