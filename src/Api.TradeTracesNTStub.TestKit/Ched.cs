using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace Api.TradeTracesNTStub.TestKit;

/// <summary>
/// Fluent construction of a CHED fixture, so it reads as the scenario rather than as a JSON literal.
/// Adds no defaults beyond the CHED type note, without which nothing can be built.
/// </summary>
public static class Ched
{
    public static ChedBuilder ChedA() => OfType("A");

    public static ChedBuilder ChedP() => OfType("P");

    public static ChedBuilder ChedPP() => OfType("PP");

    public static ChedBuilder ChedD() => OfType("D");

    public static ChedBuilder OfType(string chedType) => new(chedType);
}

public class ChedBuilder
{
    private readonly Dictionary<string, string> _notes;
    private ChedControlModel _model;

    internal ChedBuilder(string chedType)
    {
        // The type is a note, not a field — the same place TRACES keeps it.
        _notes = new Dictionary<string, string> { ["CHED_TYPE"] = chedType };
        _model = new ChedControlModel();
    }

    public ChedBuilder WithId(string id) => Set(model => model with { Id = id });

    /// <summary>Status by TRACES code (<c>1</c>) or alias (<c>NEW</c>, <c>VALIDATED</c>).</summary>
    public ChedBuilder WithStatus(string status) => Set(model => model with { Status = status });

    /// <summary>Makes the SOAP face refuse this CHED with a permission-denied fault.</summary>
    public ChedBuilder NotAccessible() => Set(model => model with { Accessible = false });

    public ChedBuilder WithNote(string subjectCode, string value)
    {
        _notes[subjectCode] = value;
        return this;
    }

    /// <summary>The applicant's declaration — purpose, what the goods are certified as.</summary>
    public ChedBuilder WithDeclaration(Action<AuthenticationBuilder> build) =>
        Document(document => document with { Declaration = Authentication(build) });

    /// <summary>The official inspector's decision. Adding it is what "deciding" a CHED means.</summary>
    public ChedBuilder WithClearance(Action<AuthenticationBuilder> build) =>
        Document(document => document with { Clearance = Authentication(build) });

    public ChedBuilder WithSupportingDocument(string typeCode, string reference, string issuingCountry) =>
        Document(document =>
            document with
            {
                ReferenceDocument =
                [
                    .. document.ReferenceDocument ?? [],
                    new ReferencedDocumentModel
                    {
                        DocumentTypeCode = typeCode,
                        RelationshipTypeCode = "ZZZ",
                        Identifier = reference,
                        IssuingCountry = issuingCountry,
                    },
                ],
            }
        );

    public ChedBuilder WithConsignment(Action<ConsignmentBuilder> build)
    {
        var builder = new ConsignmentBuilder(_model.SpecifiedConsignment);
        build(builder);

        return Set(model => model with { SpecifiedConsignment = builder.Build() });
    }

    public ChedControlModel Build() =>
        _model with
        {
            ExchangedDocument = _model.ExchangedDocument with { IncludedNote = _notes },
        };

    private static AuthenticationModel Authentication(Action<AuthenticationBuilder> build)
    {
        var builder = new AuthenticationBuilder();
        build(builder);
        return builder.Build();
    }

    private ChedBuilder Document(Func<ExchangedDocumentModel, ExchangedDocumentModel> change) =>
        Set(model => model with { ExchangedDocument = change(model.ExchangedDocument) });

    private ChedBuilder Set(Func<ChedControlModel, ChedControlModel> change)
    {
        _model = change(_model);
        return this;
    }
}

public class AuthenticationBuilder
{
    private readonly Dictionary<string, string> _clauses = [];
    private DateTimeOffset? _at;

    public AuthenticationBuilder At(DateTimeOffset when)
    {
        _at = when;
        return this;
    }

    public AuthenticationBuilder WithClause(string id, string content)
    {
        _clauses[id] = content;
        return this;
    }

