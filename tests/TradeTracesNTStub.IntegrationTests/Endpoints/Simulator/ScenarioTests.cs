using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// The scenarios a UI starts a CHED from. Tests here save their own under IDs of their own and delete
/// them afterwards.
/// </summary>
[Trait("Category", "IntegrationTest")]
public class ScenarioTests : IntegrationTestBase, IAsyncLifetime
{
    private const string Id = "integration-test-scenario";

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() =>
        await Client.DeleteAsync($"/control/scenarios/{Id}", TestContext.Current.CancellationToken);

    [Fact]
    public async Task ASavedScenarioReadsBackWithItsModel()
    {
        var token = TestContext.Current.CancellationToken;
        var model = JsonDocument.Parse("""{ "exchangedDocument": { "includedNote": { "CHED_TYPE": "A" } } }""").RootElement;

        var saved = await Client.PutAsJsonAsync($"/control/scenarios/{Id}", new { name = "A test", model }, token);
        var scenario = await Client.GetFromJsonAsync<JsonElement>($"/control/scenarios/{Id}", token);

        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        scenario.GetProperty("name").GetString().Should().Be("A test");
        JsonElement.DeepEquals(scenario.GetProperty("model"), model).Should().BeTrue();
    }

    [Theory]
    [InlineData("Not An Id", """{ "name": "A test", "model": {} }""")]
    [InlineData(Id, """{ "name": " ", "model": {} }""")]
    [InlineData(Id, """{ "name": "A test", "model": [] }""")]
    public async Task AScenarioWithoutAnIdNameOrModelIsRefused(string id, string body)
    {
        var response = await Client.PutAsync(
            $"/control/scenarios/{Uri.EscapeDataString(id)}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
