using System.Collections.Concurrent;
using System.Text.Json;

namespace Api.TradeTracesNTStub.Simulator.Control.Scenarios;

/// <summary>
/// A named control model a UI offers as a starting point for a new CHED. The model is kept exactly as
/// sent, like <c>GET /control/cheds/{id}</c>'s, because it is what the UI will POST to create one.
/// </summary>
public sealed record Scenario(string Id, string Name, JsonElement Model);

/// <summary>What a scenario is saved from: its ID is the URL's.</summary>
public sealed record ScenarioRequest(string Name, JsonElement Model);

/// <summary>Scenarios by ID. Putting one replaces any with the same ID.</summary>
public interface IScenarioStore
{
    Task PutAsync(Scenario scenario);

    Task<Scenario?> GetAsync(string id);

    /// <summary>Every scenario, by name.</summary>
    Task<IReadOnlyList<Scenario>> ListAsync();

    Task<bool> RemoveAsync(string id);
}

public sealed class InMemoryScenarioStore : IScenarioStore
{
    private readonly ConcurrentDictionary<string, Scenario> _scenarios = new();

    public Task PutAsync(Scenario scenario)
    {
        _scenarios[scenario.Id] = scenario with { Model = scenario.Model.Clone() };
        return Task.CompletedTask;
    }

    public Task<Scenario?> GetAsync(string id) =>
        Task.FromResult(_scenarios.TryGetValue(id, out var scenario) ? scenario : null);

    public Task<IReadOnlyList<Scenario>> ListAsync() =>
        Task.FromResult<IReadOnlyList<Scenario>>(
            [.. _scenarios.Values.OrderBy(scenario => scenario.Name, StringComparer.Ordinal)]
        );

    public Task<bool> RemoveAsync(string id) => Task.FromResult(_scenarios.TryRemove(id, out _));
}
