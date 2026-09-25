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

    /// <summary>The gateway's own client, bound exactly as the gateway binds it.</summary>
    private static async Task<SPSCertificateType?> GetCertificate(string id)
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

        var response = await client.getEuIntraCertificateAsync(
            new SecurityHeaderType(),
            s_credentials.WebServiceClientId,
            ISO2AlphaLanguageCodeContentType.en,
            [],
            new GetEuIntraCertificateRequestType { ID = id }
        );

        return response.GetEuIntraCertificateResponse1?.SPSCertificate;
    }
}
