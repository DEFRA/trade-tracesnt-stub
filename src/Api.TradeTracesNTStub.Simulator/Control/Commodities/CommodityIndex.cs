using Api.TradeTracesNTStub.Simulator.Control.Lookups;

namespace Api.TradeTracesNTStub.Simulator.Control.Commodities;

/// <summary>
/// A tree as the builder reads it: each CN code with the description path TRACES serves as its
/// <c>ClassName</c> elements, the coded ancestors' descriptions then its own ("LIVE ANIMALS", "Live
/// horses, asses, mules and hinnies"). Labels group codes but aren't in the path.
/// </summary>
/// <remarks>
/// Any coded node is valid, selectable or not, because the CHED-D tree marks nothing selectable. A
/// code that appears under two labels takes its first occurrence.
/// </remarks>
public sealed class CommodityIndex
{
    private readonly CommodityTree _tree;
    private readonly Dictionary<string, IReadOnlyList<LocalisedName>> _names = new(StringComparer.OrdinalIgnoreCase);

    public CommodityIndex(CommodityTree tree)
    {
        _tree = tree;
        Add(tree.Nodes, []);
    }

    public int Count => _names.Count;

    public bool Contains(string code) => _names.ContainsKey(code);

    public IReadOnlyList<LocalisedName> NamesOf(string code) =>
        _names.TryGetValue(code, out var names)
            ? names
            : throw new UnknownCodeException(
                $"Code '{code}' is not in the CHED-{_tree.ChedType} commodity tree ({_tree.TreeId}, imported "
                    + $"{_tree.ImportedAt:u}). Refresh it with POST /control/commodity-trees/refresh."
            );

    private void Add(IEnumerable<CommodityNode> nodes, IReadOnlyList<LocalisedName> ancestors)
    {
        foreach (var node in nodes)
        {
            var path = ancestors;

            if (node.Item?.Value is { Length: > 0 } code && node.Description is { } description)
            {
                path = [.. ancestors, new LocalisedName(description.Value, description.LanguageId ?? "en")];
                _names.TryAdd(code, path);
            }

            Add(node.Node ?? [], path);
        }
    }
}
