using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Control.Mapping;

/// <summary>
/// Builds the TRACES certificate the SOAP face serves. Everything is copied from the control model or
/// derived the way TRACES derives it; nothing is invented on the client's behalf. Deriving is not
/// cosmetic — Trade Gateway copies every <c>name=</c> and never looks one up, so a code written
/// without its display name reaches a consumer as a blank.
/// </summary>
public class ChedCertificateBuilder(CodeLists codeLists, Registry<OperatorEntry> operators, Registry<AuthorityEntry> authorities)
{
    private const string ChedUrlPrefix = "https://webgate.acceptance.ec.europa.eu/tracesnt/certificate/ched/";

    private const string TracesAgencyId = "ec_sante_traces";
    private const string TracesAgencyName = "European commission - DG SANTE - Traces";

    /// <summary>Every CHED is UNECE document type 636.</summary>
    private const string ChedDocumentTypeCode = "636";

    /// <summary>Note subjects the simulator reads back as well as writes.</summary>
    public const string ChedTypeNoteSubject = "CHED_TYPE";
    public const string LastUpdateNoteSubject = "LAST_UPDATE_DATETIME";

    /// <summary>
    /// Party role is fixed by the slot the operator sits in, not by the operator — TRACES overwrites
    /// whatever a submission sends here, so the simulator does the same.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> s_roleBySlot = new Dictionary<string, string>
    {
        ["consignor"] = "EX",
        ["consignee"] = "CN",
        ["delivery"] = "DP",
        ["customsTransitAgent"] = "CB",
    };

    public SPSCertificateType Build(ChedControlModel model, string id) =>
        new()
        {
            SPSExchangedDocument = BuildDocument(model, id),
            SPSConsignment = BuildConsignment(model.SpecifiedConsignment),
        };

    /// <summary>The CHED type, which lives in the mandatory <c>CHED_TYPE</c> note rather than a field of its own.</summary>
    public static string ChedTypeOf(ChedControlModel model) =>
        model.ExchangedDocument.IncludedNote.TryGetValue(ChedTypeNoteSubject, out var type)
            ? type
            : throw new UnknownCodeException(
                "exchangedDocument.includedNote must contain CHED_TYPE — it is what makes this a CHED-A rather than a CHED-P."
            );

    private SPSExchangedDocumentType BuildDocument(ChedControlModel model, string id)
    {
        var chedType = ChedTypeOf(model);
        var (statusCode, status) = codeLists.Resolve("status_code", model.Status ?? "NEW");

        var document = new SPSExchangedDocumentType
        {
            // The document's own name is the CHED type's display name — derived, never submitted.
            Name = [Text(codeLists.Get("ched_type", chedType).Name, "en")],
            ID = new IDType { Value = id },
            TypeCode = new DocumentCodeType
            {
                Value = XmlEnums.Parse<DocumentNameCodeContentType>(ChedDocumentTypeCode),
                name = "Health certificate (CHED - Common Health Entry Document)",
            },
            StatusCode = new StatusCodeType
            {
                Value = XmlEnums.Parse<StatusCodeContentType>(statusCode),
                name = status.Name,
            },
            IssueDateTime = DateTime(DateTimeOffset.UtcNow),
            IssuerSPSParty = BuildIssuer(model.SpecifiedConsignment.UnloadingBaseportLocation?.Identifier),
            IncludedSPSNote = [.. BuildNotes(model.ExchangedDocument.IncludedNote, "ched_note_subject_code")],
            ReferenceSPSReferencedDocument =
            [
                SelfReference(id),
                .. (model.ExchangedDocument.ReferenceDocument ?? [DefaultSupportingDocument]).Select(
                    SupportingDocument
                ),
            ],
            SignatorySPSAuthentication =
            [
                .. Authentications(model.ExchangedDocument, model.SpecifiedConsignment.UnloadingBaseportLocation?.Identifier),
            ],
        };

        // TRACES stamps every retrieved document with when it last changed.
        document.IncludedSPSNote =
        [
            .. document.IncludedSPSNote,
            Note(LastUpdateNoteSubject, DateTimeOffset.UtcNow.ToString("O"), "ched_note_subject_code"),
        ];

        return document;
    }

