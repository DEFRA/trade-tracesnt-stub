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
    private const string Cheds = "/control/cheds";
    private const string Intras = "/control/intras";

    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Points at a simulator by base address, e.g. <c>http://localhost:8085</c>.</summary>
    public static SimulatorControlClient At(string baseUrl) =>
        new(new HttpClient { BaseAddress = new Uri(baseUrl) });

    /// <summary>Creates a CHED and returns the ID it was stored under.</summary>
    public Task<string> CreateChed(CertificateBuilder ched, CancellationToken cancellationToken = default) =>
        Create(Cheds, ched.Build(), "a CHED", cancellationToken);

    public Task<string> CreateChed(CertificateControlModel ched, CancellationToken cancellationToken = default) =>
        Create(Cheds, ched, "a CHED", cancellationToken);

    public Task UpdateChed(string id, CertificateBuilder ched, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Put, $"{Cheds}/{id}", ched.Build(), $"update CHED '{id}'", cancellationToken);

    /// <summary>
    /// Merges a partial CHED into a stored one. This is how a decision is applied: build a CHED
    /// carrying only the clearance block and the new status, and send that.
    /// </summary>
    public Task PatchChed(string id, CertificateBuilder patch, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Patch, $"{Cheds}/{id}", patch.Build(), $"patch CHED '{id}'", cancellationToken);

    public Task DeleteChed(string id, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Delete, $"{Cheds}/{id}", null, $"delete CHED '{id}'", cancellationToken);

    /// <summary>Creates an INTRA and returns the ID it was stored under.</summary>
    public Task<string> CreateIntra(CertificateBuilder intra, CancellationToken cancellationToken = default) =>
        Create(Intras, intra.Build(), "an INTRA", cancellationToken);

    public Task UpdateIntra(string id, CertificateBuilder intra, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Put, $"{Intras}/{id}", intra.Build(), $"update INTRA '{id}'", cancellationToken);

    public Task PatchIntra(string id, CertificateBuilder patch, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Patch, $"{Intras}/{id}", patch.Build(), $"patch INTRA '{id}'", cancellationToken);

    public Task DeleteIntra(string id, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Delete, $"{Intras}/{id}", null, $"delete INTRA '{id}'", cancellationToken);

    /// <summary>Empties the simulator of CHEDs and INTRAs alike. Call it before a test, not after.</summary>
    public Task Reset(CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Post, "/control/reset", null, "reset the simulator", cancellationToken);

    private async Task<string> Create(
        string collection,
        CertificateControlModel certificate,
        string what,
        CancellationToken cancellationToken
    )
    {
        var response = await client.PostAsJsonAsync(collection, certificate, s_json, cancellationToken);
        await Ensure(response, $"create {what}", cancellationToken);

        var created = await response.Content.ReadFromJsonAsync<StoredCertificate>(s_json, cancellationToken);

        return created!.Id;
    }

    private async Task Send(
        HttpMethod method,
        string path,
        CertificateControlModel? body,
        string what,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body, options: s_json),
        };

        var response = await client.SendAsync(request, cancellationToken);
        await Ensure(response, what, cancellationToken);
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

    private record StoredCertificate(string Id);
}
