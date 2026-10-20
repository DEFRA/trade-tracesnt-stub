namespace Api.TradeTracesNTStub.Simulator.Control.Commodities;

/// <summary>
/// One CHED type's commodity classification tree, as TRACES publishes it through
/// <c>getClassificationTree</c> (the POC serves it as JSON at <c>/classificationTrees/{treeId}</c>). It
/// is the authority on which CN codes a CHED of that type may carry, and on how each is described.
/// </summary>
public record CommodityTree(
    string ChedType,
    string TreeId,
    DateTimeOffset ImportedAt,
    string Source,
    IReadOnlyList<CommodityNode> Nodes
);

/// <summary>A node as TRACES sends it. Labels (<c>type</c> 1) have no code; they only group codes.</summary>
public record CommodityNode
{
    public CommodityCode? Item { get; init; }

    public CommodityText? Description { get; init; }

    /// <summary>What TRACES lets a user pick. The CHED-D tree marks nothing, so it doesn't decide validity here.</summary>
    public bool AllowedForSelection { get; init; }

    /// <summary>0 a CN code, 1 a label, 4 "No Commodities".</summary>
    public int Type { get; init; }

    public string? Path { get; init; }

    public IReadOnlyList<CommodityNode>? Node { get; init; }
}

public record CommodityCode(string Value);

public record CommodityText(string Value, string? LanguageId = null);

/// <summary>A stored tree without its nodes: enough to say what's loaded, and from where.</summary>
public record CommodityTreeSummary(
    string ChedType,
    string TreeId,
    DateTimeOffset ImportedAt,
    string Source,
    int Nodes,
    int Codes
);

public static class CommodityTrees
{
    /// <summary>Each CHED type's tree in TRACES.</summary>
    public static IReadOnlyDictionary<string, string> TreeIds { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = "cheda",
            ["P"] = "chedp",
            ["PP"] = "chedpp",
            ["D"] = "chedd",
        };

    public static CommodityTreeSummary Summarise(CommodityTree tree)
    {
        var nodes = Flatten(tree.Nodes).ToList();
        return new CommodityTreeSummary(
            tree.ChedType,
            tree.TreeId,
            tree.ImportedAt,
            tree.Source,
            nodes.Count,
            nodes.Where(node => node.Item is not null).Select(node => node.Item!.Value).Distinct().Count()
        );
    }

    public static IEnumerable<CommodityNode> Flatten(IEnumerable<CommodityNode> nodes) =>
        nodes.SelectMany(node => Flatten(node.Node ?? []).Prepend(node));
}
