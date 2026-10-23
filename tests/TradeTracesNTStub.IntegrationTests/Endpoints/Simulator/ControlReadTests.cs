using System.Net;
using System.Net.Http.Json;
using Api.TradeTracesNTStub.TestKit;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// What the control API reads back: the model a certificate was built from, so a UI can edit and PUT
/// it, and the code lists, so a UI offers only codes the simulator accepts. The certificate itself is
/// still read over SOAP, in <see cref="ChedRetrievalTests"/>.
/// </summary>
[Trait("Category", "IntegrationTest")]
[Collection(SimulatorStateCollection.Name)]
public class ControlReadTests : IntegrationTestBase
{
    private static readonly SimulatorControlClient s_simulator = SimulatorControlClient.At("http://localhost:8085");

    [Fact]
    public async Task AChedReadsBackAsTheModelItWasBuiltFrom()
    {
        var token = TestContext.Current.CancellationToken;
        var model = TestKitTests.AChedA().Build();

        var id = await s_simulator.CreateChed(model, token);
        var stored = await s_simulator.GetChed(id, token);

        stored.Id.Should().Be(id);
        stored.Accessible.Should().BeTrue();
        stored.Certificate.Should().BeEquivalentTo(model);
    }

    [Fact]
    public async Task APatchedChedReadsBackMerged()
    {
        // Read after a decision, the model carries both the declaration and the clearance, so a UI
        // editing it and sending a PUT keeps the decision.
        var token = TestContext.Current.CancellationToken;

        var id = await s_simulator.CreateChed(TestKitTests.AChedA(), token);
        await s_simulator.PatchChed(
            id,
            Ched.ChedA().WithStatus("VALIDATED").WithClearance(clearance => clearance.Acceptable()),
            token
        );

        var document = (await s_simulator.GetChed(id, token)).Certificate;

        document.Status.Should().Be("VALIDATED");
        document.ExchangedDocument.Declaration!.IncludedClause.Should().ContainKey("PURPOSE");
        document.ExchangedDocument.Clearance!.IncludedClause.Should().ContainKey("DECISION_CONCLUSION");
    }

    [Fact]
    public async Task ChedsListNewestFirstInaccessibleOnesIncluded()
    {
        var token = TestContext.Current.CancellationToken;

        var older = await s_simulator.CreateChed(TestKitTests.AChedA(), token);
        var newer = await s_simulator.CreateChed(TestKitTests.AChedA().NotAccessible(), token);

        var page = await s_simulator.ListCheds(pageSize: 100, cancellationToken: token);
        var ids = page.Items.Select(item => item.Id).ToList();

        ids.Should().Contain([newer, older]);
        ids.IndexOf(newer).Should().BeLessThan(ids.IndexOf(older));
        page.Items.Single(item => item.Id == newer).Accessible.Should().BeFalse();
    }

    [Fact]
    public async Task AListingPagesUntilNothingIsLeft()
    {
        var token = TestContext.Current.CancellationToken;
        await s_simulator.CreateChed(TestKitTests.AChedA(), token);
        await s_simulator.CreateChed(TestKitTests.AChedA(), token);

        var first = await s_simulator.ListCheds(1, 1, token);
        var second = await s_simulator.ListCheds(2, 1, token);

        first.HasMore.Should().BeTrue();
        second.Items.Should().ContainSingle().Which.Id.Should().NotBe(first.Items[0].Id);
    }

    [Fact]
    public async Task APageSizeOverTheLimitIsRejected()
    {
        var response = await Client.GetAsync("/control/cheds?pageSize=101", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AChedNeverCreatedIsNotFound()
    {
        var response = await Client.GetAsync(
            "/control/cheds/CHEDA.XI.1999.9999999",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ACodeListReadsBackWithNamesAndAliases()
    {
        var token = TestContext.Current.CancellationToken;

        var cnCodes = await s_simulator.GetLookup("cn_code", token);
        var statuses = await s_simulator.GetLookup("status_code", token);

        cnCodes.Should().Contain(entry => entry.Code == "0101" && entry.Name == "Live horses, asses, mules and hinnies");
        statuses.Should().Contain(entry => entry.Code == "1" && entry.Alias == "NEW");
    }

    [Fact]
    public async Task TheRegistriesAreListsToo()
    {
        var operators = await s_simulator.GetLookup("operators", TestContext.Current.CancellationToken);

        operators.Should().Contain(entry => entry.Code == "770198" && entry.Name == "Simulator Exporters Ltd");
    }

    [Fact]
    public async Task TheListOfListsNamesEverySeededFile()
    {
        var lists = await Client.GetFromJsonAsync<string[]>("/control/lookups", TestContext.Current.CancellationToken);

        lists.Should().Contain(["cn_code", "country", "operators", "authorities", "status_code"]);
        lists.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task AListNotSeededIsNotFound()
    {
        var response = await Client.GetAsync("/control/lookups/no_such_list", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
