using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Control.Commodities;
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

    /// <summary>The model a CHED was built from, after any patches. Read the certificate itself over SOAP.</summary>
    public Task<StoredCertificateResponse> GetChed(string id, CancellationToken cancellationToken = default) =>
        Get<StoredCertificateResponse>($"{Cheds}/{id}", $"get CHED '{id}'", cancellationToken);

    /// <summary>One page of every CHED, newest first, inaccessible ones included. <paramref name="page"/> counts from 1.</summary>
    public Task<CertificatePage> ListCheds(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Get<CertificatePage>($"{Cheds}?page={page}&pageSize={pageSize}", "list CHEDs", cancellationToken);

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

    public Task<StoredCertificateResponse> GetIntra(string id, CancellationToken cancellationToken = default) =>
        Get<StoredCertificateResponse>($"{Intras}/{id}", $"get INTRA '{id}'", cancellationToken);

    public Task UpdateIntra(string id, CertificateBuilder intra, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Put, $"{Intras}/{id}", intra.Build(), $"update INTRA '{id}'", cancellationToken);

    public Task PatchIntra(string id, CertificateBuilder patch, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Patch, $"{Intras}/{id}", patch.Build(), $"patch INTRA '{id}'", cancellationToken);

    public Task DeleteIntra(string id, CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Delete, $"{Intras}/{id}", null, $"delete INTRA '{id}'", cancellationToken);

    /// <summary>Empties the simulator of CHEDs and INTRAs alike. Call it before a test, not after.</summary>
    public Task Reset(CancellationToken cancellationToken = default) =>
        Send(HttpMethod.Post, "/control/reset", null, "reset the simulator", cancellationToken);

    /// <summary>The codes one seeded list accepts, e.g. <c>cn_code</c> or <c>operators</c>.</summary>
    public Task<IReadOnlyList<LookupEntry>> GetLookup(string list, CancellationToken cancellationToken = default) =>
        Get<IReadOnlyList<LookupEntry>>($"/control/lookups/{list}", $"get code list '{list}'", cancellationToken);

    /// <summary>
    /// Stores a CHED type's commodity tree, in the shape TRACES publishes it. Its CN codes then decide
    /// what a CHED of that type may carry; <see cref="DeleteCommodityTree"/> undoes it.
    /// </summary>
    public async Task PutCommodityTree(
        string chedType,
        IReadOnlyList<CommodityNode> nodes,
        CancellationToken cancellationToken = default
    )
    {
        var response = await client.PutAsJsonAsync($"/control/commodity-trees/{chedType}", nodes, s_json, cancellationToken);
        await Ensure(response, $"store the CHED-{chedType} commodity tree", cancellationToken);
    }

    /// <summary>Removes a CHED type's tree, so its CN codes come from the seeded list again.</summary>
    public async Task DeleteCommodityTree(string chedType, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync($"/control/commodity-trees/{chedType}", cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            await Ensure(response, $"remove the CHED-{chedType} commodity tree", cancellationToken);
        }
    }

    /// <summary>Refreshes every CHED type's tree from TRACES, through the POC the simulator is configured with.</summary>
    public async Task<IReadOnlyList<CommodityTreeSummary>> RefreshCommodityTrees(CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsync("/control/commodity-trees/refresh", null, cancellationToken);
        await Ensure(response, "refresh the commodity trees", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<CommodityTreeSummary>>(s_json, cancellationToken))!;
    }

    private async Task<T> Get<T>(string path, string what, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(path, cancellationToken);
        await Ensure(response, what, cancellationToken);

        return (await response.Content.ReadFromJsonAsync<T>(s_json, cancellationToken))!;
    }

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
