using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.TradeTracesNTStub.Simulator.Control.Lookups;

/// <summary>
/// A display name, optionally tagged with the language TRACES tags it with.
/// </summary>
/// <remarks>
/// The language matters: the gateway selects names by language, so a name that should carry
/// <c>languageID="en"</c> and does not can be skipped entirely on the way through. Border control
/// posts are the awkward case — they carry six names in a fixed order and only one of them is
/// language-tagged.
/// </remarks>
[JsonConverter(typeof(LocalisedNameConverter))]
public record LocalisedName(string Value, string? LanguageId = null);

/// <summary>
/// Reads either <c>"a name"</c> or <c>{ "value": "a name", "languageId": "en" }</c>, so seed files
/// only spell out the language where there is one.
/// </summary>
public class LocalisedNameConverter : JsonConverter<LocalisedName>
{
    public override LocalisedName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new LocalisedName(reader.GetString()!);
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var element = document.RootElement;

        return new LocalisedName(
            element.GetProperty("value").GetString()!,
            element.TryGetProperty("languageId", out var language) ? language.GetString() : null
        );
    }

    public override void Write(Utf8JsonWriter writer, LocalisedName value, JsonSerializerOptions options)
    {
        if (value.LanguageId is null)
        {
            writer.WriteStringValue(value.Value);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("value", value.Value);
        writer.WriteString("languageId", value.LanguageId);
        writer.WriteEndObject();
    }
}
