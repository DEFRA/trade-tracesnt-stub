namespace Api.TradeTracesNTStub.Utils.Mongo;

/// <summary>
/// Creates the indexes search needs before the stub serves anything. It is also the start-up check: a
/// deploy that cannot reach Mongo fails here and shows as unhealthy, rather than starting and then
/// answering every simulator call with a fault.
/// </summary>
/// <remarks>The ledgers and serials are only ever read by <c>_id</c>, so they need no index of their own.</remarks>
public sealed class MongoStartup(IMongoDbClientFactory mongo) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await MongoCertificateStore.CreateIndexesAsync(mongo, MongoCertificateStore.Cheds, cancellationToken);
        await MongoCertificateStore.CreateIndexesAsync(mongo, MongoCertificateStore.Intras, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
