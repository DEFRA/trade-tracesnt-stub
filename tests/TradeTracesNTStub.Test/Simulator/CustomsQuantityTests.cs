using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Ports;
using CoreWCF;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// The customs ledger is stateful, so the tests that matter are sequences: each one reads the ledger
/// after every step, because an operation that answered correctly but left the wrong state behind
/// would otherwise pass.
/// </summary>
public class CustomsQuantityTests
{
    private const string ChedId = "CHEDA.XI.2026.0000001";
    private const string MrnA = "26GB00000000000001";
    private const string MrnB = "26GB00000000000002";
    private const string Office = "GBTEST01";

    private static readonly SpsCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    private readonly ChedStore _cheds = new();
    private readonly CustomsCertexChedSimulator _port;

    public CustomsQuantityTests()
    {
        WithStatus("VALIDATED");
        _port = new CustomsCertexChedSimulator(_cheds, new CustomsLedger());
    }

    [Fact]
    public async Task ReadingTheLedgerDoesNotChangeIt()
    {
        var first = await Read();
        var second = await Read();

        Available(first).Should().Equal(100m, 50m);
        Available(second).Should().Equal(100m, 50m);
        second.QuantityManagementSummary.ReservedQuantity.Should().BeEmpty();
        second.QuantityManagementSummary.ConsumedQuantity.Should().BeEmpty();
    }

    [Fact]
    public async Task AReservationThenAReleaseConsumesTheQuantity()
    {
        var reserved = await Reservation(MrnA, Item(line: 1, quantity: 30));
        reserved.ReservationResult.Should().BeTrue();

        var afterReserve = await Read();
        Available(afterReserve).Should().Equal(70m, 50m);
        afterReserve.QuantityManagementSummary.ReservedQuantity.Should().ContainSingle().Which.Item.Should().Be(MrnA);

        (await Clearance(MrnA, GoodsClearanceInformationType.Item01)).QuantityManagementOutcome.Should().Be("01");

        var afterRelease = await Read();
        Available(afterRelease).Should().Equal(70m, 50m);
        afterRelease.QuantityManagementSummary.ReservedQuantity.Should().BeEmpty();
        afterRelease
            .QuantityManagementSummary.ConsumedQuantity.Should()
            .ContainSingle()
            .Which.SwSupportingDocument.Quantity.Value.Should()
            .Be(30m);
    }

    [Fact]
    public async Task AReservationThenADeleteGivesTheQuantityBack()
    {
        await Reservation(MrnA, Item(line: 1, quantity: 30));

        (await Clearance(MrnA, GoodsClearanceInformationType.Item02)).QuantityManagementOutcome.Should().Be("01");

        var afterDelete = await Read();
        Available(afterDelete).Should().Equal(100m, 50m);
        afterDelete.QuantityManagementSummary.ReservedQuantity.Should().BeEmpty();
        afterDelete.QuantityManagementSummary.ConsumedQuantity.Should().BeEmpty();
    }

    [Fact]
    public async Task ReservingAgainForTheSameDeclarationReplacesTheHold()
    {
        await Reservation(MrnA, Item(line: 1, quantity: 30));
        await Reservation(MrnA, Item(line: 1, quantity: 10));

        var ledger = await Read();

        Available(ledger).Should().Equal(90m, 50m);
        ledger.QuantityManagementSummary.ReservedQuantity.Should().ContainSingle();
    }

    [Fact]
    public async Task ARefusedReplacementEndsTheExistingHold()
    {
        await Reservation(MrnA, Item(line: 1, quantity: 30));

        var refused = await Reservation(MrnA, Item(line: 1, quantity: 101));

        refused.ReservationFailureReason.Should().Be("05");
        refused.QuantityManagementSummary.ReservedQuantity.Should().BeEmpty("the refusal is the declaration's new position");
        Available(await Read()).Should().Equal(100m, 50m);
    }