    /// <summary>
    /// The default TRACES itself never sends but every retrieved CHED carries. Deliberately a
    /// stand-in so a fixture need not spell out a supporting document to look realistic.
    /// </summary>
    private static ReferencedDocumentModel DefaultSupportingDocument =>
        new()
        {
            DocumentTypeCode = ChedDocumentTypeCode,
            RelationshipTypeCode = "ZZZ",
            Identifier = "SIMULATOR-SUPPORTING-DOC",
            IssuingCountry = "GB",
        };

    /// <summary>
    /// The CHED pointing at itself. Retrieval-only: submissions never carry it, so it is built rather
    /// than copied, URL included.
    /// </summary>
    private SPSReferencedDocumentType SelfReference(string id) =>
        new()
        {
            TypeCode = DocumentType(ChedDocumentTypeCode),
            RelationshipTypeCode = RelationshipType("CAW"),
            ID = new IDType { Value = id },
            AttachmentBinaryObject =
            [
                new BinaryObjectType
                {
                    format = "url",
                    mimeCode = "text/url",
                    uri = ChedUrlPrefix + id,
                },
            ],
        };

    private SPSReferencedDocumentType SupportingDocument(ReferencedDocumentModel model) =>
        new()
        {
            TypeCode = DocumentType(model.DocumentTypeCode ?? ChedDocumentTypeCode),
            RelationshipTypeCode = RelationshipType(model.RelationshipTypeCode ?? "ZZZ"),
            ID = new IDType { Value = model.Identifier, schemeAgencyID = model.IssuingCountry },
        };

    private DocumentCodeType DocumentType(string code) =>
        new()
        {
            Value = XmlEnums.Parse<DocumentNameCodeContentType>(code),
            name = codeLists.Get("referenced_document_type", code).Name,
        };

    private ReferenceCodeType RelationshipType(string code) =>
        new()
        {
            Value = XmlEnums.Parse<ReferenceTypeCodeContentType>(code),
            name = codeLists.Get("referenced_document_relationship", code).Name,
        };

    /// <summary>
    /// The applicant's declaration and, once decided, the inspector's clearance. Keeping them as two
    /// named blocks on the model is what lets a decision be applied without restating the certificate.
    /// </summary>
    private IEnumerable<SPSAuthenticationType> Authentications(ExchangedDocumentModel document, string? authorityId)
    {
        if (document.Declaration is { } declaration)
        {
            yield return Authentication(declaration, "4", "Inspection (Identification of Applicant)", "ched_consignment_clause", authorityId);
        }

        if (document.Clearance is { } clearance)
        {
            yield return Authentication(clearance, "1", "Clearance (Official inspector)", "ched_decision_clause", authorityId);
        }
    }

    private SPSAuthenticationType Authentication(
        AuthenticationModel model,
        string typeCode,
        string typeName,
        string clauseList,
        string? authorityId
    ) =>
        new()
        {
            TypeCode = new GovernmentActionCodeType
            {
                Value = XmlEnums.Parse<GovernmentActionCodeContentType>(typeCode),
                name = typeName,
            },
            ActualDateTime = DateTime(model.ActualDateTime ?? DateTimeOffset.UtcNow),
            // The signatory's identity is resolved from the authenticated user, so it is derived.
            ProviderSPSParty = BuildIssuer(authorityId) ?? new SPSPartyType { Name = Text("") },
            IncludedSPSClause = [.. model.IncludedClause.Select(clause => Clause(clause.Key, clause.Value, clauseList))],
        };

    /// <summary>
    /// A clause is its ID, the code, and the code's display text. The second <c>Content</c> is
    /// TRACES's — submissions send one, retrieved documents carry two.
    /// </summary>
    private SPSClauseType Clause(string id, string content, string clauseList) =>
        new()
        {
            ID = new IDType
            {
                Value = id,
                schemeID = clauseList,
                schemeName = clauseList == "ched_decision_clause" ? "CHED decision's clauses" : "CHED consignment's clauses",
                schemeAgencyID = TracesAgencyId,
                schemeAgencyName = TracesAgencyName,
            },
            Content = codeLists.TryGet(clauseList, content, out var entry)
                ? [Text(content), Text(entry.Name, "en")]
                // Free text, such as a signatory's email address: no display twin to add.
                : [Text(content)],
        };

