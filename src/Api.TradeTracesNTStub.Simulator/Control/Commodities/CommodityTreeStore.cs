using System.Collections.Concurrent;

namespace Api.TradeTracesNTStub.Simulator.Control.Commodities;

/// <summary>One tree per CHED type. Putting a tree replaces that type's.</summary>
public interface ICommodityTreeStore
{
    Task PutAsync(CommodityTree tree);

    Task<CommodityTree?> GetAsync(string chedType);

    Task<IReadOnlyList<CommodityTreeSummary>> ListAsync();

    Task<bool> RemoveAsync(string chedType);
}

public sealed class InMemoryCommodityTreeStore : ICommodityTreeStore
{
    private readonly ConcurrentDictionary<string, CommodityTree> _trees = new(StringComparer.OrdinalIgnoreCase);

    public Task PutAsync(CommodityTree tree)
    {
        _trees[tree.ChedType] = tree;
        return Task.CompletedTask;
    }

    public Task<CommodityTree?> GetAsync(string chedType) =>
        Task.FromResult(_trees.TryGetValue(chedType, out var tree) ? tree : null);

    public Task<bool> RemoveAsync(string chedType) => Task.FromResult(_trees.TryRemove(chedType, out _));

    public Task<IReadOnlyList<CommodityTreeSummary>> ListAsync() =>
        Task.FromResult<IReadOnlyList<CommodityTreeSummary>>(
            [.. _trees.Values.OrderBy(tree => tree.ChedType).Select(CommodityTrees.Summarise)]
        );
}
