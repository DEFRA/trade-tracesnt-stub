using System.Text.Json;
using Api.TradeTracesNTStub.Simulator.Control.Scenarios;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace Api.TradeTracesNTStub.Utils.Mongo;

/// <summary>
/// One document per scenario in <c>scenarios</c>, keyed by its ID. The model is kept as BSON rather
/// than a string, so it can be read in Mongo.
/// </summary>
public sealed class MongoScenarioStore(IMongoDbClientFactory mongo) : IScenarioStore
{
    public const string Collection = "scenarios";

    private static readonly JsonWriterSettings s_relaxed = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    private readonly IMongoCollection<BsonDocument> _scenarios = mongo.GetCollection<BsonDocument>(Collection);

    public Task PutAsync(Scenario scenario) =>
        _scenarios.ReplaceOneAsync(
            Is(scenario.Id),
            new BsonDocument
            {
                ["_id"] = scenario.Id,
                ["name"] = scenario.Name,
                ["model"] = BsonDocument.Parse(scenario.Model.GetRawText()),
            },
            new ReplaceOptions { IsUpsert = true }
        );

    public async Task<Scenario?> GetAsync(string id) =>
        await _scenarios.Find(Is(id)).FirstOrDefaultAsync() is { } document ? Read(document) : null;

    public async Task<IReadOnlyList<Scenario>> ListAsync()
    {
        var documents = await _scenarios
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Ascending("name"))
            .ToListAsync();
        return [.. documents.Select(Read)];
    }

    public async Task<bool> RemoveAsync(string id) => (await _scenarios.DeleteOneAsync(Is(id))).DeletedCount > 0;

    private static Scenario Read(BsonDocument document)
    {
        using var model = JsonDocument.Parse(document["model"].AsBsonDocument.ToJson(s_relaxed));
        return new Scenario(document["_id"].AsString, document["name"].AsString, model.RootElement.Clone());
    }

    private static FilterDefinition<BsonDocument> Is(string id) => Builders<BsonDocument>.Filter.Eq("_id", id);
}
