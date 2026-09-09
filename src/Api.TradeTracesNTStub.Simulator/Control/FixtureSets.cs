using System.Text.Json;
using System.Text.Json.Serialization;
using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// Named sets of CHEDs a test can reset to.
/// </summary>
/// <remarks>
/// Plain control-model JSON in a directory on disk rather than embedded resources, so a scenario can
/// be added, shared and reviewed in a pull request without a rebuild.
/// </remarks>
public sealed class FixtureSets(string root)
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public IReadOnlyList<string> Names =>
        !Directory.Exists(root)
            ? []
            : [.. Directory.EnumerateDirectories(root).Select(Path.GetFileName).OfType<string>().Order()];

    public bool Exists(string name) => Directory.Exists(PathFor(name));

    /// <summary>
    /// Every CHED in a set, in file-name order so a set that depends on ordering behaves the same
    /// on every machine.
    /// </summary>
    public IReadOnlyList<ChedControlModel> Load(string name)
    {
        var directory = PathFor(name);

        if (!Directory.Exists(directory))
        {
            throw new ArgumentException(
                $"No fixture set named '{name}'. Available: {string.Join(", ", Names)}.",
                nameof(name)
            );
        }

        return
        [
            .. Directory
                .EnumerateFiles(directory, "*.json")
                .Order(StringComparer.Ordinal)
                .Select(Read),
        ];
    }

    private static ChedControlModel Read(string file)
    {
        try
        {
            return JsonSerializer.Deserialize<ChedControlModel>(File.ReadAllText(file), s_json)
                ?? throw new InvalidOperationException($"Fixture '{file}' is empty.");
        }
        catch (JsonException exception)
        {
            // Name the file — a parse error with only a line number is miserable to track down.
            throw new InvalidOperationException($"Fixture '{file}' is not valid JSON: {exception.Message}", exception);
        }
    }

    private string PathFor(string name)
    {
        // A set name reaches the filesystem, so it must stay a single path segment.
        if (name.Contains('/') || name.Contains('\\') || name.Contains(".."))
        {
            throw new ArgumentException($"'{name}' is not a valid fixture set name.", nameof(name));
        }

        return Path.Combine(root, name);
    }
}
