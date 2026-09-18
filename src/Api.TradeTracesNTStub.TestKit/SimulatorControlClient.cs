using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace Api.TradeTracesNTStub.TestKit;

/// <summary>
/// Drives the simulator's control API from a test. Thin on purpose: it saves repeating the URL, the
/// JSON options and the status check, and throws with the simulator's own message.
/// </summary>
public class SimulatorControlClient(HttpClient client)
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Points at a simulator by base address, e.g. <c>http://localhost:8085</c>.</summary>
    public static SimulatorControlClient At(string baseUrl) =>
        new(new HttpClient { BaseAddress = new Uri(baseUrl) });

    /// <summary>Creates a CHED and returns the ID it was stored under.</summary>
    public async Task<string> CreateChed(ChedBuilder ched, CancellationToken cancellationToken = default) =>
        await CreateChed(ched.Build(), cancellationToken);

    public async Task<string> CreateChed(ChedControlModel ched, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("/control/cheds", ched, s_json, cancellationToken);
        await Ensure(response, "create a CHED", cancellationToken);

        var created = await response.Content.ReadFromJsonAsync<StoredChed>(s_json, cancellationToken);

        return created!.Id;
    }

    public async Task UpdateChed(string id, ChedBuilder ched, CancellationToken cancellationToken = default)
    {
        var response = await client.PutAsJsonAsync($"/control/cheds/{id}", ched.Build(), s_json, cancellationToken);
        await Ensure(response, $"update CHED '{id}'", cancellationToken);
    }

    /// <summary>
    /// Merges a partial CHED into a stored one. This is how a decision is applied: build a CHED
    /// carrying only the clearance block and the new status, and send that.
    /// </summary>
    public async Task PatchChed(string id, ChedBuilder patch, CancellationToken cancellationToken = default)
    {
        var response = await client.PatchAsJsonAsync($"/control/cheds/{id}", patch.Build(), s_json, cancellationToken);
        await Ensure(response, $"patch CHED '{id}'", cancellationToken);
    }

    public async Task DeleteChed(string id, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync($"/control/cheds/{id}", cancellationToken);
        await Ensure(response, $"delete CHED '{id}'", cancellationToken);
    }

    /// <summary>Empties the simulator. Call it before a test, not after.</summary>
    public async Task Reset(CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsync("/control/reset", null, cancellationToken);

        await Ensure(response, "reset the simulator", cancellationToken);
    }

    private static async Task Ensure(HttpResponseMessage response, string what, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Carry the simulator's message through — it names the code or file that needs fixing.
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException($"The simulator refused to {what} ({(int)response.StatusCode}): {body}");
    }

    private record StoredChed(string Id);
}
