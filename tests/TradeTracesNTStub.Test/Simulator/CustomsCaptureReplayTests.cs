using System.Xml.Linq;
using System.Xml.Serialization;
using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Ports;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// Replays the sequence captured from TRACES acceptance on 2026-09-29 against the simulator, and
/// holds every simulator response to the one acceptance gave for the same step. This is what ties
/// the ledger to real TRACES: the other customs tests say what the simulator does, this says it is
/// what TRACES does.
/// </summary>
/// <remarks>
/// The captures are in <c>Captures/Customs</c>, one request and response per step, with the
/// certificate removed. The simulator holds copies of the two acceptance CHEDs: one validated with
/// 1000 kg of milk and 600 kg already consumed, one new with 1100 kg. Timestamps are compared only
/// for presence, since the simulator's clock is not acceptance's.
/// </remarks>
public class CustomsCaptureReplayTests
{
    private const string V06 = "http://ec.europa.eu/sanco/tracesnt/customs_certex/ched/v06";
    private const string Validated = "CHEDP.XI.2026.0000875";
    private const string New = "CHEDP.XI.2026.0000877";
    private const string MrnA = "26GB1669CAPTUREA01";
    private const string MrnB = "26GB1669CAPTUREB01";
    private const string MrnC = "26GB1669CAPTUREC01";
    private const string MrnConsumed = "24GBBGBKCDMS640103";
    private const string Office = "XI000002";

    private static readonly SpsCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    private readonly CustomsCertexChedSimulator _port;

    public CustomsCaptureReplayTests()
    {
        var cheds = new ChedStore();
        Store(cheds, Validated, "VALIDATED", 1000m);
        Store(cheds, New, "NEW", 1100m);
        _port = new CustomsCertexChedSimulator(cheds, new CustomsLedger());
    }

    [Fact]
    public async Task TheSimulatorAnswersEachStepAsAcceptanceDid()
    {
        // Where acceptance started: someone else's declaration already took 600 kg and cleared.
        await Reserve(Validated, MrnConsumed, 600m);
        await Clear(Validated, MrnConsumed, GoodsClearanceInformationType.Item01);

        Matches("01-read", await Read(Validated));
        Matches("02-reserveA", await Reserve(Validated, MrnA, 0.001m));
        Matches("03-read", await Read(Validated));
        Matches("04-reserveA-again", await Reserve(Validated, MrnA, 0.002m));
        Matches("05-releaseA", await Clear(Validated, MrnA, GoodsClearanceInformationType.Item01));
        Matches("06-read", await Read(Validated));
        Matches("07-releaseA-again", await Clear(Validated, MrnA, GoodsClearanceInformationType.Item01));
        Matches("08-reserveB", await Reserve(Validated, MrnB, 0.001m));
        Matches("09-deleteB", await Clear(Validated, MrnB, GoodsClearanceInformationType.Item02));
        Matches("10-read", await Read(Validated));
        Matches("11-deleteB-again", await Clear(Validated, MrnB, GoodsClearanceInformationType.Item02));
        Matches("12-releaseB", await Clear(Validated, MrnB, GoodsClearanceInformationType.Item01));
        Matches("13-over", await Reserve(Validated, MrnB, 99999999m));
        Matches("14-wrong-code", await Reserve(Validated, MrnB, 0.001m, classCode: "99999999"));
        Matches("15-unknown-line", await Reserve(Validated, MrnB, 0.001m, line: 999));
        Matches("16-unknown-ched", await Read("CHEDP.XI.2026.9999999"));
        Matches("17-reserve-not-validated", await Reserve(New, MrnB, 0.001m));
        Matches("18-read-not-validated", await Read(New));
        Matches("19-reserve-consumed-mrn", await Reserve(Validated, MrnConsumed, 0.001m));
        Matches("20-delete-consumed-mrn", await Clear(Validated, MrnConsumed, GoodsClearanceInformationType.Item02));

        // A refused replacement ends the declaration's existing hold.
        Matches("21-reserveC", await Reserve(Validated, MrnC, 0.001m));
        Matches("22-replaceC-over", await Reserve(Validated, MrnC, 99999999m));
        Matches("23-read", await Read(Validated));
    }

