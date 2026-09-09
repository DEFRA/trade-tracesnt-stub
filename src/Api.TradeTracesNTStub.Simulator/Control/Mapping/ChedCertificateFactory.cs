using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Control.Templates;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Control.Mapping;

/// <summary>
/// Turns a control model into the TRACES certificate the SOAP face serves.
/// </summary>
/// <remarks>
/// Two things happen here, and the second is the one that matters. First the overrides a test
/// supplied are layered onto a baseline template. Then every code written is given the display name
/// TRACES would have looked up — because Trade Gateway copies those names straight through and never
/// derives them, a certificate without them maps to a model full of blanks.
/// </remarks>
public class ChedCertificateFactory(CodeLists codeLists)
{
    /// <summary>UNECE government action code 1 — the official inspector's clearance.</summary>
    private static readonly GovernmentActionCodeContentType ClearanceAction =
        XmlEnums.Parse<GovernmentActionCodeContentType>("1");

    private const string ChedUrlPrefix = "https://webgate.acceptance.ec.europa.eu/tracesnt/certificate/ched/";

    public SPSCertificateType Create(ChedControlModel model, string id)
    {
        var certificate = ChedTemplates.Load(model.Template ?? TemplateFor(model.Type));

        ApplyId(certificate, id);
        ApplyStatus(certificate, model.Status);
        ApplyParties(certificate, model);
        ApplyBorderControlPost(certificate, model.BorderControlPost);
        ApplyCommodities(certificate, model.Commodities);
        ApplyDecision(certificate, model.Decision);

        return certificate;
    }

    private static string TemplateFor(ChedType type) => $"CHED{type}";

    /// <summary>
    /// The ID appears twice: as the document's own ID and inside the self-referencing "CAW" document
    /// reference, whose URL embeds it too. Missing the second is invisible until a consumer follows it.
    /// </summary>
    private static void ApplyId(SPSCertificateType certificate, string id)
    {
        var document = certificate.SPSExchangedDocument;
        document.ID = new IDType { Value = id };

        // "CAW" is the self-reference: the document pointing at itself.
        var self = document.ReferenceSPSReferencedDocument?.FirstOrDefault(reference =>
            reference.RelationshipTypeCode?.Value == XmlEnums.Parse<ReferenceTypeCodeContentType>("CAW")
        );

        if (self is null)
        {
            return;
        }

        self.ID = new IDType { Value = id };

        foreach (var attachment in self.AttachmentBinaryObject ?? [])
        {
            attachment.uri = ChedUrlPrefix + id;
        }
    }

    private void ApplyStatus(SPSCertificateType certificate, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return;
        }

        var (code, entry) = codeLists.Resolve("status_code", status);

