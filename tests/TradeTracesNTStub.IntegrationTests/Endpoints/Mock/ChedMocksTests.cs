using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Mock;

public class ChedMocksTests : IntegrationTestBase
{
    private async Task<HttpResponseMessage> PostToChedCertificateService(string soapRequestBody, string soapAction)
    {
        Client.DefaultRequestHeaders.Add("SOAPAction", soapAction);
        
        var httpContent = new StringContent(soapRequestBody, Encoding.UTF8, "application/xml");
        return await Client.PostAsync("/mock/tracesnt/ws/ChedCertificateServiceV2", httpContent, TestContext.Current.CancellationToken);
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

    [Fact]
    public async Task ChedCreateAndSubmitForDecision_WithValidRequest_ShouldBeOk_AndReturnChedDetails()
    {
        var soapRequestBody = """
                              <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                              	<s:Header>
                              	    <h:Attributes xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4"/>
                              	    <h:LanguageCode xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">EN</h:LanguageCode>
                              	    <h:WebServiceClientId xmlns:h="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">FOO</h:WebServiceClientId>
                              	    <h:BodyIdentity xmlns:h="http://ec.europa.eu/tracesnt/body/v3" xmlns="http://ec.europa.eu/tracesnt/body/v3">
                                        <AuthorityActivityAccessIdentifier>FOO</AuthorityActivityAccessIdentifier>
                                    </h:BodyIdentity>
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
                              		<CreateAndSubmitChedForDecisionRequest xmlns="http://ec.europa.eu/tracesnt/certificate/ched/submission/v2">
                              			<SPSCertificate xmlns="urn:un:unece:uncefact:data:standard:SPSCertificate:17">
                              				<SPSExchangedDocument>
                              					<Name languageID="en" xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">CHED-A - Common Health Entry Document for Animal</Name>
                              					<ID xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21"/>
                              					<TypeCode listID="1001" listVersionID="D16B" xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">636</TypeCode>
                              					<StatusCode listID="4405" listVersionID="D16B" xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">1</StatusCode>
                              					<IssueDateTime xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<DateTime xmlns="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:21">2026-04-20T14:00:00+01:00</DateTime>
                              					</IssueDateTime>
                              					<IncludedSPSNote xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ContentCode>A</ContentCode>
                              						<Content/>
                              						<SubjectCode>CHED_TYPE</SubjectCode>
                              					</IncludedSPSNote>
                              					<ReferenceSPSReferencedDocument xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<TypeCode listID="1001" listVersionID="D16B" name="Health certificate (Health certificate)">636</TypeCode>
                              						<RelationshipTypeCode listID="1153_ReferenceTypeCode" listVersionID="D16B" name="Mutually defined reference number (Supporting document)">ZZZ</RelationshipTypeCode>
                              						<ID schemeAgencyID="AL">123456</ID>
                              						<AttachmentBinaryObject uri="uri:documentid:775548" filename="document.pdf"/>
                              					</ReferenceSPSReferencedDocument>
                              					<SignatorySPSAuthentication xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<TypeCode listID="9417" listVersionID="D16B" name="Inspection (Identification of Applicant)">4</TypeCode>
                              						<ActualDateTime>
                              							<DateTime xmlns="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:21">2026-04-21T11:00:00+01:00</DateTime>
                              						</ActualDateTime>
                              						<ProviderSPSParty>
                              							<Name/>
                              						</ProviderSPSParty>
                              						<IncludedSPSClause>
                              							<ID>PURPOSE</ID>
                              							<Content>FREE_CIRCULATION</Content>
                              						</IncludedSPSClause>
                              						<IncludedSPSClause>
                              							<ID>GOODS_CERTIFIED_AS</ID>
                              							<Content>FATTENING</Content>
                              						</IncludedSPSClause>
                              					</SignatorySPSAuthentication>
                              				</SPSExchangedDocument>
                              				<SPSConsignment>
                              					<AvailabilityDueDateTime xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<DateTime xmlns="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:21">2026-04-23T00:00:00+01:00</DateTime>
                              					</AvailabilityDueDateTime>
                              					<ConsignorSPSParty xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID schemeID="operator_internal_activity_id">899361</ID>
                              						<Name/>
                              						<SpecifiedSPSAddress>
                              							<PostcodeCode>BT9</PostcodeCode>
                              							<LineOne>Test Street 876</LineOne>
                              							<CityName languageID="en">Belfast</CityName>
                              							<CountryID>XI</CountryID>
                              							<CountryName languageID="en">United Kingdom (Northern Ireland)</CountryName>
                              							<CountrySubDivisionName languageID="en">County Antrim</CountrySubDivisionName>
                              						</SpecifiedSPSAddress>
                              					</ConsignorSPSParty>
                              					<ConsigneeSPSParty xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID schemeID="operator_internal_activity_id">899361</ID>
                              						<Name/>
                              						<SpecifiedSPSAddress>
                              							<PostcodeCode>BT9</PostcodeCode>
                              							<LineOne>Test Street 876</LineOne>
                              							<CityName languageID="en">Belfast</CityName>
                              							<CountryID>XI</CountryID>
                              							<CountryName languageID="en">United Kingdom (Northern Ireland)</CountryName>
                              							<CountrySubDivisionName languageID="en">County Antrim</CountrySubDivisionName>
                              						</SpecifiedSPSAddress>
                              					</ConsigneeSPSParty>
                              					<ExportSPSCountry xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID/>
                              						<Name/>
                              					</ExportSPSCountry>
                              					<ImportSPSCountry xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID/>
                              						<Name/>
                              					</ImportSPSCountry>
                              					<TransitSPSCountry xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID/>
                              						<Name/>
                              						<SubordinateSPSCountrySubDivision>
                              							<Name/>
                              							<HierarchicalLevelCode name="None">0</HierarchicalLevelCode>
                              							<FunctionTypeCode listID="3227" listVersionID="D16B" name="Goods disposal location, designated">283</FunctionTypeCode>
                              						</SubordinateSPSCountrySubDivision>
                              					</TransitSPSCountry>
                              					<UnloadingBaseportSPSLocation xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID schemeID="un_locode" schemeName="UN/LOCODE" schemeAgencyID="un" schemeAgencyName="United Nations" schemeDataURI="authority_activity">GBBEL</ID>
                              						<Name>XI</Name>
                              						<Name languageID="en">County Down</Name>
                              						<Name>XIBEL1-DAERA</Name>
                              						<Name>Belfast Port</Name>
                              						<Name>Belfast City Council 5 Corry Place Belfast Harbour Estate BT3 9HY</Name>
                              						<Name>GBBEL</Name>
                              					</UnloadingBaseportSPSLocation>
                              					<ExaminationSPSEvent xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<OccurrenceSPSLocation>
                              							<Name/>
                              						</OccurrenceSPSLocation>
                              					</ExaminationSPSEvent>
                              					<DeliverySPSParty xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID schemeID="operator_internal_activity_id">899361</ID>
                              						<Name/>
                              						<SpecifiedSPSAddress>
                              							<PostcodeCode>BT9</PostcodeCode>
                              							<LineOne>Test Street 876</LineOne>
                              							<CityName languageID="en">Belfast</CityName>
                              							<CountryID>XI</CountryID>
                              							<CountryName languageID="en">United Kingdom (Northern Ireland)</CountryName>
                              							<CountrySubDivisionName languageID="en">County Antrim</CountrySubDivisionName>
                              						</SpecifiedSPSAddress>
                              					</DeliverySPSParty>
                              					<CustomsTransitAgentSPSParty xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID schemeID="operator_internal_activity_id" schemeName="Operator internal activity ID" schemeAgencyID="ec_sante_traces" schemeAgencyName="European commission - DG SANTE - Traces">883977</ID>
                              						<Name>TEST</Name>
                              						<SpecifiedSPSAddress>
                              							<PostcodeCode>1118</PostcodeCode>
                              							<LineOne>HANDELSKADE 1</LineOne>
                              							<CityName languageID="en">Schiphol</CityName>
                              							<CountryID>NL</CountryID>
                              							<CountryName languageID="en">Netherlands</CountryName>
                              							<CountrySubDivisionName languageID="en">North Holland</CountrySubDivisionName>
                              						</SpecifiedSPSAddress>
                              					</CustomsTransitAgentSPSParty>
                              					<MainCarriageSPSTransportMovement xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<ID schemeID="road_vehicle_registration_before_bcp" schemeName="Road vehicle registration (before BCP)" schemeAgencyID="LV" schemeAgencyName="Latvia">GH 2357</ID>
                              						<ModeCode listID="Recommendation 19" listVersionID="2" name="Road transport">3</ModeCode>
                              					</MainCarriageSPSTransportMovement>
                              					<IncludedSPSConsignmentItem xmlns="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21">
                              						<NatureIdentificationSPSCargo>
                              							<TypeCode listID="7085" listVersionID="D16B" name="General cargo (Commodities)">12</TypeCode>
                              						</NatureIdentificationSPSCargo>
                              						<IncludedSPSTradeLineItem>
                              							<SequenceNumeric>0</SequenceNumeric>
                              							<Description>Consignment totals and summary</Description>
                              							<NetWeightMeasure>0</NetWeightMeasure>
                              							<GrossWeightMeasure unitCode="KGM">50</GrossWeightMeasure>
                              							<NetVolumeMeasure unitCode="H87">1</NetVolumeMeasure>
                              							<PhysicalSPSPackage>
                              								<LevelCode name="No packaging hierarchy">4</LevelCode>
                              								<TypeCode listID="7065" listVersionID="2006">NA</TypeCode>
                              								<ItemQuantity>0.0</ItemQuantity>
                              							</PhysicalSPSPackage>
                              						</IncludedSPSTradeLineItem>
                              						<IncludedSPSTradeLineItem>
                              							<SequenceNumeric>1</SequenceNumeric>
                              							<Description/>
                              							<ScientificName languageID="la">Bison</ScientificName>
                              							<NetVolumeMeasure unitCode="H87">1</NetVolumeMeasure>
                              							<AdditionalInformationSPSNote>
                              								<Content>BOB</Content>
                              								<SubjectCode listID="ched_commodity_note_subject_code" listName="CHED Commodity Note SubjectCode list" name="Individual identification number">INDIVIDUAL_IDENTIFICATION_NUMBER</SubjectCode>
                              							</AdditionalInformationSPSNote>
                              							<ApplicableSPSClassification>
                              								<SystemID>CN</SystemID>
                              								<SystemName>CN Code (Combined Nomenclature)</SystemName>
                              								<ClassCode>0102</ClassCode>
                              								<ClassName languageID="en">LIVE ANIMALS</ClassName>
                              								<ClassName languageID="en">Live bovine animals</ClassName>
                              							</ApplicableSPSClassification>
                              							<ApplicableSPSClassification>
                              								<SystemID>IDENTIFICATION_SYSTEM</SystemID>
                              								<SystemName>Identification system</SystemName>
                              								<ClassCode>TATTOO</ClassCode>
                              								<ClassName languageID="en">Tattoo</ClassName>
                              							</ApplicableSPSClassification>
                              							<OriginSPSCountry>
                              								<ID>AD</ID>
                              								<Name languageID="en">Andorra</Name>
                              							</OriginSPSCountry>
                              						</IncludedSPSTradeLineItem>
                              					</IncludedSPSConsignmentItem>
                              				</SPSConsignment>
                              			</SPSCertificate>
                              		</CreateAndSubmitChedForDecisionRequest>
                              	</s:Body>
                              </s:Envelope>
                              """;
        soapRequestBody = soapRequestBody.Replace("{{CREATED}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            .Replace("{{EXPIRED}}", DateTime.UtcNow.AddSeconds(60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        
        var response = await PostToChedCertificateService(soapRequestBody, "\"createAndSubmitForDecision\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var verifyResponseSettings = new VerifySettings();
        verifyResponseSettings.ScrubLinesWithReplace(line => line.Contains("CHEDA.XI.2026") ? Regex.Replace(line, @"CHEDA\.XI\.2026\.\d{7}", "CHEDA.XI.2026.XXXXXXX") : line);
        await VerifyXml(responseBody, verifyResponseSettings);
    }
}