    [Fact]
    public async Task ARefusedReplacementLeavesOtherDeclarationsAlone()
    {
        await Reservation(MrnA, Item(line: 1, quantity: 30));
        await Reservation(MrnB, Item(line: 1, quantity: 20));

        await Reservation(MrnA, Item(line: 9, quantity: 1));

        var ledger = await Read();
        ledger.QuantityManagementSummary.ReservedQuantity.Should().ContainSingle().Which.Item.Should().Be(MrnB);
        Available(ledger).Should().Equal(80m, 50m);
    }

    [Fact]
    public async Task AReplacementRefusedForTheChedsStatusAlsoEndsTheHold()
    {
        await Reservation(MrnA, Item(line: 1, quantity: 30));
        WithStatus("CANCELLED");

        (await Reservation(MrnA, Item(line: 1, quantity: 10))).ReservationFailureReason.Should().Be("04");

        (await Read()).QuantityManagementSummary.ReservedQuantity.Should().BeEmpty();
    }

    [Fact]
    public async Task AReservationCarriesTheDeclarationItemAndOffice()
    {
        await Reservation(MrnA, Item(line: 2, quantity: 5, goodsItem: 3, classCode: "08051022"));

        var allocation = (await Read()).QuantityManagementSummary.ReservedQuantity.Single();

        allocation.ItemElementName.Should().Be(ItemChoiceType2.MRN);
        allocation.GoodsItemNumber.Should().Be("3");
        allocation.SwSupportingDocument.CertificateLineNumber.Should().Be("2");
        allocation.CommodityCode.HarmonizedSystemSubheadingcode.Should().Be("08051022");
        allocation.CompetentCustomsOffice.ReferenceNumber.Should().Be(Office);
        allocation.EventDateTimeSpecified.Should().BeTrue();
    }

    [Fact]
    public async Task ATaricCodeUnderTheLinesCnCodeMatchesAndIsReportedAsDeclared()
    {
        (await Reservation(MrnA, Item(line: 1, quantity: 1, classCode: "0106200010"))).ReservationResult.Should().BeTrue();

        var summary = (await Read()).QuantityManagementSummary;

        summary.AvailableQuantity[0].CommodityCode.HarmonizedSystemSubheadingcode.Should().Be("01062000");
        summary.ReservedQuantity.Single().CommodityCode.HarmonizedSystemSubheadingcode.Should().Be("0106200010");
    }

    [Theory]
    [InlineData(1, 101, "01062000", "KGM", "05")] // more than the line holds
    [InlineData(1, 1, "08051022", "KGM", "03")] // the other line's commodity
    [InlineData(9, 1, "01062000", "KGM", "07")] // no such line
    [InlineData(1, 1, "01062000", "TNE", "10")] // the line is in kilograms
    public async Task AReservationThatFailsACheckIsRefusedAndTakesNothing(
        int line,
        decimal quantity,
        string classCode,
        string unit,
        string reason
    )
    {
        var refused = await Reservation(MrnA, Item(line, quantity, goodsItem: 4, classCode, unit));

        refused.ReservationResult.Should().BeFalse();
        refused.ReservationResultSpecified.Should().BeTrue();
        refused.ReservationFailureReason.Should().Be(reason);
        refused.ReservationFailureConsignmentItem.GoodsItemNumber.Should().Be("4");
        // Acceptance names the CHED line only when there is one.
        refused.ReservationFailureConsignmentItem.DocumentLineItemNumber.Should().Be(reason == "07" ? null : line.ToString());

        var ledger = await Read();
        Available(ledger).Should().Equal(100m, 50m);
        ledger.QuantityManagementSummary.ReservedQuantity.Should().BeEmpty();
    }

    [Fact]
    public async Task OneDeclarationCannotTakeWhatAnotherHolds()
    {
        await Reservation(MrnA, Item(line: 1, quantity: 70));

        var refused = await Reservation(MrnB, Item(line: 1, quantity: 40));

        refused.ReservationFailureReason.Should().Be("05");
    }

    [Fact]
    public async Task ADeclarationAlreadyReleasedCannotReserveAgain()
    {
        await Reservation(MrnA, Item(line: 1, quantity: 10));
        await Clearance(MrnA, GoodsClearanceInformationType.Item01);

        (await Reservation(MrnA, Item(line: 1, quantity: 10))).ReservationFailureReason.Should().Be("06");
    }

