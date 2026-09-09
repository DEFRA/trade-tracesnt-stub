namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// The UN/CEFACT namespaces the CHED document is built from. The generated types carry these on
/// every member, but a few places — deserialising a template, writing a test assertion — need them
/// by name.
/// </summary>
public static class SpsNamespaces
{
    public const string SpsCertificate = "urn:un:unece:uncefact:data:standard:SPSCertificate:17";

    /// <summary>Reusable Aggregate Business Information Entity — where almost every element lives.</summary>
    public const string Rbe =
        "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:21";

    public const string UnqualifiedDataType = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:21";

    public const string ChedV2 = "http://ec.europa.eu/tracesnt/certificate/ched/v2";
}
