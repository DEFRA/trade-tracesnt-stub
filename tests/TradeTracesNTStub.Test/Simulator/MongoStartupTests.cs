using Api.TradeTracesNTStub.Utils.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// Search runs in Mongo on shared environments holding tens of thousands of CHEDs. Without its index
/// every search is a collection scan, which nothing else would notice until it was slow.
/// </summary>
public class MongoStartupTests(MongoFixture mongo)
{
    [Fact]
    public async Task StartingCreatesTheSearchIndexOnEachCertificateCollection()
    {
        var factory = mongo.NewClientFactory();
        var token = TestContext.Current.CancellationToken;

        // Twice, because every start of every instance runs it against the same database.
        await new MongoStartup(factory).StartAsync(token);
        await new MongoStartup(factory).StartAsync(token);

        foreach (var collection in new[] { MongoCertificateStore.Cheds, MongoCertificateStore.Intras })
        {
            var indexes = await (
                await factory.GetCollection<BsonDocument>(collection).Indexes.ListAsync(token)
            ).ToListAsync(token);

            indexes
                .Should()
                .ContainSingle(index => index["name"] == "search", collection)
                .Which["key"]
                .Should()
                .Be(new BsonDocument { ["accessible"] = 1, ["updatedTicks"] = -1, ["certificateId"] = -1 });
        }
    }
}
