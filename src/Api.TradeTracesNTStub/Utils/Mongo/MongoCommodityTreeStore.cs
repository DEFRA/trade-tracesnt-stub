using Api.TradeTracesNTStub.Simulator.Control.Commodities;
using MongoDB.Driver;

namespace Api.TradeTracesNTStub.Utils.Mongo;

/// <summary>
/// One document per CHED type in <c>commodity_trees</c>, keyed by the type. The nodes are kept as
/// TRACES sent them, as BSON rather than a string, so the tree can be browsed in Mongo.
/// </summary>
public sealed class MongoCommodityTreeStore(IMongoDbClientFactory mongo) : ICommodityTreeStore
{
    public const string Collection = "commodity_trees";

    private readonly IMongoCollection<TreeDocument> _trees = mongo.GetCollection<TreeDocument>(Collection);

    public Task PutAsync(CommodityTree tree)
    {
        var summary = CommodityTrees.Summarise(tree);
        var document = new TreeDocument(
            tree.ChedType.ToUpperInvariant(),
            tree.TreeId,
            tree.ImportedAt.UtcDateTime,
            tree.Source,
            summary.Nodes,
            summary.Codes,
            tree.Nodes
        );

        return _trees.ReplaceOneAsync(Is(tree.ChedType), document, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<CommodityTree?> GetAsync(string chedType) =>
        await _trees.Find(Is(chedType)).FirstOrDefaultAsync() is { } document
            ? new CommodityTree(
                document.Id,
                document.TreeId,
                new DateTimeOffset(document.ImportedAt, TimeSpan.Zero),
                document.Source,
                document.Nodes
            )
            : null;

    public async Task<IReadOnlyList<CommodityTreeSummary>> ListAsync()
    {
        // The summary fields only: a tree's nodes run to a megabyte.
        var documents = await _trees
            .Find(FilterDefinition<TreeDocument>.Empty)
            .Project<SummaryDocument>("{ nodes: 0 }")
            .Sort(Builders<TreeDocument>.Sort.Ascending(d => d.Id))
            .ToListAsync();

        return
        [
            .. documents.Select(document => new CommodityTreeSummary(
                document.Id,
                document.TreeId,
                new DateTimeOffset(document.ImportedAt, TimeSpan.Zero),
                document.Source,
                document.NodeCount,
                document.CodeCount
            )),
        ];
    }

    public async Task<bool> RemoveAsync(string chedType) =>
        (await _trees.DeleteOneAsync(Is(chedType))).DeletedCount > 0;

    private static FilterDefinition<TreeDocument> Is(string chedType) =>
        Builders<TreeDocument>.Filter.Eq(d => d.Id, chedType.ToUpperInvariant());

    /// <param name="ImportedAt">UTC; Mongo reads a date back as UTC whatever offset it was written with.</param>
    private sealed record TreeDocument(
        string Id,
        string TreeId,
        DateTime ImportedAt,
        string Source,
        int NodeCount,
        int CodeCount,
        IReadOnlyList<CommodityNode> Nodes
    );

    private sealed record SummaryDocument(
        string Id,
        string TreeId,
        DateTime ImportedAt,
        string Source,
        int NodeCount,
        int CodeCount
    );
}
