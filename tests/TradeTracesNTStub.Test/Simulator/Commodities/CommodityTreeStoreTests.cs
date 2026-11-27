using Api.TradeTracesNTStub.Simulator.Control.Commodities;

namespace TradeTracesNTStub.Test.Simulator.Commodities;

/// <summary>What both stores must keep: a tree per CHED type, replaced whole, and read back as stored.</summary>
public abstract class CommodityTreeStoreTests(ISimulatorState state)
{
    public sealed class InMemory() : CommodityTreeStoreTests(new InMemoryState());

    public sealed class Mongo(MongoFixture mongo) : CommodityTreeStoreTests(mongo.NewState());

    private readonly ICommodityTreeStore _store = state.CommodityTrees;

    [Fact]
    public async Task ATreeReadsBackAsStored()
    {
        await _store.PutAsync(CommodityTreeSample.Tree());

        var tree = await state.Another().CommodityTrees.GetAsync("a");

        tree.Should().BeEquivalentTo(CommodityTreeSample.Tree());
    }

    [Fact]
    public async Task PuttingATreeReplacesThatTypesOnly()
    {
        await _store.PutAsync(CommodityTreeSample.Tree("A"));
        await _store.PutAsync(CommodityTreeSample.Tree("P") with { TreeId = "chedp" });
        await _store.PutAsync(CommodityTreeSample.Tree("A") with { Nodes = [CommodityTreeSample.Nodes[0].Node![0]] });

        var summaries = await _store.ListAsync();

        summaries.Select(s => (s.ChedType, s.Nodes)).Should().Equal(("A", 1), ("P", 7));
    }

    [Fact]
    public async Task ARemovedTreeIsGone()
    {
        await _store.PutAsync(CommodityTreeSample.Tree());

        (await _store.RemoveAsync("A")).Should().BeTrue();
        (await _store.GetAsync("A")).Should().BeNull();
        (await _store.RemoveAsync("A")).Should().BeFalse();
    }
}
