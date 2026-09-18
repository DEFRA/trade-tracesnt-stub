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
    private const string DeclarationTypeCode = "4";
    private const string ClearanceTypeCode = "1";

    /// <summary>Baseport name positions — order is the only thing distinguishing them on the wire.</summary>
    private const int CountryName = 0;
    private const int ActivityIdName = 2;
    private const int UnLocodeName = 5;

    public static ChedCertificateQueryResultType Of(SPSCertificateType certificate)
    {
        var document = certificate.SPSExchangedDocument;
        var consignment = certificate.SPSConsignment;
        var commodity = FirstCommodity(consignment);
        var updated = LastUpdated(document) ?? Moment(document.IssueDateTime) ?? DateTime.UtcNow;
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

    /// <summary>The CHED type note carries the code and its display name already resolved.</summary>
    private static CodeType? ChedType(SPSExchangedDocumentType document) =>
        document
            .IncludedSPSNote?.FirstOrDefault(note =>
                note.SubjectCode?.Value == ChedCertificateBuilder.ChedTypeNoteSubject
            )
            ?.ContentCode?.FirstOrDefault();

    private static DateTime? LastUpdated(SPSExchangedDocumentType document)
    {
        var note = document
            .IncludedSPSNote?.FirstOrDefault(note => note.SubjectCode?.Value == ChedCertificateBuilder.LastUpdateNoteSubject)
            ?.Content?.FirstOrDefault()
            ?.Value;

        return DateTimeOffset.TryParse(note, out var parsed) ? parsed.UtcDateTime : null;
    }

    private static DateTime? ActualDateTime(SPSExchangedDocumentType document, string typeCode)
    {
        var wanted = XmlEnums.Parse<GovernmentActionCodeContentType>(typeCode);

        return Moment(
            document
                .SignatorySPSAuthentication?.FirstOrDefault(authentication => authentication.TypeCode?.Value == wanted)
                ?.ActualDateTime
        );
    }

    /// <summary>The first real commodity — the totals line is sequence zero and describes no goods.</summary>
    private static SPSTradeLineItemType? FirstCommodity(SPSConsignmentType consignment) =>
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

    private static string? AddressCountry(SPSPartyType? party) => party?.SpecifiedSPSAddress?.CountryID?.Value;

    private static IDType? Id(string? value) => value is null ? null : new IDType { Value = value };

    private static DateTime? Moment(DateTimeType? value) => value?.Item as DateTime?;
}
