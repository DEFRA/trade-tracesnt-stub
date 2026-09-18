using System.ComponentModel.DataAnnotations;

namespace Api.TradeTracesNTStub.Simulator.Control.Models;

/// <summary>
/// A CHED as a submitter would send it: everything DG SANTE mark <c>Issue=M/O/C</c> in the CHED
/// mapping workbook, and nothing they mark <c>N</c>. Property names follow Trade Gateway's JSON model.
/// </summary>
public record ChedControlModel
{
    /// <summary>Generated when omitted. Settable because a test needs to pin it; TRACES assigns its own.</summary>
    public string? Id { get; init; }

    /// <summary>
    /// By name (<c>NEW</c>) or code (<c>1</c>). Simulator state, not submitted content: TRACES derives
    /// status from the operation invoked, so do not "correct" this to match.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>When false the SOAP face returns a permission-denied fault. Simulator state; no TRACES equivalent.</summary>
    public bool? Accessible { get; init; }

    [Required]
    public ExchangedDocumentModel ExchangedDocument { get; init; } = new();

    [Required]
    public ConsignmentModel SpecifiedConsignment { get; init; } = new();
}

public record ExchangedDocumentModel
{
    /// <summary>
    /// Keyed by <c>SubjectCode</c>. <c>CHED_TYPE</c> is required — it is what makes this a CHED-A.
    /// The seed entry decides whether a value lands as a code or as free text.
    /// </summary>
    public IReadOnlyDictionary<string, string> IncludedNote { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// The one deliberate exception to the no-defaulting rule: every retrieved CHED carries one, so
    /// the simulator fabricates a default when this is omitted.
    /// </summary>
    public IReadOnlyList<ReferencedDocumentModel>? ReferenceDocument { get; init; }

    /// <summary>The applicant's declaration — <c>SignatorySPSAuthentication</c> with TypeCode 4.</summary>
    public AuthenticationModel? Declaration { get; init; }

    /// <summary>The inspector's decision — TypeCode 1. Absent until the CHED has been decided.</summary>
    public AuthenticationModel? Clearance { get; init; }
}

public record AuthenticationModel
{
    public DateTimeOffset? ActualDateTime { get; init; }

    /// <summary>
    /// Keyed by clause ID — <c>PURPOSE</c>, <c>DECISION_CONCLUSION</c>. The value is the code; the
    /// display text beside it on the wire is TRACES's to add.
    /// </summary>
    public IReadOnlyDictionary<string, string> IncludedClause { get; init; } =
        new Dictionary<string, string>();
}

public record ReferencedDocumentModel
{
    public string? DocumentTypeCode { get; init; }

    /// <summary>How the document relates to the CHED — <c>ZZZ</c> for a supporting document.</summary>
    public string? RelationshipTypeCode { get; init; }

    public string? Identifier { get; init; }

    /// <summary>Issuing country, written as the reference's <c>schemeAgencyID</c>.</summary>
    public string? IssuingCountry { get; init; }
}

public record ConsignmentModel
{
    public DateTimeOffset? AvailabilityDueDateTime { get; init; }

    /// <summary>ISO 3166-1 alpha-2. The simulator supplies the country name.</summary>
    public string? ExportCountry { get; init; }

    public string? ImportCountry { get; init; }

    public PartyModel? ConsignorParty { get; init; }

    public PartyModel? ConsigneeParty { get; init; }

    public PartyModel? DeliveryParty { get; init; }

    public PartyModel? CustomsTransitAgentParty { get; init; }

    public LocationModel? UnloadingBaseportLocation { get; init; }

    public TransportMovementModel? MainCarriageLogisticsTransportMovement { get; init; }

    public ConsignmentItemModel? IncludedConsignmentItem { get; init; }
}

/// <summary>
/// Either an <see cref="Identifier"/> alone, whose name is looked up, or a <see cref="Name"/> alone —
/// TRACES's operator created on the fly, and the way past the registry for an unseeded operator.
/// </summary>
public record PartyModel
{
    public string? Identifier { get; init; }

    /// <summary>Which identifier scheme, e.g. <c>national_registry_number</c>.</summary>
    public string? SchemeId { get; init; }

    /// <summary>Only for an unregistered operator. Supplying it alongside an identifier overrides the lookup.</summary>
    public string? Name { get; init; }

    public AddressModel? PostalAddress { get; init; }
}

public record AddressModel
{
    /// <summary>ISO 3166-1 alpha-2. The simulator supplies <c>CountryName</c>.</summary>
    public string? CountryId { get; init; }

    public string? PostcodeCode { get; init; }

    public string? LineOne { get; init; }

    public string? CityName { get; init; }

    public string? CountrySubDivisionName { get; init; }
}

/// <summary>
/// The border control post of arrival. The client gives the code and country; the five further
/// <c>Name</c> elements are looked up.
/// </summary>
public record LocationModel
{
    public string? Identifier { get; init; }

    public string? SchemeId { get; init; }

    /// <summary>Country of entry — the first <c>Name</c> on the location, and the only one submitted.</summary>
    public string? CountryId { get; init; }
}

public record TransportMovementModel
{
    /// <summary>UNECE Recommendation 19 mode — <c>3</c> is road.</summary>
    public string? ModeCode { get; init; }

    /// <summary>Vehicle registration, flight number or vessel name.</summary>
    public string? Identifier { get; init; }

    public string? SchemeId { get; init; }

    /// <summary>Country that issued the registration, written as <c>schemeAgencyID</c>.</summary>
    public string? SchemeAgencyId { get; init; }
}

public record ConsignmentItemModel
{
    /// <summary>UNECE cargo type — <c>12</c> is general cargo.</summary>
    public string? NatureIdCargo { get; init; }

    /// <summary>
    /// Written as the sequence-0 trade line item, ahead of the real commodities, with the fixed
    /// description TRACES puts there.
    /// </summary>
    public TradeLineItemModel? ConsignmentTotals { get; init; }

    /// <summary>The commodities, numbered from 1 in the order given.</summary>
    public IReadOnlyList<TradeLineItemModel> IncludedTradeLineItem { get; init; } = [];
}

public record TradeLineItemModel
{
    /// <summary>
    /// Keyed by system — <c>CN</c>, <c>IDENTIFICATION_SYSTEM</c>. The value is the class code; the
    /// system name and description hierarchy are looked up, and one CN code can expand to four levels.
    /// </summary>
    public IReadOnlyDictionary<string, string> ApplicableClassification { get; init; } =
        new Dictionary<string, string>();

    /// <summary>ISO 3166-1 alpha-2. The simulator supplies the country name.</summary>
    public string? OriginCountry { get; init; }

    public string? ScientificName { get; init; }

    public MeasureModel? NetWeight { get; init; }

    public MeasureModel? GrossWeight { get; init; }

    public MeasureModel? NetVolume { get; init; }

    public PackageModel? PhysicalReferencedLogisticsPackage { get; init; }

    /// <summary>Commodity notes keyed by <c>SubjectCode</c>, e.g. <c>INDIVIDUAL_IDENTIFICATION_NUMBER</c>.</summary>
    public IReadOnlyDictionary<string, string> AdditionalInformationNote { get; init; } =
        new Dictionary<string, string>();
}

public record MeasureModel
{
    public decimal Value { get; init; }

    /// <summary>UNECE unit code — <c>KGM</c> kilograms, <c>H87</c> pieces.</summary>
    public string? UnitCode { get; init; }
}

public record PackageModel
{
    /// <summary>UNECE package type — <c>BX</c> box, <c>NA</c> none.</summary>
    public string? TypeCode { get; init; }

    public decimal? ItemQuantity { get; init; }
}
