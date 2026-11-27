using System.Text.Json;
using Api.TradeTracesNTStub.Simulator.Control.Scenarios;

namespace TradeTracesNTStub.Test.Simulator.Scenarios;

/// <summary>What both stores must keep: a scenario per ID, replaced whole, its model read back as sent.</summary>
public abstract class ScenarioStoreTests(ISimulatorState state)
{
    public sealed class InMemory() : ScenarioStoreTests(new InMemoryState());

    public sealed class Mongo(MongoFixture mongo) : ScenarioStoreTests(mongo.NewState());

    private readonly IScenarioStore _store = state.Scenarios;

    private static Scenario Milk(string name = "CHED-P: milk") =>
        new(
            "ched-p-milk",
            name,
            JsonDocument
                .Parse(
                    """
                    {
                      "exchangedDocument": { "includedNote": { "CHED_TYPE": "P" } },
                      "specifiedConsignment": {
                        "availabilityDueDateTime": "2026-10-17T09:30:00Z",
                        "includedConsignmentItem": {
                          "includedTradeLineItem": [{ "netWeightMeasure": { "value": 1250.5, "unitCode": "KGM" } }]
                        }
                      }
                    }
                    """
                )
                .RootElement
        );

    [Fact]
    public async Task AModelReadsBackAsSent()
    {
        await _store.PutAsync(Milk());

        var scenario = await state.Another().Scenarios.GetAsync("ched-p-milk");

        scenario!.Name.Should().Be("CHED-P: milk");
        JsonElement.DeepEquals(scenario.Model, Milk().Model).Should().BeTrue(scenario.Model.GetRawText());
    }

    [Fact]
    public async Task PuttingAScenarioReplacesTheOneWithItsId()
    {
        await _store.PutAsync(Milk());
        await _store.PutAsync(Milk() with { Id = "ched-a-horses", Name = "CHED-A: horses" });
        await _store.PutAsync(Milk("CHED-P: skimmed milk"));

        var scenarios = await _store.ListAsync();

        scenarios.Select(s => (s.Id, s.Name)).Should().Equal(("ched-a-horses", "CHED-A: horses"), ("ched-p-milk", "CHED-P: skimmed milk"));
    }

    [Fact]
    public async Task ARemovedScenarioIsGone()
    {
        await _store.PutAsync(Milk());

        (await _store.RemoveAsync("ched-p-milk")).Should().BeTrue();
        (await _store.GetAsync("ched-p-milk")).Should().BeNull();
        (await _store.RemoveAsync("ched-p-milk")).Should().BeFalse();
    }
}
