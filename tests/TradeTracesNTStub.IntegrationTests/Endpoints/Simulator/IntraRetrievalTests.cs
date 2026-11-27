using System.ServiceModel;
using Api.TradeTracesNTStub.TestKit;
using TracesNT;
using TracesNT.ClientBehaviours;
using TracesNT.WebServices;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// An INTRA created through the TestKit, read back through the gateway's own SOAP client. The builder
/// rules are shared with CHED and covered there; this proves the INTRA port serves what was stored.
/// </summary>
[Trait("Category", "IntegrationTest")]
[Collection(SimulatorStateCollection.Name)]
public class IntraRetrievalTests
{
    private const string BaseUrl = "http://localhost:8085";

    private static readonly SimulatorControlClient s_simulator = SimulatorControlClient.At(BaseUrl);

    private static readonly TracesNtCredentials s_credentials = new()
    {
        Username = "simulator-user",
        AuthenticationKey = "simulator-auth-key",
        WebServiceClientId = "simulator-client-id",
    };

    private static CertificateBuilder AnIntra() =>
        Intra
            .OfModel("64/432 (2016/2008) F1 Bovine")
            .WithStatus("VALIDATED")
            .WithConsignment(consignment =>
                consignment
                    .ExportedFrom("AF")
                    .ImportedTo("XI")
                    .WithConsignor(party => party.Operator("770198").InCountry("XI"))
                    .WithDespatchParty(party => party.Operator("770198").InCountry("XI"))
                    .WithCommodity(commodity => commodity.CnCode("0101").Packages(2, "BX"))
            );

    [Fact]
    public async Task AnIntraCreatedThroughTheControlApiIsServedOverSoap()
    {
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateIntra(AnIntra(), token);

        var certificate = await GetCertificate(id);

        certificate.Should().NotBeNull();
        certificate!.SPSExchangedDocument.ID.Value.Should().Be(id);
        certificate.SPSExchangedDocument.Name[0].Value.Should().Be("64/432 (2016/2008) F1 Bovine");
        certificate.SPSExchangedDocument.StatusCode.name.Should().Be("Issued (Validated)");
        certificate.SPSConsignment.DespatchSPSParty.Name.Value.Should().Be("Simulator Exporters Ltd");
    }

    [Fact]
    public async Task AnIntraMarkedInaccessibleIsATypedPermissionDeniedFault()
    {
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateIntra(AnIntra().NotAccessible(), token);

        var act = () => GetCertificate(id);

        await act.Should().ThrowAsync<FaultException<EuIntraCertificatePermissionDeniedExceptionType>>();
    }

    [Fact]
    public async Task ResetRemovesIntras()
    {
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateIntra(AnIntra(), token);

        await s_simulator.Reset(token);

        var act = () => GetCertificate(id);

        await act.Should().ThrowAsync<FaultException<EuIntraCertificateNotFoundExceptionType>>();
    }

    [Fact]
    public async Task FindReturnsWhatWasStoredAndSkipsWhatIsNotAccessible()
    {
        var token = TestContext.Current.CancellationToken;

        await s_simulator.Reset(token);

        var visible = await s_simulator.CreateIntra(AnIntra(), token);
        await s_simulator.CreateIntra(AnIntra().NotAccessible(), token);

        var results = await Find(pageSize: 100);

        results.Select(result => result.ID).Should().Equal(visible);
        results.Single().Status.name.Should().Be("Issued (Validated)");
    }

    [Fact]
    public async Task FindPagesWithOneBasedOffsets()
    {
        var token = TestContext.Current.CancellationToken;

        await s_simulator.Reset(token);

        var older = await s_simulator.CreateIntra(AnIntra(), token);
        var newer = await s_simulator.CreateIntra(AnIntra(), token);

        (await Find(pageSize: 1)).Select(result => result.ID).Should().Equal(newer);
        (await Find(pageSize: 1, offset: 2)).Select(result => result.ID).Should().Equal(older);
    }

    private static async Task<IReadOnlyList<EuIntraCertificateQueryResultType>> Find(int pageSize = 10, int offset = 1)
    {
        var response = await Client()
            .findEuIntraCertificateAsync(
                new SecurityHeaderType(),
                s_credentials.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new FindEuIntraCertificateRequestType
                {
                    UpdateDateTimeRange = new DateTimeRange(),
                    pageSize = pageSize,
                    offset = offset,
                }
            );

        return response.FindEuIntraCertificateResponse1?.EuIntraCertificateResult ?? [];
    }

    private static async Task<SPSCertificateType?> GetCertificate(string id)
    {
        var response = await Client().getEuIntraCertificateAsync(
            new SecurityHeaderType(),
            s_credentials.WebServiceClientId,
            ISO2AlphaLanguageCodeContentType.en,
            [],
            new GetEuIntraCertificateRequestType { ID = id }
        );

        return response.GetEuIntraCertificateResponse1?.SPSCertificate;
    }

    /// <summary>The gateway's own client, bound exactly as the gateway binds it.</summary>
    private static EuIntraCertificatePortClient Client()
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferPoolSize = int.MaxValue,
        };

        var client = new EuIntraCertificatePortClient(
            binding,
            new EndpointAddress($"{BaseUrl}/EuIntraCertificateServiceV1")
        );
        client.Endpoint.EndpointBehaviors.Add(new WsSecurityEndpointBehavior(s_credentials));

        return client;
    }
}
