using System.Reflection;
using System.Text.Json;

namespace Api.TradeTracesNTStub.Simulator.Control.Lookups;

/// <summary>
/// The display names TRACES derives from a code.
/// </summary>
/// <remarks>
/// TRACES enriches on the way out: a document is submitted with <c>&lt;StatusCode&gt;1&lt;/StatusCode&gt;</c>
/// and retrieved as <c>&lt;StatusCode name="To be done (New)"&gt;1&lt;/StatusCode&gt;</c>. Trade Gateway copies
/// that <c>name</c> straight into its own model and never derives it, so if the simulator does not
/// supply it nothing downstream will.
/// <para>
/// Only the codes the shipped fixtures and tests use are seeded. An unknown code is a loud failure
/// rather than a blank name, because a blank name surfaces much later as an unexplained snapshot diff.
/// To add one, edit the matching file under <c>Control/Lookups/SeedData</c>.
/// </para>
/// </remarks>
public sealed class CodeLists
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, CodeEntry>> _lists;

    public CodeLists(IReadOnlyDictionary<string, IReadOnlyDictionary<string, CodeEntry>> lists) => _lists = lists;

    /// <summary>The lists shipped with the simulator.</summary>
    public static CodeLists Seeded { get; } = LoadSeeded();

    /// <summary>
    /// The display name for a code, or throws naming the list and code so the caller knows exactly
    /// what to add to the seed data.
    /// </summary>
    public CodeEntry Get(string listId, string code)
    {
        if (!_lists.TryGetValue(listId, out var list))
        {
            throw new UnknownCodeException(
                $"No code list '{listId}' is seeded. Add {listId}.json under Control/Lookups/SeedData."
            );
        }

        if (!list.TryGetValue(code, out var entry))
        {
            throw new UnknownCodeException(
                $"Code '{code}' is not in code list '{listId}'. "
                    + $"Add it to Control/Lookups/SeedData/{listId}.json so the simulator can supply its display name."
            );
        }

        return entry;
    }

    public bool TryGet(string listId, string code, out CodeEntry entry)
    {
        entry = default!;
        return _lists.TryGetValue(listId, out var list) && list.TryGetValue(code, out entry!);
    }

    /// <summary>
    /// Resolves either a raw code or a friendly alias to its code and display name, so a test can
    /// write <c>"NEW"</c> instead of remembering that a new CHED is status <c>1</c>.
    /// </summary>
    public (string Code, CodeEntry Entry) Resolve(string listId, string codeOrAlias)
    {
        if (TryGet(listId, codeOrAlias, out var direct))
        {
            return (codeOrAlias, direct);
        }

        if (!_lists.TryGetValue(listId, out var list))
        {
            throw new UnknownCodeException(
                $"No code list '{listId}' is seeded. Add {listId}.json under Control/Lookups/SeedData."
            );
        }

        var byAlias = list.FirstOrDefault(pair =>
            string.Equals(pair.Value.Alias, codeOrAlias, StringComparison.OrdinalIgnoreCase)
        );

        if (byAlias.Value is not null)
        {
            return (byAlias.Key, byAlias.Value);
        }

        throw new UnknownCodeException(
            $"'{codeOrAlias}' is neither a code nor an alias in code list '{listId}'. "
                + $"Add it to Control/Lookups/SeedData/{listId}.json so the simulator can supply its display name."
        );
    }

    private static CodeLists LoadSeeded()
    {
        const string prefix = "Api.TradeTracesNTStub.Simulator.Control.Lookups.SeedData.";
        var assembly = typeof(CodeLists).Assembly;

        var lists = assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
            .ToDictionary(
                name => name[prefix.Length..^".json".Length],
                name => Read(assembly, name),
                StringComparer.OrdinalIgnoreCase
            );

        return new CodeLists(lists);
    }

    private static IReadOnlyDictionary<string, CodeEntry> Read(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)!;

        var entries =
            JsonSerializer.Deserialize<Dictionary<string, CodeEntry>>(
                stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    // Seed files carry provenance notes, so comments have to be tolerated.
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                }
            ) ?? [];

        return entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// One code's display metadata. <see cref="ListName"/> is the human label for the list itself, which
/// TRACES emits alongside the code as <c>listName</c>.
/// </summary>
public record CodeEntry
{
    public string Name { get; init; } = "";

    public string? ListName { get; init; }

    /// <summary>
    /// A readable name a test can use instead of the raw code — <c>NEW</c> rather than <c>1</c>.
    /// </summary>
    public string? Alias { get; init; }

    /// <summary>
    /// Display strings for codes that expand to more than one — a CN code's chapter-then-description
    /// hierarchy, or a border control post's six positional names. Order is significant.
    /// </summary>
    public IReadOnlyList<LocalisedName>? Names { get; init; }
}

/// <summary>
/// Thrown when a control model names a code the simulator has no display name for. Surfaces as a 400
/// so the test author sees it immediately rather than finding a blank label downstream.
/// </summary>
public class UnknownCodeException(string message) : Exception(message);
