using System.Net;
using System.Net.Http.Json;
using Api.TradeTracesNTStub.Simulator.Control.Commodities;
using Api.TradeTracesNTStub.TestKit;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// A CHED type's commodity tree decides which CN codes its CHEDs may carry and how they're described.
/// The stub starts with no trees; each test here uploads a small CHED-A tree and deletes it afterwards,
/// so the rest of the suite sees the stub as it starts.
/// </summary>
[Trait("Category", "IntegrationTest")]
[Collection(SimulatorStateCollection.Name)]
public class CommodityTreeTests : IntegrationTestBase, IAsyncLifetime
{
    private static readonly SimulatorControlClient s_simulator = SimulatorControlClient.At("http://localhost:8085");

    // LIVE ANIMALS > Live sheep and goats > Sheep, a code cn_code.json doesn't have, and 0101 beside it.
    private static readonly IReadOnlyList<CommodityNode> s_tree =
    [
        new()
        {
            Item = new CommodityCode("01"),
            Description = new CommodityText("LIVE ANIMALS", "en"),
            Path = "R/N-10000",
            Node =
            [
                new()
                {
                    Item = new CommodityCode("0101"),
                    Description = new CommodityText("Live horses, asses, mules and hinnies", "en"),
                    AllowedForSelection = true,
                },
                new()
                {
                    Item = new CommodityCode("0104"),
                    Description = new CommodityText("Live sheep and goats", "en"),
                    Node =
                    [
                        new()
                        {
                            Item = new CommodityCode("010410"),
                            Description = new CommodityText("Sheep", "en"),
                            AllowedForSelection = true,
                        },
                    ],
                },
            ],
        },
    ];

    public async ValueTask InitializeAsync() =>
        await s_simulator.PutCommodityTree("A", s_tree, TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() =>
        await s_simulator.DeleteCommodityTree("A", TestContext.Current.CancellationToken);

    private static CertificateBuilder AChedACarrying(string cnCode) =>
        TestKitTests
            .AChedA()
            .WithConsignment(consignment =>
                consignment.WithCommodity(commodity => commodity.CnCode(cnCode).OriginCountry("AF").Pieces(3))
            );

    [Fact]
    public async Task AChedTakesItsCommodityDescriptionFromTheTree()
    {
        var id = await s_simulator.CreateChed(AChedACarrying("010410"), TestContext.Current.CancellationToken);

        var certificate = await ChedRetrievalTests.GetCertificate(id);

        certificate!
            .SPSConsignment.IncludedSPSConsignmentItem[0]
            .IncludedSPSTradeLineItem.Last()
            .ApplicableSPSClassification.Single(classification => classification.SystemID.Value == "CN")
            .ClassName.Select(name => name.Value)
            .Should()
            .Equal("LIVE ANIMALS", "Live sheep and goats", "Sheep");
    }

    [Fact]
    public async Task ACodeOutsideTheTreeIsRefusedNamingIt()
    {
        // 0103 is in cn_code.json, but the tree, once loaded, decides.
        var act = () => s_simulator.CreateChed(AChedACarrying("0103"), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should()
            .Contain("CHED-A commodity tree (cheda");
    }

    [Fact]
    public async Task WithoutATreeTheSeededListDecidesAgain()
    {
        var token = TestContext.Current.CancellationToken;
        await s_simulator.DeleteCommodityTree("A", token);

        var act = () => s_simulator.CreateChed(AChedACarrying("0103"), token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TheTreeAndWhatIsLoadedReadBack()
    {
        var token = TestContext.Current.CancellationToken;

        var summaries = await Client.GetFromJsonAsync<CommodityTreeSummary[]>("/control/commodity-trees", token);
        var tree = await Client.GetFromJsonAsync<CommodityTree>("/control/commodity-trees/A", token);

        summaries.Should().ContainSingle(s => s.ChedType == "A" && s.TreeId == "cheda" && s.Codes == 4 && s.Source == "upload");
        tree!.Nodes.Should().BeEquivalentTo(s_tree);
    }

    [Fact]
    public async Task ARefreshWithNoSourceConfiguredSaysSo()
    {
        var response = await Client.PostAsync("/control/commodity-trees/refresh", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should()
            .Contain("Simulator:CommodityTrees:SourceUrl");
    }

    [Fact]
    public async Task AnUnknownChedTypeHasNoTree()
    {
        var response = await Client.PutAsJsonAsync(
            "/control/commodity-trees/Z",
            s_tree,
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