    [Theory]
    [InlineData(GoodsClearanceInformationType.Item01)]
    [InlineData(GoodsClearanceInformationType.Item02)]
    public async Task ClearingADeclarationWithNothingReservedIsNotFound(GoodsClearanceInformationType mode)
    {
        (await Clearance(MrnA, mode)).QuantityManagementOutcome.Should().Be("02");
    }

    [Theory]
    [InlineData(GoodsClearanceInformationType.Item01)]
    [InlineData(GoodsClearanceInformationType.Item02)]
    public async Task ClearingADeclarationThatHasAlreadyBeenReleasedIsAlreadyConsumed(GoodsClearanceInformationType mode)
    {
        await Reservation(MrnA, Item(line: 1, quantity: 10));
        await Clearance(MrnA, GoodsClearanceInformationType.Item01);

        (await Clearance(MrnA, mode)).QuantityManagementOutcome.Should().Be("03");
    }

    [Theory]
    [InlineData("NEW")]
    [InlineData("IN_PROGRESS")]
    [InlineData("REJECTED")]
    [InlineData("CANCELLED")]
    public async Task AChedThatIsNotValidatedCannotBeReservedAgainst(string status)
    {
        WithStatus(status);

        var refused = await Reservation(MrnA, Item(line: 1, quantity: 10));

        refused.ReservationResult.Should().BeFalse();
        refused.ReservationFailureReason.Should().Be("04");
        refused.ReservationFailureConsignmentItem.Should().BeNull("the refusal is about the CHED, not an item");
        Available(refused).Should().Equal(100m, 50m);
        (await Read()).QuantityManagementSummary.ReservedQuantity.Should().BeEmpty();
    }

    [Fact]
    public async Task AChedThatIsNotValidatedCanStillBeRead()
    {
        WithStatus("NEW");

        Available(await Read()).Should().Equal(100m, 50m);
    }

    [Fact]
    public async Task ReleasingAfterTheChedHasMovedOnWritesOffWithAWarning()
    {
        await Reservation(MrnA, Item(line: 1, quantity: 10));
        WithStatus("CANCELLED");

        var released = await Clearance(MrnA, GoodsClearanceInformationType.Item01);

        released.QuantityManagementOutcome.Should().Be("04");
        (await Read()).QuantityManagementSummary.ConsumedQuantity.Should().ContainSingle();
    }

    [Fact]
    public async Task AnUnknownChedIsAResponseWithNoCertificateNotAFault()
    {
        var response = await Read("CHEDA.XI.2026.9999999");

        response.ChedCertificate.Should().BeNull();
        response.QuantityManagementSummary.Should().BeNull();
    }

    [Fact]
    public async Task TheHeaderIsEchoed()
    {
        var response = await _port.processedChedRequestAsync(ReadRequest(ChedId));

        response.CertexHeader.MessageId.Should().Be("0123456789abcdef0123456789abcdef");
        response.CertexHeader.UniqRequesterPrefix.Should().Be(Office);
    }

    [Fact]
    public async Task AReadThatCarriesADeclarationIsRejected()
    {
        // Read and reserve differ by one character. A read that also names a declaration is a
        // gateway bug, and it must fail here rather than be quietly served as either.
        var request = ReadRequest(ChedId);
        request.ProcessedChedRequest1.CustomsDeclarationReferenceNumber = Mrn(MrnA);

        var act = () => _port.processedChedRequestAsync(request);

        (await act.Should().ThrowAsync<FaultException<ExceptionWithUniqueInfoType>>())
            .Which.Detail.MessageId.Should()
            .Be("0123456789abcdef0123456789abcdef");
    }

    [Fact]
    public async Task AReservationWithNoItemsIsRejected()
    {
        var act = () => _port.processedChedRequestAsync(ReserveRequest(MrnA));

        await act.Should().ThrowAsync<FaultException<ExceptionWithUniqueInfoType>>();
    }

