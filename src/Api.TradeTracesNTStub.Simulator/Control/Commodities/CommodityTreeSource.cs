using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Api.TradeTracesNTStub.Simulator.Control.Commodities;

/// <summary>
/// Where trees come from: a refresh fetches them from trade-traces-poc, whose default target is real
/// TRACES.
/// </summary>
public class CommodityTreeOptions
{
    /// <summary>The POC's base URL, e.g. <c>http://trade-traces-poc:8080</c>. Unset, refresh is refused.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>The CDP developer key, sent as <c>x-api-key</c>, when the POC sits behind the CDP gateway.</summary>
    public string? ApiKey { get; set; }
}

public class CommodityTreeSource(HttpClient client, IOptions<CommodityTreeOptions> options)
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    public string? SourceUrl => options.Value.SourceUrl?.TrimEnd('/');

    public async Task<IReadOnlyList<CommodityNode>> FetchAsync(string treeId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{SourceUrl}/classificationTrees/{treeId}/?format=json"
        );
        request.Headers.Accept.ParseAdd("application/json");
        if (options.Value.ApiKey is { Length: > 0 } apiKey)
        {
            request.Headers.Add("x-api-key", apiKey);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<CommodityNode>>(s_json, cancellationToken)
            ?? [];
    }
}
