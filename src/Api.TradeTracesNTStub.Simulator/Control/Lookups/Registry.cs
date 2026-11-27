using System.Reflection;
using System.Text.Json;

namespace Api.TradeTracesNTStub.Simulator.Control.Lookups;

/// <summary>
/// Things TRACES resolves from an identifier — operators, authorities. Separate from
/// <see cref="CodeLists"/> because these are records, not a code and a display name.
/// </summary>
public sealed class Registry<TEntry>(string fileName, IReadOnlyDictionary<string, TEntry> entries)
    where TEntry : class
{
    private const string Prefix = "Api.TradeTracesNTStub.Simulator.Control.Lookups.SeedData.";

    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static Registry<TEntry> Load(string fileName)
    {
        using var stream =
            typeof(Registry<TEntry>).Assembly.GetManifestResourceStream(Prefix + fileName)
            ?? throw new InvalidOperationException($"Seed data '{fileName}' is missing from the assembly.");

        var entries =
            JsonSerializer.Deserialize<Dictionary<string, TEntry>>(stream, s_json)
            ?? throw new InvalidOperationException($"Seed data '{fileName}' is empty.");

        return new Registry<TEntry>(
            fileName,
            entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
        );
    }

    public bool TryGet(string id, out TEntry entry) => entries.TryGetValue(id, out entry!);

    public TEntry Get(string id) =>
        TryGet(id, out var entry)
            ? entry
            : throw new UnknownCodeException(
                $"'{id}' is not in {fileName}. Add it under Control/Lookups/SeedData, "
                    + "or supply the details on the request so no lookup is needed."
            );
}

/// <summary>Resolved from the identifier a submission carries; the submission's own name is empty.</summary>
public record OperatorEntry
{
    public string Name { get; init; } = "";

    /// <summary>Code from <c>operator_activity_type</c>.</summary>
    public string? ActivityType { get; init; }

    /// <summary>Code from <c>classification_section_code</c>.</summary>
    public string? ClassificationSection { get; init; }
}

/// <summary>
/// Supplies the baseport's derived <c>Name</c> elements, where order carries the meaning, and the
/// <c>IssuerSPSParty</c> subtree, which appears in no submission and so has nowhere else to come from.
/// </summary>
public record AuthorityEntry
{
    /// <summary>Authority name — baseport <c>Name[3]</c> and the issuer's name.</summary>
    public string Name { get; init; } = "";

    /// <summary>City of entry — baseport <c>Name[1]</c>, language-tagged on the wire.</summary>
    public string? City { get; init; }

    /// <summary>Authority activity ID — baseport <c>Name[2]</c>, and the issuer party's ID.</summary>
    public string? ActivityId { get; init; }

    /// <summary>Authority address — baseport <c>Name[4]</c>.</summary>
    public string? Address { get; init; }

    /// <summary>UN/LOCODE — baseport <c>Name[5]</c>, empty when the post has none.</summary>
    public string? UnLocode { get; init; }

    /// <summary>The issuing officer's postal address and name, for <c>IssuerSPSParty</c>.</summary>
    public AuthorityAddressEntry? PostalAddress { get; init; }

    public string? OfficerName { get; init; }
}

public record AuthorityAddressEntry
{
    public string? CountryId { get; init; }

    public string? PostcodeCode { get; init; }

    public string? LineOne { get; init; }

    public string? CityName { get; init; }
}
