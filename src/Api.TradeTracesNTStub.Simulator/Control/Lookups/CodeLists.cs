using System.Reflection;
using System.Text.Json;

namespace Api.TradeTracesNTStub.Simulator.Control.Lookups;

/// <summary>
/// The display names TRACES derives from a code — submitted as <c>1</c>, retrieved as
/// <c>name="To be done (New)"</c>. Trade Gateway copies that name and never derives it, so if the
/// simulator omits it nothing downstream supplies it. Hence an unknown code fails loudly rather
/// than producing a blank, which would surface much later as an unexplained diff.
/// </summary>
public sealed class CodeLists
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, CodeEntry>> _lists;

    public CodeLists(IReadOnlyDictionary<string, IReadOnlyDictionary<string, CodeEntry>> lists) => _lists = lists;

    /// <summary>The lists shipped with the simulator.</summary>
    public static CodeLists Seeded { get; } = LoadSeeded();

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

    /// <summary>Takes a code or an alias, so a test can write <c>NEW</c> rather than <c>1</c>.</summary>
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

public record CodeEntry
{
    public string Name { get; init; } = "";

    public string? ListName { get; init; }

    /// <summary>A readable name a test can use instead of the raw code — <c>NEW</c> rather than <c>1</c>.</summary>
    public string? Alias { get; init; }

    /// <summary>
    /// Note subjects only. Names the list the value is drawn from, so it belongs in <c>ContentCode</c>.
    /// Absent means free text in <c>Content</c> — <c>CHED_TYPE</c> versus <c>LAST_UPDATE_DATETIME</c>.
    /// </summary>
    public string? ContentCodeList { get; init; }

    /// <summary>
    /// For codes that expand to several — a CN code's hierarchy, a border post's six positional
    /// names. Order is significant.
    /// </summary>
    public IReadOnlyList<LocalisedName>? Names { get; init; }
}

/// <summary>Surfaces as a 400 naming the file to edit.</summary>
public class UnknownCodeException(string message) : Exception(message);
