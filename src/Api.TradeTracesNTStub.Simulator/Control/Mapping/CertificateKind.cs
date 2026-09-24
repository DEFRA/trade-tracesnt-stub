using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace Api.TradeTracesNTStub.Simulator.Control.Mapping;

/// <summary>
/// What differs between a CHED and an INTRA. Both are the same UN/CEFACT <c>SPSCertificate</c>, built
/// by the same code and sharing the status and party-role lists; TRACES files their notes and clauses
/// under different code lists. Every value here is taken from a retrieved certificate — the CHED ones
/// from EU acceptance, the INTRA ones from the WireMock sample <c>Samples/INTRA/ITAHC.TEMPLATE.xml</c>.
/// </summary>
public sealed class CertificateKind
{
    public static readonly CertificateKind Ched = new()
    {
        Name = "CHED",
        DocumentTypeCode = "636",
        DocumentTypeName = "Health certificate (CHED - Common Health Entry Document)",
        UrlPrefix = "https://webgate.acceptance.ec.europa.eu/tracesnt/certificate/ched/",
        RoleBySlot = new Dictionary<string, string>
        {
            ["consignor"] = "EX",
            ["consignee"] = "CN",
            ["delivery"] = "DP",
            ["despatch"] = "PW",
            ["customsTransitAgent"] = "CB",
        },
        OperatorScheme = new Scheme("operator_internal_activity_id", "Operator internal activity ID"),
        NoteSubjectList = "ched_note_subject_code",
        CommodityNoteSubjectList = "ched_commodity_note_subject_code",
        DeclarationClauses = new Scheme("ched_consignment_clause", "CHED consignment's clauses"),
        ClearanceClauses = new Scheme("ched_decision_clause", "CHED decision's clauses"),
        ClearanceTypeName = "Clearance (Official inspector)",
        // Every retrieved CHED carries a supporting document, so one is fabricated when a fixture omits it.
        DefaultSupportingDocument = new ReferencedDocumentModel
        {
            DocumentTypeCode = "636",
            RelationshipTypeCode = "ZZZ",
            Identifier = "SIMULATOR-SUPPORTING-DOC",
            IssuingCountry = "GB",
        },
        IdPrefix = model => $"CHED{ChedTypeOf(model)}",
        DocumentName = (model, codeLists) => codeLists.Get("ched_type", ChedTypeOf(model)).Name,
    };

    public static readonly CertificateKind Intra = new()
    {
        Name = "INTRA",
        DocumentTypeCode = "856",
        DocumentTypeName = "Inspection certificate (EU Intra Certificate)",
        UrlPrefix = "https://webgate.acceptance.ec.europa.eu/tracesnt/certificate/eu-intra/",
        RoleBySlot = new Dictionary<string, string>
        {
            ["consignor"] = "CZ",
            ["consignee"] = "CN",
            ["delivery"] = "DP",
            ["despatch"] = "PW",
            ["customsTransitAgent"] = "CB",
        },
        OperatorScheme = new Scheme("operator_activity_id", "Operator activity ID"),
        NoteSubjectList = "eu_intra_note_subject_code",
        // Not seen in any sample; named by analogy with the document-level list.
        CommodityNoteSubjectList = "eu_intra_commodity_note_subject_code",
        DeclarationClauses = new Scheme("eu_intra_consignment_clause", "EU Intra consignment's clauses"),
        ClearanceClauses = new Scheme("eu_intra_certification_clause", "EU Intra certification's clauses"),
        ClearanceTypeName = "Clearance (Certification by certifying officer)",
        IdPrefix = _ => "INTRA",
        DocumentName = (model, _) =>
            model.ExchangedDocument.Name
            ?? throw new UnknownCodeException(
                "exchangedDocument.name must be set for an INTRA — it is the certificate model, e.g. "
                    + "'64/432 (2016/2008) F1 Bovine'."
            ),
    };

    /// <summary>As the control API's messages name it.</summary>
    public required string Name { get; init; }

    public required string DocumentTypeCode { get; init; }

    public required string DocumentTypeName { get; init; }

    /// <summary>Where the certificate's self-reference points; the ID is appended.</summary>
    public required string UrlPrefix { get; init; }

    /// <summary>
    /// Party role is fixed by the slot the operator sits in, not by the operator — TRACES overwrites
    /// whatever a submission sends here, so the simulator does the same.
    /// </summary>
    public required IReadOnlyDictionary<string, string> RoleBySlot { get; init; }

    public required Scheme OperatorScheme { get; init; }

    public required string NoteSubjectList { get; init; }

    public required string CommodityNoteSubjectList { get; init; }

    public required Scheme DeclarationClauses { get; init; }

    public required Scheme ClearanceClauses { get; init; }

    public required string ClearanceTypeName { get; init; }

    public ReferencedDocumentModel? DefaultSupportingDocument { get; init; }

    /// <summary>The part of a generated ID before the country — <c>CHEDA</c>, <c>INTRA</c>.</summary>
    public required Func<CertificateControlModel, string> IdPrefix { get; init; }

    /// <summary>
    /// A CHED's name is its type's display name, derived. An INTRA's is its certificate model, which no
    /// note carries, so the submitter states it.
    /// </summary>
    public required Func<CertificateControlModel, CodeLists, string> DocumentName { get; init; }

    /// <summary>The CHED type, which lives in the mandatory <c>CHED_TYPE</c> note rather than a field of its own.</summary>
    public static string ChedTypeOf(CertificateControlModel model) =>
        model.ExchangedDocument.IncludedNote.TryGetValue(SpsCertificateBuilder.ChedTypeNoteSubject, out var type)
            ? type
            : throw new UnknownCodeException(
                "exchangedDocument.includedNote must contain CHED_TYPE — it is what makes this a CHED-A rather than a CHED-P."
            );
}

/// <summary>A code list or identifier scheme as it appears on the wire: its ID and its display name.</summary>
public record Scheme(string Id, string Name);
