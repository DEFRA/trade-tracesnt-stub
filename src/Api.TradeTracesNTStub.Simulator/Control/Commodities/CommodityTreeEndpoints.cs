using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.TradeTracesNTStub.Simulator.Control.Commodities;

/// <summary>
/// The commodity trees CHEDs are checked and described against: refreshed from TRACES through the
/// POC, uploaded directly, or read back (for a commodity picker).
/// </summary>
public static class CommodityTreeEndpoints
{
    public static void MapCommodityTrees(this RouteGroupBuilder control)
    {
        var trees = control.MapGroup("/commodity-trees");

        trees
            .MapGet("/", List)
            .WithSummary("List the commodity trees loaded")
            .Produces<IReadOnlyList<CommodityTreeSummary>>();

        trees
            .MapGet("/{chedType}", Get)
            .WithSummary("Get a CHED type's commodity tree")
            .Produces<CommodityTree>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        trees
            .MapPut("/{chedType}", Put)
            .WithSummary("Store a CHED type's commodity tree")
            .WithDescription(
                "Takes the tree as TRACES publishes it — the POC's /classificationTrees/{treeId}?format=json — "
                    + "and replaces this type's."
            )
            .Produces<CommodityTreeSummary>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        trees
            .MapDelete("/{chedType}", Delete)
            .WithSummary("Remove a CHED type's commodity tree")
            .WithDescription("Its CN codes then come from the seeded cn_code.json, as before any import.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trees
            .MapPost("/refresh", Refresh)
            .WithSummary("Refresh the commodity trees from TRACES")
            .WithDescription(
                "Fetches each CHED type's tree (or only ?type=) through the POC configured as "
                    + "Simulator:CommodityTrees:SourceUrl, and stores it. A failure stops the refresh; "
                    + "trees fetched before it stay stored."
            )
            .Produces<IReadOnlyList<CommodityTreeSummary>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    private static async Task<IResult> List(ICommodityTreeStore store) => Results.Ok(await store.ListAsync());

    private static async Task<IResult> Get(string chedType, ICommodityTreeStore store) =>
        await store.GetAsync(chedType) is { } tree ? Results.Ok(tree) : NoTree(chedType);

    private static async Task<IResult> Put(
        string chedType,
        IReadOnlyList<CommodityNode> nodes,
        CommodityCatalogue catalogue
    ) =>
        CommodityTrees.TreeIds.ContainsKey(chedType)
            ? Results.Ok(await catalogue.PutAsync(chedType, "upload", nodes))
            : UnknownType(chedType);

    private static async Task<IResult> Delete(string chedType, CommodityCatalogue catalogue) =>
        await catalogue.RemoveAsync(chedType) ? Results.NoContent() : NoTree(chedType);

    private static async Task<IResult> Refresh(
        string? type,
        CommodityTreeSource source,
        CommodityCatalogue catalogue,
        CancellationToken token
    )
    {
        if (source.SourceUrl is null)
        {
            return Results.Problem(
                "No source is configured. Set Simulator:CommodityTrees:SourceUrl to the POC, or PUT a tree instead.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        if (type is not null && !CommodityTrees.TreeIds.ContainsKey(type))
        {
            return UnknownType(type);
        }

        var types = type is null ? CommodityTrees.TreeIds.Keys : [type.ToUpperInvariant()];
        var refreshed = new List<CommodityTreeSummary>();

        foreach (var chedType in types)
        {
            var treeId = CommodityTrees.TreeIds[chedType];
            IReadOnlyList<CommodityNode> nodes;
            try
            {
                nodes = await source.FetchAsync(treeId, token);
            }
            catch (HttpRequestException exception)
            {
                return Results.Problem(
                    $"Could not fetch the CHED-{chedType} tree ({treeId}) from {source.SourceUrl}: {exception.Message}"
                        + (refreshed.Count > 0 ? $" Refreshed before it: {string.Join(", ", refreshed.Select(t => t.ChedType))}." : ""),
                    statusCode: StatusCodes.Status502BadGateway
                );
            }

            refreshed.Add(await catalogue.PutAsync(chedType, source.SourceUrl, nodes));
        }

        return Results.Ok(refreshed);
    }

    private static IResult NoTree(string chedType) =>
        Results.Problem(
            $"No commodity tree is loaded for CHED type '{chedType}'.",
            statusCode: StatusCodes.Status404NotFound
        );

    private static IResult UnknownType(string chedType) =>
        Results.Problem(
            $"'{chedType}' is not a CHED type with a commodity tree. Use one of {string.Join(", ", CommodityTrees.TreeIds.Keys)}.",
            statusCode: StatusCodes.Status404NotFound
        );
}
