using System.ServiceModel;
using Api.TradeTracesNTStub.TestKit;
using TracesNT.WebServices;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// Exercises the TestKit as a consuming repository would — through the builders and the control
/// client, rather than reaching past them into the HTTP API.
/// </summary>
[Trait("Category", "IntegrationTest")]
[Collection(SimulatorStateCollection.Name)]
public class TestKitTests
{
    private static readonly SimulatorControlClient s_simulator = SimulatorControlClient.At("http://localhost:8085");

    private static ChedBuilder AChedA() =>
        Ched.ChedA()
            .WithStatus("NEW")
            .WithDeclaration(declaration => declaration.Declaring("FREE_CIRCULATION", "FATTENING"))
            .WithConsignment(consignment =>
                consignment
                    .ArrivingAt("GBBEL", "XI")
                    .ExportedFrom("AF")
                    .ImportedTo("XI")
                    .WithConsignor(party => party.Operator("770198").InCountry("XI"))
                    .WithConsignee(party => party.Operator("899361").InCountry("XI"))
                    .CarryingCargoType("12")
                    .WithCommodity(commodity =>
                        commodity.CnCode("0101").OriginCountry("AF").Packages(2, "BX").NetWeightKg(900)
                    )
            );

    [Fact]
    public async Task ABuiltChedIsServedOverSoap()
    {
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateChed(AChedA(), token);

        var certificate = await ChedRetrievalTests.GetCertificate(id);

        certificate.Should().NotBeNull();
        certificate!.SPSExchangedDocument.ID.Value.Should().Be(id);

        // The name came from the operator registry, not from the request.
        certificate.SPSConsignment.ConsignorSPSParty.Name.Value.Should().Be("Simulator Exporters Ltd");
    }

    [Fact]
    public async Task AnUnregisteredOperatorNeedsNoRegistryEntry()
    {
        // TRACES calls this an operator created on the fly, and it is the way past the registry when
        // a test needs an operator nobody has seeded.
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateChed(
            AChedA()
                .WithConsignment(consignment =>
                    consignment.WithCustomsTransitAgent(party => party.Named("Nobody Seeded Ltd").InCountry("XI"))
                ),
            token
        );

        var certificate = await ChedRetrievalTests.GetCertificate(id);

        certificate!.SPSConsignment.CustomsTransitAgentSPSParty.Name.Value.Should().Be("Nobody Seeded Ltd");
    }

    [Fact]
    public async Task ADecisionIsAppliedWithoutRestatingTheCertificate()
    {
        // The point of the keyed objects: a patch carrying only the clearance block decides the CHED.
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateChed(AChedA(), token);

        await s_simulator.PatchChed(
            id,
            Ched.ChedA().WithStatus("VALIDATED").WithClearance(clearance => clearance.Acceptable()),
            token
        );

        var document = (await ChedRetrievalTests.GetCertificate(id))!.SPSExchangedDocument;

        document.StatusCode.name.Should().Be("Issued (Validated)");

        // The applicant's declaration survived; the clearance was added beside it.
        document
            .SignatorySPSAuthentication.SelectMany(authentication => authentication.IncludedSPSClause)
            .Select(clause => clause.ID.Value)
            .Should()
            .Contain("PURPOSE")
            .And.Contain("DECISION_CONCLUSION");
    }

    [Fact]
    public async Task ANotAccessibleChedIsRefused()
    {
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateChed(AChedA().NotAccessible(), token);

        var act = () => ChedRetrievalTests.GetCertificate(id);

        await act.Should().ThrowAsync<FaultException<ChedCertificatePermissionDeniedExceptionType>>();
    }

    [Fact]
    public async Task AnUnknownCodeFailsWithTheSimulatorsOwnMessage()
    {
        var token = TestContext.Current.CancellationToken;

        var act = () =>
            s_simulator.CreateChed(
                AChedA()
                    .WithConsignment(consignment =>
                        consignment.WithCommodity(commodity => commodity.CnCode("9999999999"))
                    ),
                token
            );

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should()
            .Contain("cn_code")
            .And.Contain("SeedData");
    }
}
