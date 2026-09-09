using System.ServiceModel;
using Api.TradeTracesNTStub.TestKit;
using TracesNT.WebServices;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// Exercises the TestKit the way a journey test will: build a CHED with the builders, push it through
/// the control client, read it back over SOAP.
/// </summary>
/// <remarks>
/// The TestKit is shipped as a package for other repositories to consume, so it needs a test that
/// uses it as a consumer would rather than reaching past it into the HTTP API.
/// </remarks>
[Trait("Category", "IntegrationTest")]
public class TestKitTests
{
    private static readonly SimulatorControlClient s_simulator = SimulatorControlClient.At("http://localhost:8085");

    [Fact]
    public async Task ABuiltChedIsServedOverSoap()
    {
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateChed(
            Ched.ChedA()
                .WithStatus("VALIDATED")
                .ArrivingAt("GBBEL")
                .WithConsignor("770198", "Test Consignor Ltd")
                .WithCommodity(commodity =>
                    commodity.CnCode("0101").OriginCountry("AF").Packages(2, "BX").NetWeightKg(900)
                )
                .WithDecision(decision => decision.Acceptable()),
            token
        );

        var certificate = await ChedRetrievalTests.GetCertificate(id);

        certificate.Should().NotBeNull();
        certificate!.SPSExchangedDocument.ID.Value.Should().Be(id);
        certificate.SPSConsignment.ConsignorSPSParty.Name.Value.Should().Be("Test Consignor Ltd");

        // The decision is written as the official inspector's authentication block.
        certificate
            .SPSExchangedDocument.SignatorySPSAuthentication.Should()
            .Contain(authentication =>
                authentication.IncludedSPSClause.Any(clause =>
                    clause.ID.Value == "DECISION_CONCLUSION"
                    && clause.Content[0].Value == "ACCEPTABLE_FOR_FREE_CIRCULATION"
                )
            );
    }

    [Fact]
    public async Task ANotAccessibleChedIsRefused()
    {
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateChed(Ched.ChedA().NotAccessible(), token);

        var act = () => ChedRetrievalTests.GetCertificate(id);

        await act.Should().ThrowAsync<FaultException<ChedCertificatePermissionDeniedExceptionType>>();
    }

    [Fact]
    public async Task AnUnknownCodeFailsWithTheSimulatorsOwnMessage()
    {
        var token = TestContext.Current.CancellationToken;

        var act = () =>
            s_simulator.CreateChed(Ched.ChedA().WithCommodity(commodity => commodity.CnCode("9999999999")), token);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should()
            .Contain("cn_code")
            .And.Contain("SeedData");
    }
}
