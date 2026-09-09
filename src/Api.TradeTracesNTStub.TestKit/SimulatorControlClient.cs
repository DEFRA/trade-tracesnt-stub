using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace Api.TradeTracesNTStub.TestKit;

/// <summary>
/// Drives the simulator's control API from a test.
/// </summary>
/// <remarks>
/// Thin on purpose. It exists so a test does not repeat the URL, the JSON options and the
/// status-code check, not to add behaviour of its own. Failures throw with the simulator's own
/// message, which is written to say what to fix.
/// </remarks>
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

    public async Task DeleteChed(string id, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync($"/control/cheds/{id}", cancellationToken);
        await Ensure(response, $"delete CHED '{id}'", cancellationToken);
    }

    /// <summary>Empties the simulator, or loads a named fixture set. Call it before a test, not after.</summary>
    public async Task Reset(string? fixtureSet = null, CancellationToken cancellationToken = default)
    {
        var url = fixtureSet is null ? "/control/reset" : $"/control/reset?fixtureSet={Uri.EscapeDataString(fixtureSet)}";
        var response = await client.PostAsync(url, null, cancellationToken);

        await Ensure(response, fixtureSet is null ? "reset the simulator" : $"load fixture set '{fixtureSet}'", cancellationToken);
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
