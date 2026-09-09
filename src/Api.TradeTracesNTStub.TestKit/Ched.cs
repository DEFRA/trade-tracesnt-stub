using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace Api.TradeTracesNTStub.TestKit;

/// <summary>
/// Fluent starting points for building a CHED fixture.
/// </summary>
/// <remarks>
/// The control model is already all-optional, so a builder is not strictly needed — it earns its
/// place by letting a test read as the scenario it describes rather than as a JSON literal, and by
/// keeping the codes a test has to remember in one place.
/// </remarks>
public static class Ched
{
    public static ChedBuilder ChedA() => new(ChedType.A);

    public static ChedBuilder ChedP() => new(ChedType.P);

    public static ChedBuilder ChedPP() => new(ChedType.PP);

    public static ChedBuilder ChedD() => new(ChedType.D);

    public static ChedBuilder OfType(ChedType type) => new(type);
}

public class ChedBuilder(ChedType type)
{
    private readonly List<CommodityModel> _commodities = [];
    private ChedControlModel _model = new() { Type = type };

    public ChedBuilder WithId(string id) => Set(model => model with { Id = id });

    public ChedBuilder FromTemplate(string template) => Set(model => model with { Template = template });

    /// <summary>Status by TRACES code (<c>1</c>) or by name (<c>NEW</c>, <c>VALIDATED</c>).</summary>
    public ChedBuilder WithStatus(string status) => Set(model => model with { Status = status });

    /// <summary>
    /// Marks the CHED unreadable, so the SOAP face answers with a permission-denied fault. The way
    /// to cover the 403 path without a magic ID.
    /// </summary>
    public ChedBuilder NotAccessible() => Set(model => model with { Accessible = false });

    public ChedBuilder WithConsignor(string operatorId, string? name = null) =>
        Set(model => model with { Consignor = new PartyModel { OperatorId = operatorId, Name = name } });

    public ChedBuilder WithConsignee(string operatorId, string? name = null) =>
        Set(model => model with { Consignee = new PartyModel { OperatorId = operatorId, Name = name } });

    public ChedBuilder WithDeliveryParty(string operatorId, string? name = null) =>
        Set(model => model with { DeliveryParty = new PartyModel { OperatorId = operatorId, Name = name } });

    /// <summary>The BCP of arrival, by TRACES activity code or UN/LOCODE.</summary>
    public ChedBuilder ArrivingAt(string borderControlPost) =>
        Set(model => model with { BorderControlPost = borderControlPost });

    public ChedBuilder WithCommodity(Action<CommodityBuilder> build)
    {
        var builder = new CommodityBuilder();
        build(builder);
        _commodities.Add(builder.Build());

        return this;
    }

    public ChedBuilder WithDecision(Action<DecisionBuilder> build)
    {
        var builder = new DecisionBuilder();
        build(builder);

        return Set(model => model with { Decision = builder.Build() });
    }

    public ChedControlModel Build() =>
        _commodities.Count == 0 ? _model : _model with { Commodities = _commodities };

    private ChedBuilder Set(Func<ChedControlModel, ChedControlModel> change)
    {
        _model = change(_model);
        return this;
    }
}

public class CommodityBuilder
{
    private CommodityModel _model = new();

    public CommodityBuilder CnCode(string cnCode) => Set(model => model with { CnCode = cnCode });

    public CommodityBuilder Describedas(string description) => Set(model => model with { Description = description });

    public CommodityBuilder OriginCountry(string countryCode) =>
        Set(model => model with { OriginCountry = countryCode });

    public CommodityBuilder ScientificName(string name) => Set(model => model with { ScientificName = name });

    public CommodityBuilder NetWeightKg(decimal kilograms) => Set(model => model with { NetWeightKg = kilograms });

    public CommodityBuilder GrossWeightKg(decimal kilograms) => Set(model => model with { GrossWeightKg = kilograms });

    /// <summary>Packaging, by UNECE package type code — <c>BX</c> box, <c>BG</c> bag.</summary>
    public CommodityBuilder Packages(decimal count, string packageType) =>
        Set(model => model with { PackageCount = count, PackageType = packageType });

    internal CommodityModel Build() => _model;

    private CommodityBuilder Set(Func<CommodityModel, CommodityModel> change)
    {
        _model = change(_model);
        return this;
    }
}

public class DecisionBuilder
{
    private DecisionModel _model = new();

    /// <summary>All three checks satisfactory, cleared for free circulation.</summary>
    public DecisionBuilder Acceptable() =>
        Set(model =>
            model with
            {
                Conclusion = "ACCEPTABLE_FOR_FREE_CIRCULATION",
                DocumentaryCheck = "SATISFACTORY",
                IdentityCheck = "SATISFACTORY",
                PhysicalCheck = "SATISFACTORY",
            }
        );

    /// <summary>Refused, with the reasons and the measure taken on the consignment.</summary>
    public DecisionBuilder NotAcceptable(string measure, params string[] refusalReasons) =>
        Set(model =>
            model with
            {
                Conclusion = "NOT_ACCEPTABLE",
                DocumentaryCheck = "NOT_SATISFACTORY",
                IdentityCheck = "NOT_SATISFACTORY",
                PhysicalCheck = "NOT_SATISFACTORY",
                NotAcceptableMeasure = measure,
                RefusalReasons = refusalReasons,
            }
        );

    public DecisionBuilder Concluding(string conclusion) => Set(model => model with { Conclusion = conclusion });

    public DecisionBuilder DocumentaryCheck(string result) => Set(model => model with { DocumentaryCheck = result });

    public DecisionBuilder IdentityCheck(string result) => Set(model => model with { IdentityCheck = result });

    public DecisionBuilder PhysicalCheck(string result) => Set(model => model with { PhysicalCheck = result });

    internal DecisionModel Build() => _model;

    private DecisionBuilder Set(Func<DecisionModel, DecisionModel> change)
    {
        _model = change(_model);
        return this;
    }
}
