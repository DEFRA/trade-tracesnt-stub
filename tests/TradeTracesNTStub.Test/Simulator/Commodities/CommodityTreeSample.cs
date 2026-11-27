using System.Text.Json;
using Api.TradeTracesNTStub.Simulator.Control.Commodities;

namespace TradeTracesNTStub.Test.Simulator.Commodities;

/// <summary>
/// A cut of the real CHED-A tree, in the JSON TRACES publishes: a chapter, codes at three depths, a
/// code that isn't selectable, a label grouping a code, and that code again under the chapter.
/// </summary>
public static class CommodityTreeSample
{
    public const string Json = """
        [
          {
            "item": { "value": "01" },
            "description": { "languageID": "en", "value": "LIVE ANIMALS" },
            "allowedForSelection": false, "type": 0, "path": "R/N-10000",
            "node": [
              {
                "item": { "value": "0101" },
                "description": { "languageID": "en", "value": "Live horses, asses, mules and hinnies" },
                "allowedForSelection": true, "type": 0, "path": "R/N-10000/N-10065"
              },
              {
                "item": { "value": "0104" },
                "description": { "languageID": "en", "value": "Live sheep and goats" },
                "allowedForSelection": false, "type": 0, "path": "R/N-10000/N-10068",
                "node": [
                  {
                    "item": { "value": "010410" },
                    "description": { "languageID": "en", "value": "Sheep" },
                    "allowedForSelection": true, "type": 0, "path": "R/N-10000/N-10068/N-10584"
                  }
                ]
              },
              {
                "description": { "languageID": "en", "value": "Cats, Dogs and ferrets." },
                "allowedForSelection": false, "type": 1, "path": "R/N-10000/L-10160",
                "node": [
                  {
                    "item": { "value": "01061900" },
                    "description": { "languageID": "en", "value": "Other" },
                    "allowedForSelection": true, "type": 0, "path": "R/N-10000/L-10160/N-11619"
                  }
                ]
              },
              {
                "item": { "value": "01061900" },
                "description": { "languageID": "en", "value": "Other mammals" },
                "allowedForSelection": true, "type": 0, "path": "R/N-10000/N-11620"
              }
            ]
          }
        ]
        """;

    public static IReadOnlyList<CommodityNode> Nodes { get; } =
        JsonSerializer.Deserialize<IReadOnlyList<CommodityNode>>(Json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    public static CommodityTree Tree(string chedType = "A") =>
        new(chedType, "cheda", new DateTimeOffset(2026, 10, 16, 6, 0, 0, TimeSpan.Zero), "test", Nodes);
}
