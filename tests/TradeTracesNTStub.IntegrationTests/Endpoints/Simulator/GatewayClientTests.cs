using System.ServiceModel;
using System.ServiceModel.Channels;
using Api.TradeTracesNTStub.TestKit;
using TracesNT;
using TracesNT.ClientBehaviours;
using TracesNT.WebServices;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// Drives the simulator with Trade Gateway's own generated WCF clients, configured exactly as the
/// gateway configures them. This is the test that proves the epic's premise: the gateway reaches the
/// simulator by pointing <c>TRACESNT__BASEURL</c> at it and changing nothing else.
/// </summary>
/// <remarks>
/// A binding or address mismatch surfaces here as a transport or serialisation error rather than a
/// SOAP fault, so asserting that a well-formed fault comes back is what makes the test meaningful.
/// </remarks>
[Trait("Category", "IntegrationTest")]
[Collection(SimulatorStateCollection.Name)]
public class GatewayClientTests
{
    private const string BaseUrl = "http://localhost:8085";
    private const string Mrn = "26GB00000000000001";

    private static readonly TracesNtCredentials s_default = new()
    {
        Username = "simulator-user",
        AuthenticationKey = "simulator-auth-key",
        WebServiceClientId = "simulator-client-id",
    };

    private static readonly TracesNtCredentials s_customs = new()
    {
        Username = "simulator-customs-user",
        AuthenticationKey = "simulator-customs-auth-key",
        WebServiceClientId = "simulator-customs-client-id",
    };

    [Fact]
    public async Task ChedPort_IsReachable_AndServesCheds()
    {
        // getChedCertificate has real behaviour now, so reachability is proved by the typed
        // not-found fault for a CHED nobody created. The round trip itself is in ChedRetrievalTests.
        var client = Client<ChedCertificatePortClient, ChedCertificatePort>(
            "ChedCertificateServiceV2",
            s_default,
            (binding, address) => new ChedCertificatePortClient(binding, address)
        );

        var act = () =>
            client.getChedCertificateAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetChedCertificateRequestType { ID = "CHEDA.GB.2026.0000001" }
            );

