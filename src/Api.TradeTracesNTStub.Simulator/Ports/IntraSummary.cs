using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

/// <summary>
/// The search-result view of a stored INTRA, projected from the certificate as <see cref="ChedSummary"/>
/// projects a CHED. Fields the builder never writes — local ID, competent authorities, transit and
/// place-of-loading countries — are left out rather than invented.
/// </summary>
public static class IntraSummary
{
    public static EuIntraCertificateQueryResultType Of(SPSCertificateType certificate)
    {
        var document = certificate.SPSExchangedDocument;
        var consignment = certificate.SPSConsignment;
        var commodity = ChedSummary.FirstCommodity(consignment);
        var updated = ChedSummary.Updated(document);
        var declared = ChedSummary.ActualDateTime(document, ChedSummary.DeclarationTypeCode);
        var certified = ChedSummary.ActualDateTime(document, ChedSummary.ClearanceTypeCode);

        return new EuIntraCertificateQueryResultType
        {
            ID = document.ID?.Value ?? "",
            Status = new StatusCodeType { Value = document.StatusCode.Value, name = document.StatusCode.name },
            CommodityApplicableSPSClassification = commodity?.ApplicableSPSClassification ?? [],

            CountryOfDispatch = ChedSummary.Id(consignment.ExportSPSCountry?.ID?.Value),
            CountryOfDestination = ChedSummary.Id(consignment.ImportSPSCountry?.ID?.Value),
            CountryOfOrigin = [.. commodity?.OriginSPSCountry?.Select(country => ChedSummary.Id(country.ID?.Value)!) ?? []],

            CountryOfConsignor = ChedSummary.Id(ChedSummary.AddressCountry(consignment.ConsignorSPSParty)),
            ConsignorName = consignment.ConsignorSPSParty?.Name?.Value,
            CountryOfConsignee = ChedSummary.Id(ChedSummary.AddressCountry(consignment.ConsigneeSPSParty)),
            ConsigneeName = consignment.ConsigneeSPSParty?.Name?.Value,

            // The simulator tracks no separate create or status-change time.
            CreateDateTime = ChedSummary.Moment(document.IssueDateTime) ?? updated,
            UpdateDateTime = updated,
            StatusChangeDateTime = updated,
            StatusChangeDateTimeSpecified = true,
            DeclarationDateTime = declared ?? default,
            DeclarationDateTimeSpecified = declared.HasValue,
            // An INTRA's clearance is its certification: same signatory type code, different name.
            CertificationDateTime = certified ?? default,
            CertificationDateTimeSpecified = certified.HasValue,
        };
    }
}