    /// <summary>The applicant's two standard clauses.</summary>
    public AuthenticationBuilder Declaring(string purpose, string goodsCertifiedAs) =>
        WithClause("PURPOSE", purpose).WithClause("GOODS_CERTIFIED_AS", goodsCertifiedAs);

    /// <summary>All three checks satisfactory, cleared for free circulation.</summary>
    public AuthenticationBuilder Acceptable() =>
        WithClause("DECISION_CONCLUSION", "ACCEPTABLE_FOR_FREE_CIRCULATION")
            .Checked("DOCUMENTARY_CHECK", "SATISFACTORY")
            .Checked("IDENTITY_CHECK", "SATISFACTORY")
            .Checked("PHYSICAL_CHECK", "SATISFACTORY");

    public AuthenticationBuilder NotAcceptable() =>
        WithClause("DECISION_CONCLUSION", "NOT_ACCEPTABLE")
            .Checked("DOCUMENTARY_CHECK", "NOT_SATISFACTORY")
            .Checked("IDENTITY_CHECK", "NOT_SATISFACTORY")
            .Checked("PHYSICAL_CHECK", "NOT_SATISFACTORY");

    /// <summary>A check is a pair of clauses: that it happened, and how it went.</summary>
    public AuthenticationBuilder Checked(string check, string result) =>
        WithClause(check, "YES").WithClause($"{check}_RESULT", result);

    internal AuthenticationModel Build() => new() { ActualDateTime = _at, IncludedClause = _clauses };
}

public class ConsignmentBuilder(ConsignmentModel model)
{
    private readonly List<TradeLineItemModel> _commodities = [];
    private ConsignmentModel _model = model;

    public ConsignmentBuilder ArrivingAt(string borderControlPost, string countryOfEntry) =>
        Set(consignment =>
            consignment with
            {
                UnloadingBaseportLocation = new LocationModel
                {
                    Identifier = borderControlPost,
                    CountryId = countryOfEntry,
                },
            }
        );

    public ConsignmentBuilder ArrivingOn(DateTimeOffset when) =>
        Set(consignment => consignment with { AvailabilityDueDateTime = when });

    public ConsignmentBuilder ExportedFrom(string countryCode) =>
        Set(consignment => consignment with { ExportCountry = countryCode });

    public ConsignmentBuilder ImportedTo(string countryCode) =>
        Set(consignment => consignment with { ImportCountry = countryCode });

    public ConsignmentBuilder WithConsignor(Action<PartyBuilder> build) =>
        Set(consignment => consignment with { ConsignorParty = Party(build) });

    public ConsignmentBuilder WithConsignee(Action<PartyBuilder> build) =>
        Set(consignment => consignment with { ConsigneeParty = Party(build) });

    public ConsignmentBuilder WithDeliveryParty(Action<PartyBuilder> build) =>
        Set(consignment => consignment with { DeliveryParty = Party(build) });

    public ConsignmentBuilder WithCustomsTransitAgent(Action<PartyBuilder> build) =>
        Set(consignment => consignment with { CustomsTransitAgentParty = Party(build) });

    public ConsignmentBuilder TravellingBy(string modeCode, string registration, string? scheme = null) =>
        Set(consignment =>
            consignment with
            {
                MainCarriageLogisticsTransportMovement = new TransportMovementModel
                {
                    ModeCode = modeCode,
                    Identifier = registration,
                    SchemeId = scheme,
                },
            }
        );

    public ConsignmentBuilder WithCommodity(Action<CommodityBuilder> build)
    {
        var builder = new CommodityBuilder();
        build(builder);
        _commodities.Add(builder.Build());

        return this;
    }

