using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Extensions;
using Api.TradeTracesNTStub.Utils.Mongo;

namespace Api.TradeTracesNTStub.Config;

/// <summary>What holds the simulator's certificates and customs ledger, set by <c>Simulator:Store</c>.</summary>
public enum SimulatorStore
{
    /// <summary>A restart is a reset, and each instance holds its own state: fine for one local instance.</summary>
    InMemory,

    /// <summary>Survives restarts and is shared by every instance, which CDP needs when it runs more than one.</summary>
    MongoDB,
}

public static class SimulatorStoreExtensions
{
    public static IServiceCollection AddSimulatorState(this IServiceCollection services, IConfiguration configuration)
    {
        var configured = configuration["Simulator:Store"];
        SimulatorStore? store = Enum.TryParse<SimulatorStore>(configured, ignoreCase: true, out var parsed)
            ? parsed
            : null;

        return store switch
        {
            SimulatorStore.InMemory => services.AddInMemorySimulatorState(),
            SimulatorStore.MongoDB => services
                .AddHostedService<MongoStartup>()
                .AddSingleton<IChedStore>(provider =>
                    new MongoCertificateStore(Mongo(provider), MongoCertificateStore.Cheds)
                )
                .AddSingleton<IIntraStore>(provider =>
                    new MongoCertificateStore(Mongo(provider), MongoCertificateStore.Intras)
                )
                .AddSingleton<ICustomsLedger>(provider => new MongoCustomsLedger(Mongo(provider))),

            // Missing, unknown, or a number naming no mode. There is no default: appsettings carries one,
            // so a missing value is broken config, not a choice.
            _ => throw new InvalidOperationException(
                $"Simulator:Store is '{configured}'. Set it to {string.Join(" or ", Enum.GetNames<SimulatorStore>())}."
            ),
        };
    }

    private static IMongoDbClientFactory Mongo(IServiceProvider provider) =>
        provider.GetRequiredService<IMongoDbClientFactory>();
}
