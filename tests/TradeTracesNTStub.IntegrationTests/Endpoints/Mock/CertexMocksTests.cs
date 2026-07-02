using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Mock
{
    public class CertexMocksTests : IntegrationTestBase
    {
        private async Task<HttpResponseMessage> PostToCertexService(string soapRequestBody, string soapAction)
        {
            Client.DefaultRequestHeaders.Add("SOAPAction", soapAction);

            var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
            return await Client.PostAsync("/mock/tracesnt/ws/CustomsCertexChedServiceV06", httpContent, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ProcessedChedRequest_WithChedInNewStatus_ShouldReturnError_AndReturnChed()
        {
            var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header xmlns:v06="http://ec.europa.eu/sanco/tracesnt/customs_certex/ched/v06" 
                                            xmlns:v03="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03" 
                                            xmlns:v1="http://ec.europa.eu/tracesnt/body/v1" 
                                            xmlns:v3="http://ec.europa.eu/sanco/tracesnt/base/v3">
                                    <v06:CertexHeader>
                                       <v03:MessageId>test</v03:MessageId>
                                       <v03:UniqRequesterPrefix>123456-</v03:UniqRequesterPrefix>
                                    </v06:CertexHeader>
                                    <v1:CustomsOfficeReferenceNumber>XI000002</v1:CustomsOfficeReferenceNumber>
                                      <v3:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</v3:LanguageCode>
                                      <v3:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</v3:WebServiceClientId>
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
                              	<s:Body xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
                                          xmlns:xsd="http://www.w3.org/2001/XMLSchema" 
                                          xmlns:v03="http://ec.europa.eu/sanco/tracesnt/customs_certex/base/v03" 
                                          xmlns:v06="http://ec.europa.eu/sanco/tracesnt/customs_certex/ched/v06">
                                    <v06:ProcessedChedRequest>
                                       <v06:SendingDate>2026-06-30T08:00:53Z</v06:SendingDate>
                                       <v06:ChedCertificateId>CHEDPP.XI.2026.0000074</v06:ChedCertificateId>
                                       <v06:CustomsDeclarationReferenceNumber>
                                          <v03:MRN>26UK12345678900001</v03:MRN>
                                       </v06:CustomsDeclarationReferenceNumber>
                                       <v06:CompetentCustomsOffice>
                                          <v03:ReferenceNumber>XI000002</v03:ReferenceNumber>
                                       </v06:CompetentCustomsOffice>
                                       <v06:CommodityDescriptionForChed>
                                          <v03:GoodsItemNumber>1</v03:GoodsItemNumber>
                                          <v03:CertificateLineNumber>1</v03:CertificateLineNumber>
                                          <v03:ClassCode>08051028</v03:ClassCode>
                                          <v03:NetWeightQuantity>10</v03:NetWeightQuantity>
                                          <v03:NetWeightUnitOfMeasure>KGM</v03:NetWeightUnitOfMeasure>
                                       </v06:CommodityDescriptionForChed>
                                       <v06:Language>EN</v06:Language>
                                       <v06:QuantityManagementIndication>1</v06:QuantityManagementIndication>
                                    </v06:ProcessedChedRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
            soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
                .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            var response = await PostToCertexService(soapRequestBody, "\"processedChedRequest\"");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            await VerifyXml(responseBody);
        }
    }
}