    /// <summary>Consignment-level totals, written as the sequence-0 line TRACES puts first.</summary>
    public ConsignmentBuilder Totalling(decimal grossWeightKg, string packageType, decimal packageCount) =>
        Item(item =>
            item with
            {
                ConsignmentTotals = new TradeLineItemModel
                {
                    GrossWeight = new MeasureModel { Value = grossWeightKg, UnitCode = "KGM" },
                    PhysicalReferencedLogisticsPackage = new PackageModel
                    {
                        TypeCode = packageType,
                        ItemQuantity = packageCount,
                    },
                },
            }
        );

    public ConsignmentBuilder CarryingCargoType(string cargoType) =>
        Item(item => item with { NatureIdCargo = cargoType });

    internal ConsignmentModel Build() =>
        _commodities.Count == 0
            ? _model
            : Item(item => item with { IncludedTradeLineItem = _commodities })._model;

    private static PartyModel Party(Action<PartyBuilder> build)
    {
        var builder = new PartyBuilder();
        build(builder);
        return builder.Build();
    }

    private ConsignmentBuilder Item(Func<ConsignmentItemModel, ConsignmentItemModel> change) =>
        Set(consignment =>
            consignment with
            {
                IncludedConsignmentItem = change(consignment.IncludedConsignmentItem ?? new ConsignmentItemModel()),
            }
        );

    private ConsignmentBuilder Set(Func<ConsignmentModel, ConsignmentModel> change)
    {
        _model = change(_model);
        return this;
    }
}

public class PartyBuilder
{
    private PartyModel _model = new();

    /// <summary>A registered operator: the simulator resolves the name, as TRACES does.</summary>
    public PartyBuilder Operator(string identifier, string? scheme = null) =>
        Set(party => party with { Identifier = identifier, SchemeId = scheme });

    /// <summary>An operator created on the fly — a name and no identifier, so no lookup happens.</summary>
    public PartyBuilder Named(string name) => Set(party => party with { Name = name });

    public PartyBuilder InCountry(string countryCode) => Address(address => address with { CountryId = countryCode });

    public PartyBuilder At(string lineOne, string city, string postcode) =>
        Address(address => address with { LineOne = lineOne, CityName = city, PostcodeCode = postcode });

    internal PartyModel Build() => _model;

    private PartyBuilder Address(Func<AddressModel, AddressModel> change) =>
        Set(party => party with { PostalAddress = change(party.PostalAddress ?? new AddressModel()) });

    private PartyBuilder Set(Func<PartyModel, PartyModel> change)
    {
        _model = change(_model);
        return this;
    }
}

public class CommodityBuilder
{
    private readonly Dictionary<string, string> _classifications = [];
    private readonly Dictionary<string, string> _notes = [];
    private TradeLineItemModel _model = new();

    /// <summary>Combined Nomenclature code. The simulator expands it to its description hierarchy.</summary>
    public CommodityBuilder CnCode(string cnCode) => ClassifiedAs("CN", cnCode);

    public CommodityBuilder ClassifiedAs(string system, string code)
    {
        _classifications[system] = code;
        return this;
    }

    public CommodityBuilder OriginCountry(string countryCode) =>
        Set(item => item with { OriginCountry = countryCode });

    public CommodityBuilder ScientificName(string name) => Set(item => item with { ScientificName = name });

    public CommodityBuilder NetWeightKg(decimal kilograms) =>
        Set(item => item with { NetWeight = new MeasureModel { Value = kilograms, UnitCode = "KGM" } });

    public CommodityBuilder Packages(decimal count, string packageType) =>
        Set(item =>
            item with
            {
                PhysicalReferencedLogisticsPackage = new PackageModel
                {
                    TypeCode = packageType,
                    ItemQuantity = count,
                },
            }
        );

    public CommodityBuilder WithNote(string subjectCode, string value)
    {
        _notes[subjectCode] = value;
        return this;
    }

    internal TradeLineItemModel Build() =>
        _model with { ApplicableClassification = _classifications, AdditionalInformationNote = _notes };

    private CommodityBuilder Set(Func<TradeLineItemModel, TradeLineItemModel> change)
    {
        _model = change(_model);
        return this;
    }
}