    private static void Matches(string step, ProcessedChedInformationResponseType simulated)
    {
        var captured = Captured<ProcessedChedInformationResponseType>(step, "ProcessedChedInformationResponse");

        Shape(simulated).Should().BeEquivalentTo(Shape(captured), $"acceptance answered step {step} that way");
    }

    private static void Matches(string step, ChedQuantityManagementOutcomeType simulated)
    {
        var captured = Captured<ChedQuantityManagementOutcomeType>(step, "ChedClearanceResponse");

        new { simulated.QuantityManagementOutcome, simulated.StatusCode, simulated.OperationCode }
            .Should()
            .BeEquivalentTo(
                new { captured.QuantityManagementOutcome, captured.StatusCode, captured.OperationCode },
                $"acceptance answered step {step} that way"
            );
    }

    /// <summary>
    /// What a consumer can see. An absent list and an empty one are the same on the wire, so both
    /// come out empty.
    /// </summary>
    private static object Shape(ProcessedChedInformationResponseType response) =>
        new
        {
            response.OperationCode,
            response.PushActive,
            response.ReservationResultSpecified,
            response.ReservationResult,
            response.ReservationFailureReason,
            FailedItem = response.ReservationFailureConsignmentItem is { } item
                ? new { item.GoodsItemNumber, item.DocumentLineItemNumber }
                : null,
            HasSummary = response.QuantityManagementSummary is not null,
            Available = (response.QuantityManagementSummary?.AvailableQuantity ?? [])
                .Select(line => new
                {
                    line.CommodityCode?.HarmonizedSystemSubheadingcode,
                    line.CommodityCode?.CombinedNomenclatureCode,
                    line.CommodityCode?.TARICCode,
                    line.SwSupportingDocument.UnitOfMeasure,
                    line.SwSupportingDocument.Quantity,
                    line.SwSupportingDocument.CertificateLineNumber,
                })
                .ToList(),
            Reserved = Allocations(response.QuantityManagementSummary?.ReservedQuantity),
            Consumed = Allocations(response.QuantityManagementSummary?.ConsumedQuantity),
        };

    private static List<object> Allocations(AllocatedProductQuantityByCustomsOfficeEnhanced4ChedR51Type[]? allocations) =>
        [
            .. (allocations ?? []).Select(allocation => new
            {
                allocation.GoodsItemNumber,
                allocation.CommodityCode?.HarmonizedSystemSubheadingcode,
                allocation.CommodityCode?.CombinedNomenclatureCode,
                allocation.CommodityCode?.TARICCode,
                allocation.SwSupportingDocument.UnitOfMeasure,
                allocation.SwSupportingDocument.Quantity.Value,
                allocation.SwSupportingDocument.Quantity.TechnicalRoundingQuantitySpecified,
                allocation.SwSupportingDocument.CertificateLineNumber,
                allocation.EventDateTimeSpecified,
                allocation.CompetentCustomsOffice?.ReferenceNumber,
                allocation.Item,
                allocation.ItemElementName,
            }),
        ];

    /// <summary>Reads a captured response body with the same generated types a consumer uses.</summary>
    private static T Captured<T>(string step, string element)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Simulator", "Captures", "Customs", $"{step}.response.xml");
        var body = XDocument.Load(path).Descendants(XName.Get(element, V06)).Single();
        var serializer = new XmlSerializer(typeof(T), new XmlRootAttribute(element) { Namespace = V06 });