        certificate.SPSExchangedDocument.StatusCode = new StatusCodeType
        {
            Value = XmlEnums.Parse<StatusCodeContentType>(code),
            name = entry.Name,
        };
    }

    private void ApplyParties(SPSCertificateType certificate, ChedControlModel model)
    {
        var consignment = certificate.SPSConsignment;

        ApplyParty(consignment.ConsignorSPSParty, model.Consignor);
        ApplyParty(consignment.ConsigneeSPSParty, model.Consignee);
        ApplyParty(consignment.DeliverySPSParty, model.DeliveryParty);
    }

    /// <summary>
    /// Overwrites only what the test named, so the template's role and activity codes — which carry
    /// their own display names — survive untouched.
    /// </summary>
    private void ApplyParty(SPSPartyType? party, PartyModel? model)
    {
        if (party is null || model is null)
        {
            return;
        }

        if (model.OperatorId is not null)
        {
            // Keep the template's scheme attributes; only the value changes.
            party.ID ??= new IDType();
            party.ID.Value = model.OperatorId;
        }

        if (model.Name is not null)
        {
            party.Name = new TextType { Value = model.Name };
        }

        if (model.CountryCode is not null && party.SpecifiedSPSAddress is not null)
        {
            var country = codeLists.Get("country", model.CountryCode);
            party.SpecifiedSPSAddress.CountryID = new IDType { Value = model.CountryCode };
            party.SpecifiedSPSAddress.CountryName = new TextType { Value = country.Name, languageID = "en" };
        }
    }

    /// <summary>
    /// The BCP is an ID plus six unlabelled <c>Name</c> elements whose order carries the meaning —
    /// country, region, activity code, port name, address, UN/LOCODE. There is no attribute to
    /// disambiguate them, so the registry stores them in order and they are written in order.
    /// </summary>
    private void ApplyBorderControlPost(SPSCertificateType certificate, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var location = certificate.SPSConsignment.UnloadingBaseportSPSLocation;

        if (location is null)
        {
            return;
        }

        var entry = codeLists.Get("border_control_post", code);
        var names = entry.Names ?? [new LocalisedName(entry.Name)];

        location.ID ??= new IDType();
        location.ID.Value = code;
        location.Name =
        [
            .. names.Select(name => new TextType { Value = name.Value, languageID = name.LanguageId }),
        ];
    }

    /// <summary>
    /// Replaces the template's commodities outright. TRACES keeps a sequence-0 "totals and summary"
    /// line ahead of the real ones, so that line is preserved and the supplied commodities are
    /// numbered from 1.
    /// </summary>
    private void ApplyCommodities(SPSCertificateType certificate, IReadOnlyList<CommodityModel>? commodities)
    {
        if (commodities is null)
        {
            return;
        }

        var item = certificate.SPSConsignment.IncludedSPSConsignmentItem?.FirstOrDefault();

        if (item is null)
        {
            return;
        }

        var summary = item.IncludedSPSTradeLineItem?.FirstOrDefault(line => line.SequenceNumeric?.Value == 0);
        var template = item.IncludedSPSTradeLineItem?.FirstOrDefault(line => line.SequenceNumeric?.Value != 0);

        var lines = commodities
            .Select((commodity, index) => BuildLine(commodity, index + 1, template))
            .ToList();

        item.IncludedSPSTradeLineItem = summary is null ? [.. lines] : [summary, .. lines];
    }

    private SPSTradeLineItemType BuildLine(CommodityModel commodity, int sequence, SPSTradeLineItemType? template)
    {
        var line = new SPSTradeLineItemType { SequenceNumeric = new NumericType { Value = sequence } };

        if (template is not null)
        {
            // Carry over the shapes a test does not name — identification system, product type,
            // commodity notes — so they stay realistic instead of coming back empty. The CN
            // classification is left behind deliberately; ApplyCnCode owns it.
            line.ApplicableSPSClassification =
            [
                .. template.ApplicableSPSClassification?.Where(c => c.SystemID?.Value != "CN") ?? [],
            ];
            line.AdditionalInformationSPSNote = template.AdditionalInformationSPSNote;
            line.NetVolumeMeasure = template.NetVolumeMeasure;
            line.PhysicalSPSPackage = template.PhysicalSPSPackage;
        }

        if (commodity.Description is not null)
        {
            line.Description = [new TextType { Value = commodity.Description }];
        }

        if (commodity.ScientificName is not null)
        {
            line.ScientificName = [new TextType { Value = commodity.ScientificName, languageID = "la" }];
        }

        if (commodity.NetWeightKg is not null)
        {
            line.NetWeightMeasure = new MeasureType { Value = commodity.NetWeightKg.Value, unitCode = "KGM" };
        }

        if (commodity.GrossWeightKg is not null)
        {
            line.GrossWeightMeasure = new MeasureType { Value = commodity.GrossWeightKg.Value, unitCode = "KGM" };
        }

        ApplyCnCode(line, commodity.CnCode);
        ApplyOriginCountry(line, commodity.OriginCountry);
        ApplyPackaging(line, commodity);

        return line;
    }

    /// <summary>
    /// A CN code expands to its whole description hierarchy — chapter, then each level down. The
    /// gateway renders the list as-is, so all levels are written, not just the leaf.
    /// </summary>
    private void ApplyCnCode(SPSTradeLineItemType line, string? cnCode)
    {
        if (cnCode is null)
        {
            return;
        }

        var entry = codeLists.Get("cn_code", cnCode);
        var names = entry.Names ?? [new LocalisedName(entry.Name)];

        var classification = new SPSClassificationType
        {
            SystemID = new IDType { Value = "CN" },
            SystemName = [new TextType { Value = "CN Code (Combined Nomenclature)" }],
            ClassCode = new CodeType { Value = cnCode },
            ClassName =
            [
                .. names.Select(name => new TextType { Value = name.Value, languageID = name.LanguageId ?? "en" }),
            ],
        };

        var others =
            line.ApplicableSPSClassification?.Where(existing => existing.SystemID?.Value != "CN") ?? [];

        line.ApplicableSPSClassification = [classification, .. others];
    }

    private void ApplyOriginCountry(SPSTradeLineItemType line, string? countryCode)
    {
        if (countryCode is null)
        {
            return;
        }

        var country = codeLists.Get("country", countryCode);

        line.OriginSPSCountry =
        [
            new SPSCountryType
            {
                ID = new IDType { Value = countryCode },
                Name = [new TextType { Value = country.Name, languageID = "en" }],
            },
        ];
    }

    private void ApplyPackaging(SPSTradeLineItemType line, CommodityModel commodity)
    {
        if (commodity.PackageType is null && commodity.PackageCount is null)
        {
            return;
        }

        var package = line.PhysicalSPSPackage?.FirstOrDefault() ?? new SPSPackageType();

        if (commodity.PackageType is not null)
        {
            var entry = codeLists.Get("package_type", commodity.PackageType);
            package.TypeCode = new PackageTypeCodeType
            {
                Value = XmlEnums.Parse<PackageTypeCodeContentType>(commodity.PackageType),
                name = entry.Name,
            };
        }

        if (commodity.PackageCount is not null)
        {
            package.ItemQuantity = new QuantityType { Value = commodity.PackageCount.Value };
        }

        line.PhysicalSPSPackage = [package];
    }

    /// <summary>
    /// A decision is a second signatory authentication block — the official inspector's — alongside
    /// the applicant's declaration. Absent means the CHED has not been decided, so the block is left off.
    /// </summary>
    private void ApplyDecision(SPSCertificateType certificate, DecisionModel? decision)
    {
        if (decision is null)
        {
            return;
        }

        var document = certificate.SPSExchangedDocument;

        var clauses = new List<SPSClauseType> { Clause("DECISION_CONCLUSION", decision.Conclusion) };

        AddCheck(clauses, "DOCUMENTARY_CHECK", decision.DocumentaryCheck);
        AddCheck(clauses, "IDENTITY_CHECK", decision.IdentityCheck);
        AddCheck(clauses, "PHYSICAL_CHECK", decision.PhysicalCheck);

        var clearance = new SPSAuthenticationType
        {
            TypeCode = new GovernmentActionCodeType
            {
                Value = ClearanceAction,
                name = "Clearance (Official inspector)",
            },
            ActualDateTime = new DateTimeType { Item = DateTime.UtcNow },
            ProviderSPSParty = new SPSPartyType { Name = new TextType { Value = "" } },
            IncludedSPSClause = [.. clauses],
        };

        var declarations =
            document.SignatorySPSAuthentication?.Where(authentication =>
                authentication.TypeCode?.Value != ClearanceAction
            ) ?? [];

        document.SignatorySPSAuthentication = [.. declarations, clearance];

        ApplyRefusalNotes(document, decision);
    }

    /// <summary>
    /// A check contributes two clauses: whether it happened, and its result. TRACES writes them as a
    /// pair and the gateway reads them as a pair.
    /// </summary>
    private void AddCheck(List<SPSClauseType> clauses, string id, string? result)
    {
        if (result is null)
        {
            return;
        }

        clauses.Add(Clause(id, "YES"));
        clauses.Add(Clause($"{id}_RESULT", result));
    }

    private SPSClauseType Clause(string id, string content)
    {
        var entry = codeLists.Get("ched_decision_clause", content);

        return new SPSClauseType
        {
            ID = new IDType
            {
                Value = id,
                schemeID = "ched_decision_clause",
                schemeName = "CHED decision's clauses",
                schemeAgencyID = "ec_sante_traces",
                schemeAgencyName = "European commission - DG SANTE - Traces",
            },
            Content =
            [
                new TextType { Value = content },
                new TextType { Value = entry.Name, languageID = "en" },
            ],
        };
    }

    /// <summary>Refusal reasons and the measure taken ride as document notes, not decision clauses.</summary>
    private static void ApplyRefusalNotes(SPSExchangedDocumentType document, DecisionModel decision)
    {
        var notes = document.IncludedSPSNote?.ToList() ?? [];

        if (decision.NotAcceptableMeasure is not null)
        {
            notes.Add(
                new SPSNoteType
                {
                    ContentCode =
                    [
                        new CodeType
                        {
                            Value = decision.NotAcceptableMeasure,
                            listID = "ched_not_acceptable_measure",
                        },
                    ],
                    Content = [new TextType { Value = "" }],
                    SubjectCode = new CodeType { Value = "NOT_ACCEPTABLE_MEASURE" },
                }
            );
        }

        foreach (var reason in decision.RefusalReasons ?? [])
        {
            notes.Add(
                new SPSNoteType
                {
                    ContentCode = [new CodeType { Value = reason, listID = "refusal_reason" }],
                    Content = [new TextType { Value = "" }],
                    SubjectCode = new CodeType { Value = "REFUSAL_REASON" },
                }
            );
        }

        document.IncludedSPSNote = [.. notes];
    }
}
