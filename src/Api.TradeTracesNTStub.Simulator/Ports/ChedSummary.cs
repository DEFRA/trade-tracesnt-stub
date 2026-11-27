using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

/// <summary>
/// The search-result view of a stored CHED, projected from the certificate each time rather than
/// stored beside it — a result that disagreed with the document it points at is worse than no search.
/// </summary>
public static class ChedSummary
{
    /// <summary>Signatory type codes: the applicant's declaration, and the officer's clearance.</summary>
    internal const string DeclarationTypeCode = "4";
    internal const string ClearanceTypeCode = "1";

    /// <summary>Baseport name positions — order is the only thing distinguishing them on the wire.</summary>
    private const int CountryName = 0;
    private const int ActivityIdName = 2;
    private const int UnLocodeName = 5;

    public static ChedCertificateQueryResultType Of(SPSCertificateType certificate)
    {
        var document = certificate.SPSExchangedDocument;
        var consignment = certificate.SPSConsignment;
        var commodity = FirstCommodity(consignment);
        var updated = Updated(document);
        var declared = ActualDateTime(document, DeclarationTypeCode);
        var decided = ActualDateTime(document, ClearanceTypeCode);

        return new ChedCertificateQueryResultType
        {
            Type = ChedType(document),
            ID = document.ID?.Value ?? "",

            // TRACES writes this empty when the submitter gave no local reference.
            LocalReference = new IDType { Value = "" },
            Status = new StatusCodeType { Value = document.StatusCode.Value, name = document.StatusCode.name },
            CommodityApplicableSPSClassification = commodity?.ApplicableSPSClassification ?? [],

            BCPCode = BaseportCode(consignment, ActivityIdName),
            BCPUnLocode = BaseportCode(consignment, UnLocodeName),
            CountryOfEntry = Id(consignment.ImportSPSCountry?.ID?.Value ?? BaseportName(consignment, CountryName)),
            CountryOfDispatch = Id(consignment.ExportSPSCountry?.ID?.Value),
            CountryOfOrigin = [.. commodity?.OriginSPSCountry?.Select(country => Id(country.ID?.Value)!) ?? []],
            CountryOfPlaceOfDestination = Id(AddressCountry(consignment.DeliverySPSParty)),

            CountryOfConsignor = Id(AddressCountry(consignment.ConsignorSPSParty)),
            ConsignorName = consignment.ConsignorSPSParty?.Name?.Value,
            CountryOfConsignee = Id(AddressCountry(consignment.ConsigneeSPSParty)),
            ConsigneeName = consignment.ConsigneeSPSParty?.Name?.Value,

            // The simulator tracks no separate create or status-change time.
            CreateDateTime = Moment(document.IssueDateTime) ?? updated,
            UpdateDateTime = updated,
            StatusChangeDateTime = updated,
            StatusChangeDateTimeSpecified = true,
            DeclarationDateTime = declared ?? default,
            DeclarationDateTimeSpecified = declared.HasValue,
            DecisionDateTime = decided ?? default,
            DecisionDateTimeSpecified = decided.HasValue,
            PriorNotificationDateTime = Moment(consignment.AvailabilityDueDateTime) ?? default,
            PriorNotificationDateTimeSpecified = consignment.AvailabilityDueDateTime is not null,
        };
    }

    /// <summary>
    /// When the certificate last changed, in UTC: what search filters and sorts on. The stores read it from
    /// here as they store, so it cannot disagree with the <c>UpdateDateTime</c> a search result reports.
    /// </summary>
    public static DateTime UpdatedAt(SPSCertificateType certificate) => AsUtc(Updated(certificate.SPSExchangedDocument));

    /// <summary>
    /// Search bounds arrive at the host's offset, and the builder's datetimes carry no offset at all but are
    /// UTC. Comparing them unconverted would compare wall-clock readings from different clocks.
    /// </summary>
    public static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();

    /// <summary>
    /// A search bound left at its default is no bound — <c>From</c> and <c>To</c> are plain datetimes with
    /// no companion <c>Specified</c> flag, so an omitted one arrives as <c>0001-01-01</c> and a <c>To</c>
    /// read literally would match nothing.
    /// </summary>
    public static DateTime? Bound(DateTime? value) => value is { } bound && bound != default ? AsUtc(bound) : null;

    internal static DateTime Updated(SPSExchangedDocumentType document) =>
        LastUpdated(document) ?? Moment(document.IssueDateTime) ?? DateTime.UtcNow;

    /// <summary>The CHED type note carries the code and its display name already resolved.</summary>
    private static CodeType? ChedType(SPSExchangedDocumentType document) =>
        document
            .IncludedSPSNote?.FirstOrDefault(note =>
                note.SubjectCode?.Value == SpsCertificateBuilder.ChedTypeNoteSubject
            )
            ?.ContentCode?.FirstOrDefault();

    private static DateTime? LastUpdated(SPSExchangedDocumentType document)
    {
        var note = document
            .IncludedSPSNote?.FirstOrDefault(note => note.SubjectCode?.Value == SpsCertificateBuilder.LastUpdateNoteSubject)
            ?.Content?.FirstOrDefault()
            ?.Value;

        return DateTimeOffset.TryParse(note, out var parsed) ? parsed.UtcDateTime : null;
    }

    internal static DateTime? ActualDateTime(SPSExchangedDocumentType document, string typeCode)
    {
        var wanted = XmlEnums.Parse<GovernmentActionCodeContentType>(typeCode);

        return Moment(
            document
                .SignatorySPSAuthentication?.FirstOrDefault(authentication => authentication.TypeCode?.Value == wanted)
                ?.ActualDateTime
        );
    }

    /// <summary>The first real commodity — the totals line is sequence zero and describes no goods.</summary>
    internal static SPSTradeLineItemType? FirstCommodity(SPSConsignmentType consignment) =>
        consignment
            .IncludedSPSConsignmentItem?.SelectMany(item => item.IncludedSPSTradeLineItem ?? [])
            .FirstOrDefault(line => line.SequenceNumeric?.Value is not 0);

    private static string? BaseportName(SPSConsignmentType consignment, int position)
    {
        var names = consignment.UnloadingBaseportSPSLocation?.Name;

        return names is not null && names.Length > position ? names[position].Value : null;
    }

    private static CodeType? BaseportCode(SPSConsignmentType consignment, int position) =>
        BaseportName(consignment, position) is { Length: > 0 } name ? new CodeType { Value = name } : null;

    internal static string? AddressCountry(SPSPartyType? party) => party?.SpecifiedSPSAddress?.CountryID?.Value;

    internal static IDType? Id(string? value) => value is null ? null : new IDType { Value = value };

    internal static DateTime? Moment(DateTimeType? value) => value?.Item as DateTime?;
}