    [Fact]
    public async Task AReadAskingForAPdfIsRejected()
    {
        var request = ReadRequest(ChedId);
        request.ProcessedChedRequest1.PdfGenerationIndication = true;
        request.ProcessedChedRequest1.PdfGenerationIndicationSpecified = true;

        var act = () => _port.processedChedRequestAsync(request);

        await act.Should().ThrowAsync<FaultException<ExceptionWithUniqueInfoType>>();
    }

    /// <summary>Stores the CHED afresh in this status, as a control-API PATCH of the status would.</summary>
    private void WithStatus(string status)
    {
        var model = AChedA with { Status = status };
        _cheds.Put(new StoredCertificate(ChedId, s_builder.Build(CertificateKind.Ched, model, ChedId), true, model));
    }

    private async Task<ProcessedChedInformationResponseType> Read(string chedId = ChedId) =>
        (await _port.processedChedRequestAsync(ReadRequest(chedId))).ProcessedChedInformationResponse1;

    private async Task<ProcessedChedInformationResponseType> Reservation(
        string mrn,
        params ConsignmentItemR6ForReservationType[] items
    ) => (await _port.processedChedRequestAsync(ReserveRequest(mrn, items))).ProcessedChedInformationResponse1;

    private async Task<ChedQuantityManagementOutcomeType> Clearance(string mrn, GoodsClearanceInformationType mode) =>
        (
            await _port.chedClearanceRequestAsync(
                new ChedClearanceRequest
                {
                    CertexHeader = Header,
                    CustomsOfficeReferenceNumber = Office,
                    ChedClearanceRequest1 = new ChedClearanceRequestType
                    {
                        ChedCertificateId = ChedId,
                        CustomsDocumentReference = mrn,
                        CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = Office },
                        GoodsClearanceInformation = mode,
                        SendingDate = DateTime.UtcNow,
                    },
                }
            )
        ).ChedClearanceResponse1;

    /// <summary>A read as the gateway sends it: indication 0, and a declaration element present but empty.</summary>
    private static ProcessedChedRequest ReadRequest(string chedId) =>
        Request(chedId, "0", new CustomsDeclarationReferenceNumber4CoiChedR51InputType(), null);

    private static ProcessedChedRequest ReserveRequest(string mrn, params ConsignmentItemR6ForReservationType[] items) =>
        Request(ChedId, "1", Mrn(mrn), items);

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

    private static CustomsDeclarationReferenceNumber4CoiChedR51InputType Mrn(string mrn) =>
        new() { Item = mrn, ItemElementName = ItemChoiceType1.MRN };

    private static ConsignmentItemR6ForReservationType Item(
        int line,
        decimal quantity,
        int goodsItem = 1,
        string classCode = "01062000",
        string unit = "KGM"
    ) =>
        new()
        {
            GoodsItemNumber = goodsItem.ToString(),
            CertificateLineNumber = line.ToString(),
            ClassCode = classCode,
            NetWeightQuantity = quantity,
            NetWeightQuantitySpecified = true,
            NetWeightUnitOfMeasure = Enum.Parse<UniversalUnitOfMeasureType>(unit),
            NetWeightUnitOfMeasureSpecified = true,
        };

    private static IEnumerable<decimal> Available(ProcessedChedInformationResponseType response) =>
        response.QuantityManagementSummary.AvailableQuantity.Select(line => line.SwSupportingDocument.Quantity);

    /// <summary>Two lines: 100 kg of one commodity, 50 kg of another.</summary>
    private static CertificateControlModel AChedA =>
        new()
        {
            Status = "VALIDATED",
            ExchangedDocument = new ExchangedDocumentModel
            {
                IncludedNote = new Dictionary<string, string> { ["CHED_TYPE"] = "A" },
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
                        Line("01062000", 100m),
                        Line("08051022", 50m),
                    ],
                },
            },
        };

    private static TradeLineItemModel Line(string cnCode, decimal kilograms) =>
        new()
        {
            ApplicableClassification = new Dictionary<string, string> { ["CN"] = cnCode },
            OriginCountry = "AF",
            NetWeight = new MeasureModel { Value = kilograms, UnitCode = "KGM" },
        };
}
