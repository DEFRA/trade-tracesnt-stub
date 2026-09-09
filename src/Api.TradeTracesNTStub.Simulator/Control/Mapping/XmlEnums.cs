using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Serialization;

namespace Api.TradeTracesNTStub.Simulator.Control.Mapping;

/// <summary>
/// Converts between a code as it appears on the wire and the generated enum member for it.
/// </summary>
/// <remarks>
/// The generated contracts turn every code list into an enum whose members are named for C#, not for
/// TRACES — status <c>1</c> is <c>StatusCodeContentType.Item1</c> and the real value lives in an
/// <see cref="XmlEnumAttribute"/>. Reading that attribute is the only way to go from a code a test
/// wrote to the member the serialiser will emit, and it works for every one of these enums rather
/// than needing a hand-written map per list.
/// </remarks>
public static class XmlEnums
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, object>> s_byWireValue = new();

    /// <summary>The enum member whose XML value is <paramref name="value"/>.</summary>
    public static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum
    {
        if (TryParse<TEnum>(value, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"'{value}' is not a valid {typeof(TEnum).Name} value.", nameof(value));
    }

    public static bool TryParse<TEnum>(string value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        var map = s_byWireValue.GetOrAdd(typeof(TEnum), Build);

        if (map.TryGetValue(value, out var found))
        {
            parsed = (TEnum)found;
            return true;
        }

        parsed = default;
        return false;
    }

    /// <summary>The wire value for an enum member — the inverse of <see cref="Parse{TEnum}"/>.</summary>
    public static string ToWireValue<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        var field = typeof(TEnum).GetField(name);

        return field?.GetCustomAttribute<XmlEnumAttribute>()?.Name ?? name;
    }

    /// <summary>
    /// Some of these enums declare two members for one wire value — the code lists they were
    /// generated from overlap. Either member serialises to the same string, so the first wins and
    /// the mapping stays deterministic.
    /// </summary>
    private static IReadOnlyDictionary<string, object> Build(Type enumType) =>
        enumType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .GroupBy(field => field.GetCustomAttribute<XmlEnumAttribute>()?.Name ?? field.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().GetValue(null)!, StringComparer.Ordinal);
}
