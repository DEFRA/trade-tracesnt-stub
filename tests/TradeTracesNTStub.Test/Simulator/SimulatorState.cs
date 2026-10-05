using Api.TradeTracesNTStub.Config;
using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Utils.Mongo;
using EphemeralMongo;
using Microsoft.Extensions.Options;

[assembly: AssemblyFixture(typeof(TradeTracesNTStub.Test.Simulator.MongoFixture))]

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// The simulator's state behind one of its stores. A test class that takes one runs against both,
/// through a nested subclass per store, so neither store needs its own copy of the tests.
/// </summary>
public interface ISimulatorState
{
    IChedStore Cheds { get; }

    ICustomsLedger Ledger { get; }

    /// <summary>
    /// The same state reached another way: in memory, by another request to the same instance; in
    /// Mongo, by another instance of the stub, as on CDP or after a restart.
    /// </summary>
    ISimulatorState Another();
}

public sealed class InMemoryState : ISimulatorState
{
    public IChedStore Cheds { get; } = new InMemoryCertificateStore();

    public ICustomsLedger Ledger { get; } = new InMemoryCustomsLedger();

    public ISimulatorState Another() => this;
}

public sealed class MongoState : ISimulatorState
{
    private readonly string _connectionString;
    private readonly string _database;

    public MongoState(string connectionString, string database)
    {
        _connectionString = connectionString;
        _database = database;

        // The stub's own factory, so documents are mapped under the same conventions it registers.
        var mongo = new MongoDbClientFactory(
            Options.Create(new MongoConfig { DatabaseUri = connectionString, DatabaseName = database })
        );

        Cheds = new MongoCertificateStore(mongo, "cheds");
        Ledger = new MongoCustomsLedger(mongo);
    }

    public IChedStore Cheds { get; }

    public ICustomsLedger Ledger { get; }

    public ISimulatorState Another() => new MongoState(_connectionString, _database);
}

/// <summary>One mongod for the whole run, and a database per test so tests in parallel never meet.</summary>
public sealed class MongoFixture : IAsyncLifetime
{
    private IMongoRunner? _runner;

    public async ValueTask InitializeAsync() => _runner = await MongoRunner.RunAsync(new MongoRunnerOptions());

    public ValueTask DisposeAsync()
    {
        _runner?.Dispose();
        return ValueTask.CompletedTask;
    }

    public ISimulatorState NewState() => new MongoState(_runner!.ConnectionString, NewDatabaseName());

    /// <summary>A client on a database no other test uses, for tests of the Mongo plumbing itself.</summary>
    public MongoDbClientFactory NewClientFactory() =>
        new(Options.Create(new MongoConfig { DatabaseUri = _runner!.ConnectionString, DatabaseName = NewDatabaseName() }));

    private static string NewDatabaseName() => $"test-{Guid.NewGuid():N}";
}