    /// <summary>
    /// The issuing authority. It appears in every retrieved CHED and in no submission at all, so
    /// there is nowhere to copy it from — it is resolved from the border control post.
    /// </summary>
    private SPSPartyType? BuildIssuer(string? borderControlPost)
    {
        if (borderControlPost is null || !authorities.TryGet(borderControlPost, out var authority))
        {
            return null;
        }

        return new SPSPartyType
        {
            ID = new IDType
            {
                Value = authority.ActivityId,
                schemeID = "authority_activity_id",
                schemeName = "Authority activity ID",
                schemeAgencyID = TracesAgencyId,
                schemeAgencyName = TracesAgencyName,
            },
            Name = Text(authority.Name),
            RoleCode = new PartyRoleCodeType
            {
                Value = XmlEnums.Parse<PartyRoleCodeContentType>("VJ"),
                name = "Officer (Identification of Applicant)",
            },
            TypeCode =
            [
                new CodeType
                {
                    Value = "AUTHORITY",
                    listID = "user_body_role",
                    listName = "User body role",
                    name = codeLists.Get("user_body_role", "AUTHORITY").Name,
                },
            ],
            SpecifiedSPSAddress = authority.PostalAddress is { } address
                ? Address(
                    new AddressModel
                    {
                        CountryId = address.CountryId,
                        PostcodeCode = address.PostcodeCode,
                        LineOne = address.LineOne,
                        CityName = address.CityName,
                    }
                )
                : null,
            SpecifiedSPSPerson = authority.OfficerName is null
                ? null
                : new SPSPersonType { Name = Text(authority.OfficerName) },
        };
    }

    private SPSConsignmentType BuildConsignment(ConsignmentModel model) =>
        new()
        {
            AvailabilityDueDateTime = model.AvailabilityDueDateTime is { } due ? DateTime(due) : null,
            ExportSPSCountry = Country(model.ExportCountry),
            ImportSPSCountry = Country(model.ImportCountry),
            ConsignorSPSParty = Party(model.ConsignorParty, "consignor"),
            ConsigneeSPSParty = Party(model.ConsigneeParty, "consignee"),
            DeliverySPSParty = Party(model.DeliveryParty, "delivery"),
            CustomsTransitAgentSPSParty = Party(model.CustomsTransitAgentParty, "customsTransitAgent"),
            UnloadingBaseportSPSLocation = Baseport(model.UnloadingBaseportLocation),
            MainCarriageSPSTransportMovement = Transport(model.MainCarriageLogisticsTransportMovement),
            IncludedSPSConsignmentItem = ConsignmentItems(model.IncludedConsignmentItem),
        };

    /// <summary>
    /// A party is the identifier and address the client sent, plus the name, role and activity codes
    /// TRACES resolves. Sending a name instead of an identifier is how TRACES models an operator
    /// created on the fly, and it is the way past the registry when a test needs an unknown operator.
    /// </summary>
    private SPSPartyType? Party(PartyModel? model, string slot)
    {
        if (model is null)
        {
            return null;
        }

        var registered = model.Identifier is not null && model.Name is null
            ? operators.Get(model.Identifier)
            : null;

        return new SPSPartyType
        {
            ID = model.Identifier is null
                ? null
                : new IDType
                {
                    Value = model.Identifier,
                    schemeID = model.SchemeId ?? "operator_internal_activity_id",
                    schemeName = "Operator internal activity ID",
                    schemeAgencyID = TracesAgencyId,
                    schemeAgencyName = TracesAgencyName,
                },
            Name = Text(model.Name ?? registered?.Name ?? ""),
            RoleCode = s_roleBySlot.TryGetValue(slot, out var role)
                ? new PartyRoleCodeType
                {
                    Value = XmlEnums.Parse<PartyRoleCodeContentType>(role),
                    name = codeLists.Get("party_role", role).Name,
                }
                : null,
            TypeCode = [.. ActivityCodes(registered)],
            SpecifiedSPSAddress = Address(model.PostalAddress),
        };
    }

    private IEnumerable<CodeType> ActivityCodes(OperatorEntry? registered)
    {
        if (registered?.ActivityType is { } activity)
        {
            yield return new CodeType
            {
                Value = activity,
                listID = "operator_activity_type",
                listName = "Operator activity type",
                name = codeLists.Get("operator_activity_type", activity).Name,
            };
        }

        if (registered?.ClassificationSection is { } section)
        {
            yield return new CodeType
            {
                Value = section,
                listID = "classification_section_code",
                listName = "Operator classification section code",
                name = codeLists.Get("classification_section_code", section).Name,
            };
        }
    }