        using var reader = body.CreateReader();
        return (T)serializer.Deserialize(reader)!;
    }

    private async Task<ProcessedChedInformationResponseType> Read(string chedId) =>
        (await _port.processedChedRequestAsync(Request(chedId, "0", new(), null))).ProcessedChedInformationResponse1;

    /// <summary>Reserves as the capture did: goods item 1 against line 1, as milk (<c>040100</c>), in kilograms.</summary>
    private async Task<ProcessedChedInformationResponseType> Reserve(
        string chedId,
        string mrn,
        decimal kilograms,
        int line = 1,
        string classCode = "040100"
    ) =>
        (
            await _port.processedChedRequestAsync(
                Request(
                    chedId,
                    "1",
                    new() { Item = mrn, ItemElementName = ItemChoiceType1.MRN },
                    [
                        new ConsignmentItemR6ForReservationType
                        {
                            GoodsItemNumber = "1",
                            CertificateLineNumber = line.ToString(),
                            ClassCode = classCode,
                            NetWeightQuantity = kilograms,
                            NetWeightQuantitySpecified = true,
                            NetWeightUnitOfMeasure = UniversalUnitOfMeasureType.KGM,
                            NetWeightUnitOfMeasureSpecified = true,
                        },
                    ]
                )
            )
        ).ProcessedChedInformationResponse1;

    private async Task<ChedQuantityManagementOutcomeType> Clear(
        string chedId,
        string mrn,
        GoodsClearanceInformationType mode
    ) =>
        (
            await _port.chedClearanceRequestAsync(
                new ChedClearanceRequest
                {
                    CertexHeader = Header,
                    CustomsOfficeReferenceNumber = Office,
                    ChedClearanceRequest1 = new ChedClearanceRequestType
                    {
                        ChedCertificateId = chedId,
                        CustomsDocumentReference = mrn,
                        CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = Office },
                        GoodsClearanceInformation = mode,
                        SendingDate = DateTime.UtcNow,
                    },
                }
            )
        ).ChedClearanceResponse1;

    private static ProcessedChedRequest Request(
        string chedId,
        string indication,
        CustomsDeclarationReferenceNumber4CoiChedR51InputType declaration,
        ConsignmentItemR6ForReservationType[]? items
    ) =>
        new()
        {
            CertexHeader = Header,
            CustomsOfficeReferenceNumber = Office,
            ProcessedChedRequest1 = new ProcessedChedRequestType
            {
                SendingDate = DateTime.UtcNow,
                ChedCertificateId = chedId,
                CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = Office },
                QuantityManagementIndication = indication,
                CustomsDeclarationReferenceNumber = declaration,
                CommodityDescriptionForChed = items,
            },
        };

    private static CertexHeaderType Header =>
        new() { MessageId = "0123456789abcdef0123456789abcdef", UniqRequesterPrefix = Office };

    private static void Store(ChedStore cheds, string id, string status, decimal kilograms)
    {
        var model = new CertificateControlModel
        {
            Status = status,
            ExchangedDocument = new ExchangedDocumentModel
            {
                IncludedNote = new Dictionary<string, string> { ["CHED_TYPE"] = "P" },
            },
            SpecifiedConsignment = new ConsignmentModel
            {
                ExportCountry = "AF",
                ImportCountry = "XI",
                UnloadingBaseportLocation = new LocationModel { Identifier = "GBBEL", CountryId = "XI" },
                IncludedConsignmentItem = new ConsignmentItemModel
                {
                    ConsignmentTotals = new TradeLineItemModel(),
                    IncludedTradeLineItem =
                    [
                        new TradeLineItemModel
                        {
                            ApplicableClassification = new Dictionary<string, string> { ["CN"] = "0401" },
                            OriginCountry = "AF",
                            NetWeight = new MeasureModel { Value = kilograms, UnitCode = "KGM" },
                        },
                    ],
                },
            },
        };

        cheds.Put(new StoredCertificate(id, s_builder.Build(CertificateKind.Ched, model, id), true, model));
    }
}
