using System.Collections.Concurrent;

namespace Api.TradeTracesNTStub.Simulator.Control.Commodities;

/// <summary>
/// The indexes the builder reads, one per CHED type, cached over the store: a tree is up to a
/// megabyte, too much to read for every certificate. An import through this instance replaces its
/// entry at once; entries also expire, so other instances pick up a refresh within minutes.
/// </summary>
public sealed class CommodityCatalogue(ICommodityTreeStore store, TimeProvider time)
{
    private static readonly TimeSpan s_lifetime = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (CommodityIndex? Index, DateTimeOffset LoadedAt)> _indexes =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The type's index, or null when no tree has been imported for it.</summary>
    public async Task<CommodityIndex?> GetAsync(string chedType)
    {
        if (_indexes.TryGetValue(chedType, out var cached) && time.GetUtcNow() - cached.LoadedAt < s_lifetime)
        {
            return cached.Index;
        }

        var index = await store.GetAsync(chedType) is { } tree ? new CommodityIndex(tree) : null;
        _indexes[chedType] = (index, time.GetUtcNow());
        return index;
    }

    /// <summary>Drops the type's tree, so its CN codes come from <c>cn_code.json</c> again.</summary>
    public async Task<bool> RemoveAsync(string chedType)
    {
        _indexes[chedType] = (null, time.GetUtcNow());
        return await store.RemoveAsync(chedType);
    }

    /// <summary>Stores the type's tree, stamped as imported now, and replaces its cached index.</summary>
    public async Task<CommodityTreeSummary> PutAsync(string chedType, string source, IReadOnlyList<CommodityNode> nodes)
    {
        var tree = new CommodityTree(
            chedType.ToUpperInvariant(),
            CommodityTrees.TreeIds[chedType],
            time.GetUtcNow(),
            source,
            nodes
        );
        await store.PutAsync(tree);
        _indexes[tree.ChedType] = (new CommodityIndex(tree), time.GetUtcNow());
        return CommodityTrees.Summarise(tree);
    }
}
