using System.ComponentModel.DataAnnotations;

namespace Api.TradeTracesNTStub.Simulator.Control.Models;

/// <summary>
/// What a test supplies to create or update a CHED.
/// </summary>
/// <remarks>
/// Deliberately not a copy of the TRACES XML schema, and deliberately not exhaustive. Everything
/// except <see cref="Type"/> is optional: anything left unset keeps the value from the baseline
/// template, so a test states only what its scenario actually turns on. Keeping this independent of
/// the generated types is what lets the control surface stay small while the SOAP surface grows.
/// </remarks>
public record ChedControlModel
{
    /// <summary>Which CHED this is. Selects the baseline template unless <see cref="Template"/> overrides it.</summary>
    [Required]
    public ChedType Type { get; init; }

    /// <summary>The certificate ID. Generated in the TRACES format when omitted.</summary>
    public string? Id { get; init; }

    /// <summary>A named baseline to start from. Defaults to the one for <see cref="Type"/>.</summary>
    public string? Template { get; init; }

    /// <summary>TRACES status, by name (<c>NEW</c>) or raw code (<c>1</c>).</summary>
    public string? Status { get; init; }

    /// <summary>
    /// When false the SOAP face returns a permission-denied fault for this CHED instead of serving it.
    /// Lets a test cover the 403 path without a magic ID or a global fault-injection switch.
    /// </summary>
    public bool Accessible { get; init; } = true;

    public PartyModel? Consignor { get; init; }

    public PartyModel? Consignee { get; init; }

    public PartyModel? DeliveryParty { get; init; }

    /// <summary>The BCP the consignment arrives at, by its TRACES activity code or UN/LOCODE.</summary>
    public string? BorderControlPost { get; init; }

    /// <summary>Replaces the template's commodities outright when given. An empty list clears them.</summary>
    public IReadOnlyList<CommodityModel>? Commodities { get; init; }

    /// <summary>The official inspector's decision. Absent means the CHED has not been decided.</summary>
    public DecisionModel? Decision { get; init; }
}

public enum ChedType
{
    A,
    P,
    PP,
    D,
}

/// <summary>
/// An operator on the consignment. Give <see cref="OperatorId"/> alone to have the simulator fill in
/// the name and address the way TRACES does; give the other fields to override that.
/// </summary>
public record PartyModel
{
    public string? OperatorId { get; init; }

    public string? Name { get; init; }

    public string? CountryCode { get; init; }
}

public record CommodityModel
{
    /// <summary>Combined Nomenclature code. The simulator supplies the description hierarchy.</summary>
    public string? CnCode { get; init; }

    public string? Description { get; init; }

    /// <summary>ISO 3166-1 alpha-2. The simulator supplies the country name.</summary>
    public string? OriginCountry { get; init; }

    public decimal? NetWeightKg { get; init; }

    public decimal? GrossWeightKg { get; init; }

    /// <summary>UNECE package type code, e.g. <c>BX</c> for a box.</summary>
    public string? PackageType { get; init; }

    public decimal? PackageCount { get; init; }

    public string? ScientificName { get; init; }
}

/// <summary>
/// The clearance decision, written as the official-inspector authentication block TRACES adds when a
/// CHED is decided.
/// </summary>
public record DecisionModel
{
    /// <summary>e.g. <c>ACCEPTABLE_FOR_FREE_CIRCULATION</c> or <c>NOT_ACCEPTABLE</c>.</summary>
    [Required]
    public string Conclusion { get; init; } = "";

    public string? DocumentaryCheck { get; init; }

    public string? IdentityCheck { get; init; }

    public string? PhysicalCheck { get; init; }

    /// <summary>Only meaningful when <see cref="Conclusion"/> is a refusal.</summary>
    public IReadOnlyList<string>? RefusalReasons { get; init; }

    /// <summary>The measure taken on a refused consignment, e.g. <c>DESTRUCTION</c>.</summary>
    public string? NotAcceptableMeasure { get; init; }
}
