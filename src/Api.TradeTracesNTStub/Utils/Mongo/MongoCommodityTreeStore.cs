using System.Text.Json;
using Api.TradeTracesNTStub.Simulator.Control.Commodities;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace Api.TradeTracesNTStub.Utils.Mongo;

/// <summary>
/// One document per CHED type in <c>commodity_trees</c>, keyed by the type. The nodes are kept as
/// TRACES sent them, as BSON rather than a string, so the tree can be browsed in Mongo.
/// </summary>
public sealed class MongoCommodityTreeStore(IMongoDbClientFactory mongo) : ICommodityTreeStore
{
    public const string Collection = "commodity_trees";

    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);
    private static readonly JsonWriterSettings s_relaxed = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    private readonly IMongoCollection<BsonDocument> _trees = mongo.GetCollection<BsonDocument>(Collection);

    public Task PutAsync(CommodityTree tree)
    {
        var summary = CommodityTrees.Summarise(tree);
        var document = new BsonDocument
        {
            ["_id"] = tree.ChedType.ToUpperInvariant(),
            ["treeId"] = tree.TreeId,
            ["importedAt"] = tree.ImportedAt.UtcDateTime,
            ["source"] = tree.Source,
            ["nodeCount"] = summary.Nodes,
            ["codeCount"] = summary.Codes,
            ["nodes"] = BsonDocument.Parse($"{{\"n\":{JsonSerializer.Serialize(tree.Nodes, s_json)}}}")["n"],
        };

        return _trees.ReplaceOneAsync(Is(tree.ChedType), document, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<CommodityTree?> GetAsync(string chedType)
    {
        var document = await _trees.Find(Is(chedType)).FirstOrDefaultAsync();
        if (document is null)
        {
            return null;
        }

        var nodes =
            JsonSerializer.Deserialize<IReadOnlyList<CommodityNode>>(document["nodes"].ToJson(s_relaxed), s_json) ?? [];
        return new CommodityTree(
            document["_id"].AsString,
            document["treeId"].AsString,
            new DateTimeOffset(document["importedAt"].ToUniversalTime(), TimeSpan.Zero),
            document["source"].AsString,
            nodes
        );
    }

    public async Task<IReadOnlyList<CommodityTreeSummary>> ListAsync()
    {
        // The summary fields only: a tree's nodes run to a megabyte.
        var documents = await _trees
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Project(Builders<BsonDocument>.Projection.Exclude("nodes"))
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .ToListAsync();

        return
        [
            .. documents.Select(document => new CommodityTreeSummary(
                document["_id"].AsString,
                document["treeId"].AsString,
                new DateTimeOffset(document["importedAt"].ToUniversalTime(), TimeSpan.Zero),
                document["source"].AsString,
                document["nodeCount"].AsInt32,
                document["codeCount"].AsInt32
            )),
        ];
    }

    public async Task<bool> RemoveAsync(string chedType) =>
        (await _trees.DeleteOneAsync(Is(chedType))).DeletedCount > 0;

    private static FilterDefinition<BsonDocument> Is(string chedType) =>
        Builders<BsonDocument>.Filter.Eq("_id", chedType.ToUpperInvariant());
}
