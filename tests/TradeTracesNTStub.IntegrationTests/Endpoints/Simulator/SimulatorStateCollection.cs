namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// The simulator holds one CHED store for the whole process, and <c>/control/reset</c> empties all of
/// it. xUnit runs test classes in parallel, so every class that creates or clears a CHED joins this
/// collection — a collection's classes run one after another. That is what lets these tests reset and
/// then assert on exactly what they created, rather than hedging around a neighbour's data.
/// </summary>
[CollectionDefinition(Name)]
public class SimulatorStateCollection
{
    public const string Name = "Simulator state";
}