    private SPSAddressType? Address(AddressModel? model) =>
        model is null
            ? null
            : new SPSAddressType
            {
                PostcodeCode = model.PostcodeCode is null ? null : new CodeType { Value = model.PostcodeCode },
                LineOne = model.LineOne is null ? null : Text(model.LineOne),
                CityName = model.CityName is null ? null : Text(model.CityName, "en"),
                CountryID = model.CountryId is null ? null : new IDType { Value = model.CountryId },
                CountryName = model.CountryId is null ? null : Text(codeLists.Get("country", model.CountryId).Name, "en"),
                CountrySubDivisionName = model.CountrySubDivisionName is null
                    ? null
                    : Text(model.CountrySubDivisionName, "en"),
            };

    private SPSCountryType? Country(string? code) =>
        code is null
            ? null
            : new SPSCountryType
            {
                ID = new IDType { Value = code },
                Name = [Text(codeLists.Get("country", code).Name, "en")],
            };

    /// <summary>
    /// The border control post: the client sends the code and the country of entry, and the other
    /// five names come from the authority registry. Their order is the only thing that distinguishes
    /// them on the wire, so they are written in the order TRACES uses.
    /// </summary>
    private SPSLocationType? Baseport(LocationModel? model)
    {
        if (model?.Identifier is null)
        {
            return null;
        }

        var authority = authorities.Get(model.Identifier);

        return new SPSLocationType
        {
            ID = new IDType
            {
                Value = model.Identifier,
                schemeID = model.SchemeId ?? "un_locode",
                schemeName = "UN/LOCODE",
                schemeAgencyID = "un",
                schemeAgencyName = "United Nations",
                schemeDataURI = "authority_activity",
            },
            Name =
            [
                Text(model.CountryId ?? ""),
                Text(authority.City ?? "", "en"),
                Text(authority.ActivityId ?? ""),
                Text(authority.Name),
                Text(authority.Address ?? ""),
                Text(authority.UnLocode ?? ""),
            ],
        };
    }

    private SPSTransportMovementType[]? Transport(TransportMovementModel? model) =>
        model is null
            ? null
            :
            [
                new SPSTransportMovementType
                {
                    ID = model.Identifier is null
                        ? null
                        : new IDType
                        {
                            Value = model.Identifier,
                            schemeID = model.SchemeId,
                            schemeAgencyID = model.SchemeAgencyId,
                        },
                    ModeCode = model.ModeCode is null
                        ? null
                        : new TransportModeCodeType
                        {
                            Value = XmlEnums.Parse<TransportModeCodeContentType>(model.ModeCode),
                            name = codeLists.Get("transport_mode", model.ModeCode).Name,
                        },
                },
            ];

    /// <summary>
    /// TRACES keeps a sequence-0 "totals and summary" line ahead of the real commodities. The client
    /// supplies the totals; the fixed description and the numbering are the simulator's.
    /// </summary>
    private SPSConsignmentItemType[]? ConsignmentItems(ConsignmentItemModel? model)
    {
        if (model is null)
        {
            return null;
        }

        var lines = new List<SPSTradeLineItemType>();

        if (model.ConsignmentTotals is { } totals)
        {
            var summary = TradeLine(totals, 0);
            summary.Description = [Text("Consignment totals and summary")];
            lines.Add(summary);
        }

        lines.AddRange(model.IncludedTradeLineItem.Select((line, index) => TradeLine(line, index + 1)));

        return
        [
            new SPSConsignmentItemType
            {
                NatureIdentificationSPSCargo = model.NatureIdCargo is null
                    ? null
                    :
                    [
                        new SPSCargoType
                        {
                            TypeCode = new CargoTypeClassificationCodeType
                            {
                                Value = XmlEnums.Parse<CargoTypeClassificationCodeContentType>(model.NatureIdCargo),
                                name = codeLists.Get("cargo_type", model.NatureIdCargo).Name,
                            },
                        },
                    ],
                IncludedSPSTradeLineItem = [.. lines],
            },
        ];
    }