        await act.Should().ThrowAsync<FaultException<ChedCertificateNotFoundExceptionType>>();
    }

    [Fact]
    public async Task ChedPort_StillReportsNotImplemented_ForOperationsWithNoBehaviour()
    {
        var client = Client<ChedCertificatePortClient, ChedCertificatePort>(
            "ChedCertificateServiceV2",
            s_default,
            (binding, address) => new ChedCertificatePortClient(binding, address)
        );

        var act = () =>
            client.getChedFollowUpAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetChedFollowUpRequestType { ID = "CHEDA.GB.2026.0000001" }
            );

        await ShouldReportNotImplemented(act, "getChedFollowUp");
    }

    [Fact]
    public async Task EuIntraPort_IsReachable_AndServesIntras()
    {
        // As for the CHED port: a typed not-found fault for an INTRA nobody created proves the port
        // is wired up. The round trip itself is in IntraRetrievalTests.
        var client = Client<EuIntraCertificatePortClient, EuIntraCertificatePort>(
            "EuIntraCertificateServiceV1",
            s_default,
            (binding, address) => new EuIntraCertificatePortClient(binding, address)
        );

        var act = () =>
            client.getEuIntraCertificateAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetEuIntraCertificateRequestType { ID = "INTRA.GB.2026.0000001" }
            );

        await act.Should().ThrowAsync<FaultException<EuIntraCertificateNotFoundExceptionType>>();
    }

    [Fact]
    public async Task EuIntraPort_StillReportsNotImplemented_ForOperationsWithNoBehaviour()
    {
        var client = Client<EuIntraCertificatePortClient, EuIntraCertificatePort>(
            "EuIntraCertificateServiceV1",
            s_default,
            (binding, address) => new EuIntraCertificatePortClient(binding, address)
        );

        var act = () =>
            client.getEuIntraPdfCertificateAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetEuIntraPdfCertificateRequestType { ID = "INTRA.GB.2026.0000001" }
            );

        await ShouldReportNotImplemented(act, "getEuIntraPdfCertificate");
    }

    [Fact]
    public async Task DocomPort_IsReachable_AndReportsNotImplemented()
    {
        var client = Client<DocomCertificateRetrievalPortClient, DocomCertificateRetrievalPort>(
            "DocomCertificateRetrievalServiceV1",
            s_default,
            (binding, address) => new DocomCertificateRetrievalPortClient(binding, address)
        );

        var act = () =>
            client.getDocomCertificateAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetDocomCertificateRequestType { ID = "DOCOM.GB.2026.0000001" }
            );

        await ShouldReportNotImplemented(act, "getDocomCertificate");
    }

    [Fact]
    public async Task ReferenceDataPort_IsReachable_AndReportsNotImplemented()
    {
        var client = Client<ReferenceDataPortClient, ReferenceDataPort>(
            "ReferenceDataServiceV1",
            s_default,
            (binding, address) => new ReferenceDataPortClient(binding, address)
        );

        var act = () =>
            client.getClassificationSectionsAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                new GetClassificationSectionsRequestType()
            );

        await ShouldReportNotImplemented(act, "getClassificationSections");
    }

    [Fact]
    public async Task CustomsPort_ReservesReadsAndReleases()
    {
        var token = TestContext.Current.CancellationToken;
        var chedId = await SimulatorControlClient
            .At(BaseUrl)
            .CreateChed(
                Ched.ChedA()
                    .WithStatus("VALIDATED")
                    .WithConsignment(consignment =>
                        consignment
                            .ArrivingAt("GBBEL", "XI")
                            .ExportedFrom("AF")
                            .ImportedTo("XI")
                            .WithCommodity(commodity => commodity.CnCode("0101").OriginCountry("AF").NetWeightKg(900))
                    ),
                token
            );
        var client = Client<CustomsCertexChedPortClient, CustomsCertexChedPort>(
            "CustomsCertexChedServiceV06",
            s_customs,
            (binding, address) => new CustomsCertexChedPortClient(binding, address)
        );

        var reserved = await Processed(client, chedId, "1", Mrn, [Item(300)]);
        var read = await Processed(client, chedId, "0", "", null);
        var released = await client.chedClearanceRequestAsync(
            new SecurityHeaderType(),
            s_customs.WebServiceClientId,
            ISO2AlphaLanguageCodeContentType.en,
            "GBTEST01",
            Header,
            new ChedClearanceRequestType
            {
                ChedCertificateId = chedId,
                CustomsDocumentReference = Mrn,
                CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = "GBTEST01" },
                GoodsClearanceInformation = GoodsClearanceInformationType.Item01,
                SendingDate = DateTime.UtcNow,
            }
        );

        reserved.ProcessedChedInformationResponse1.ReservationResult.Should().BeTrue();
        reserved.CertexHeader.MessageId.Should().Be(Header.MessageId);
        read.ProcessedChedInformationResponse1.QuantityManagementSummary.AvailableQuantity.Single()
            .SwSupportingDocument.Quantity.Should()
            .Be(600m);
        read.ProcessedChedInformationResponse1.QuantityManagementSummary.ReservedQuantity.Single()
            .Item.Should()
            .Be(Mrn);
        released.ChedClearanceResponse1.QuantityManagementOutcome.Should().Be("01");
    }

    [Fact]
    public async Task CustomsPort_ReservesAnimalsInPieces()
    {
        var token = TestContext.Current.CancellationToken;
        var chedId = await SimulatorControlClient
            .At(BaseUrl)
            .CreateChed(
                Ched.ChedA()
                    .WithStatus("VALIDATED")
                    .WithConsignment(consignment =>
                        consignment
                            .ArrivingAt("GBBEL", "XI")
                            .ExportedFrom("AF")
                            .ImportedTo("XI")
                            .WithCommodity(commodity => commodity.CnCode("0102").OriginCountry("AF").Pieces(2))
                    ),
                token
            );
        var client = Client<CustomsCertexChedPortClient, CustomsCertexChedPort>(
            "CustomsCertexChedServiceV06",
            s_customs,
            (binding, address) => new CustomsCertexChedPortClient(binding, address)
        );

        var reserved = await Processed(client, chedId, "1", Mrn, [Pieces(1)]);
        var read = await Processed(client, chedId, "0", "", null);

        reserved.ProcessedChedInformationResponse1.ReservationResult.Should().BeTrue();
        var line = read.ProcessedChedInformationResponse1.QuantityManagementSummary.AvailableQuantity.Single();
        line.SwSupportingDocument.UnitOfMeasure.Should().Be(UniversalUnitOfMeasureType.H87);
        line.SwSupportingDocument.Quantity.Should().Be(1m);
    }

    [Fact]
    public async Task CustomsPort_RejectsTheDefaultAccount()
    {
        // The customs port authenticates as its own account. Nothing in the response surface reveals
        // which account was used, so this cross-account case is the only way to catch the misconfiguration.
        var client = Client<CustomsCertexChedPortClient, CustomsCertexChedPort>(
            "CustomsCertexChedServiceV06",
            s_default,
            (binding, address) => new CustomsCertexChedPortClient(binding, address)
        );

        var act = () =>
            client.processedChedRequestAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                "GBTEST01",
                new CertexHeaderType(),
                new ProcessedChedRequestType()
            );

        (await act.Should().ThrowAsync<FaultException>()).Which.Message.Should().Contain("UnauthenticatedException");
    }

    [Fact]
    public async Task ChedPort_RejectsAWrongAuthenticationKey()
    {
        var wrongKey = s_default with { AuthenticationKey = "not-the-key" };
        var client = Client<ChedCertificatePortClient, ChedCertificatePort>(
            "ChedCertificateServiceV2",
            wrongKey,
            (binding, address) => new ChedCertificatePortClient(binding, address)
        );

        var act = () =>
            client.getChedCertificateAsync(
                new SecurityHeaderType(),
                wrongKey.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetChedCertificateRequestType { ID = "CHEDA.GB.2026.0000001" }
            );

        (await act.Should().ThrowAsync<FaultException>()).Which.Message.Should().Contain("UnauthenticatedException");
    }

    private static CertexHeaderType Header =>
        new() { MessageId = "0123456789abcdef0123456789abcdef", UniqRequesterPrefix = "GBTEST01" };

    /// <summary>Shaped as the gateway's <c>CustomsChedService</c> shapes it, read or reserve alike.</summary>
    private static Task<ProcessedChedInformationResponse> Processed(
        CustomsCertexChedPortClient client,
        string chedId,
        string indication,
        string declaration,
        ConsignmentItemR6ForReservationType[]? items
    ) =>
        client.processedChedRequestAsync(
            new SecurityHeaderType(),
            s_customs.WebServiceClientId,
            ISO2AlphaLanguageCodeContentType.en,
            "GBTEST01",
            Header,
            new ProcessedChedRequestType
            {
                SendingDate = DateTime.UtcNow,
                ChedCertificateId = chedId,
                CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = "GBTEST01" },
                QuantityManagementIndication = indication,
                CustomsDeclarationReferenceNumber = declaration == ""
                    ? new()
                    : new() { Item = declaration, ItemElementName = ItemChoiceType1.MRN },
                CommodityDescriptionForChed = items,
            }
        );

    private static ConsignmentItemR6ForReservationType Pieces(decimal count) =>
        new()
        {
            GoodsItemNumber = "1",
            CertificateLineNumber = "1",
            ClassCode = "0102",
            NetVolumeQuantity = count,
            NetVolumeQuantitySpecified = true,
            NetVolumeUnitOfMeasure = UniversalUnitOfMeasureType.H87,
            NetVolumeUnitOfMeasureSpecified = true,
        };

    private static ConsignmentItemR6ForReservationType Item(decimal kilograms) =>
        new()
        {
            GoodsItemNumber = "1",
            CertificateLineNumber = "1",
            ClassCode = "0101",
            NetWeightQuantity = kilograms,
            NetWeightQuantitySpecified = true,
            NetWeightUnitOfMeasure = UniversalUnitOfMeasureType.KGM,
            NetWeightUnitOfMeasureSpecified = true,
        };

    private static async Task ShouldReportNotImplemented(Func<Task> act, string operation)
    {
        var fault = (await act.Should().ThrowAsync<FaultException>()).Which;

        fault.Message.Should().Contain(operation).And.Contain("not implemented");

        // Distinguishable from an authentication failure, which is a sender fault.
        fault.Message.Should().NotContain("UnauthenticatedException");
    }

    /// <summary>Mirrors the gateway's own client registration: same binding, same WS-Security behaviour.</summary>
    private static TClient Client<TClient, TChannel>(
        string servicePath,
        TracesNtCredentials credentials,
        Func<Binding, EndpointAddress, TClient> create
    )
        where TClient : ClientBase<TChannel>
        where TChannel : class
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferPoolSize = int.MaxValue,
        };

        var client = create(binding, new EndpointAddress($"{BaseUrl}/{servicePath}"));
        client.Endpoint.EndpointBehaviors.Add(new WsSecurityEndpointBehavior(credentials));

        return client;
    }
}