    private SPSTradeLineItemType TradeLine(TradeLineItemModel model, int sequence) =>
        new()
        {
            SequenceNumeric = new NumericType { Value = sequence },
            ScientificName = model.ScientificName is null ? null : [Text(model.ScientificName, "la")],
            NetWeightMeasure = Measure(model.NetWeight),
            GrossWeightMeasure = Measure(model.GrossWeight),
            NetVolumeMeasure = Measure(model.NetVolume),
            ApplicableSPSClassification = [.. model.ApplicableClassification.Select(Classification)],
            OriginSPSCountry = Country(model.OriginCountry) is { } origin ? [origin] : null,
            PhysicalSPSPackage = Package(model.PhysicalReferencedLogisticsPackage),
            AdditionalInformationSPSNote =
            [
                .. BuildNotes(model.AdditionalInformationNote, "ched_commodity_note_subject_code"),
            ],
        };

    /// <summary>
    /// A class code expands to the whole description path — a CN code can carry four levels of it,
    /// which is why a client could not supply this even if it wanted to.
    /// </summary>
    private SPSClassificationType Classification(KeyValuePair<string, string> classification)
    {
        var (system, code) = classification;
        var entry = codeLists.Get(ListFor(system), code);
        var names = entry.Names ?? [new LocalisedName(entry.Name)];

        return new SPSClassificationType
        {
            SystemID = new IDType { Value = system },
            SystemName = [Text(codeLists.Get("classification_system", system).Name)],
            ClassCode = new CodeType { Value = code },
            ClassName = [.. names.Select(name => Text(name.Value, name.LanguageId ?? "en"))],
        };
    }

    /// <summary>Classification systems each have their own code list — <c>CN</c> reads from <c>cn_code</c>.</summary>
    private static string ListFor(string system) =>
        system switch
        {
            "CN" => "cn_code",
            _ => $"classification_{system.ToLowerInvariant()}",
        };

    private IEnumerable<SPSNoteType> BuildNotes(IReadOnlyDictionary<string, string> notes, string subjectList) =>
        notes.Select(note => Note(note.Key, note.Value, subjectList));

    /// <summary>
    /// Whether a note's value is a code or free text is a property of its subject, not of the caller
    /// — <c>CHED_TYPE</c> carries a code, <c>LAST_UPDATE_DATETIME</c> carries a timestamp.
    /// </summary>
    private SPSNoteType Note(string subject, string value, string subjectList)
    {
        var subjectEntry = codeLists.Get(subjectList, subject);

        var note = new SPSNoteType
        {
            Content = [Text("")],
            SubjectCode = new CodeType
            {
                Value = subject,
                listID = subjectList,
                listName = subjectEntry.ListName,
                name = subjectEntry.Name,
            },
        };

        if (subjectEntry.ContentCodeList is { } contentList)
        {
            var content = codeLists.Get(contentList, value);
            note.ContentCode =
            [
                new CodeType
                {
                    Value = value,
                    listID = contentList,
                    listName = content.ListName,
                    name = content.Name,
                },
            ];
        }
        else
        {
            note.Content = [Text(value)];
        }

        return note;
    }

    private SPSPackageType[]? Package(PackageModel? model) =>
        model is null
            ? null
            :
            [
                new SPSPackageType
                {
                    LevelCode = new CodeType { Value = "4", name = "No packaging hierarchy" },
                    TypeCode = model.TypeCode is null
                        ? null
                        : new PackageTypeCodeType
                        {
                            Value = XmlEnums.Parse<PackageTypeCodeContentType>(model.TypeCode),
                            name = DisplayName(codeLists.Get("package_type", model.TypeCode)),
                        },
                    ItemQuantity = model.ItemQuantity is null
                        ? null
                        : new QuantityType { Value = model.ItemQuantity.Value },
                },
            ];

    private static MeasureType? Measure(MeasureModel? model) =>
        model is null ? null : new MeasureType { Value = model.Value, unitCode = model.UnitCode };

    private static TextType Text(string value, string? language = null) =>
        new() { Value = value, languageID = language };

    /// <summary>
    /// Some codes have no display label at all — package type <c>NA</c> is written by TRACES with no
    /// <c>name</c> attribute. An empty seed entry means exactly that, so it is omitted rather than
    /// written as <c>name=""</c>.
    /// </summary>
    private static string? DisplayName(CodeEntry entry) => string.IsNullOrEmpty(entry.Name) ? null : entry.Name;

    private static DateTimeType DateTime(DateTimeOffset value) => new() { Item = value.DateTime };
}
