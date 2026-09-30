using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

/// <summary>
/// Quantity management against the CHEDs the control API stored. Two operations carry four
/// behaviours, told apart by field values rather than by operation: <c>processedChedRequest</c>
/// reads with indication <c>0</c> and reserves with <c>1</c>, and <c>chedClearanceRequest</c>
/// releases with <c>01</c> and deletes with <c>02</c>. A request that mixes the two shapes is
/// refused rather than guessed at, because a gateway that sends a read shaped like a reservation is
/// exactly the bug this port is here to catch.
/// </summary>
public class CustomsCertexChedSimulator(ChedStore cheds, CustomsLedger ledger)
    : CustomsCertexChedPort
{
    private const string ReadOnly = "0";
    private const string Reserve = "1";
    private const string NoOperation = "0";

    /// <summary>
    /// The statuses quantity can be reserved against. Acceptance refuses a new CHED with reason 04;
    /// validated is the only status seen to succeed, so it is the only one allowed.
    /// </summary>
    private static readonly HashSet<string> s_reservableStatuses = ["70"];

    public Task<ProcessedChedInformationResponse> processedChedRequestAsync(ProcessedChedRequest request)
    {
        var header = request.CertexHeader;
        var body = request.ProcessedChedRequest1 ?? throw SimulatorFaults.Customs(header, "Missing request body");

        if (body.PdfGenerationIndicationSpecified && body.PdfGenerationIndication)
        {
            throw SimulatorFaults.Customs(header, "PDF generation is not supported by the TRACES NT simulator");
        }

        var declaration = body.CustomsDeclarationReferenceNumber?.Item;
        var items = body.CommodityDescriptionForChed ?? [];

        var reserving = body.QuantityManagementIndication switch
        {
            ReadOnly when string.IsNullOrEmpty(declaration) && items.Length == 0 => false,
            Reserve
                when body.CustomsDeclarationReferenceNumber?.ItemElementName == ItemChoiceType1.MRN
                    && !string.IsNullOrEmpty(declaration)
                    && items.Length > 0 => true,
            _ => throw SimulatorFaults.Customs(
                header,
                $"QuantityManagementIndication '{body.QuantityManagementIndication}' does not match the "
                    + "declaration and items sent: a read sends 0 with neither, a reservation sends 1 "
                    + "with an MRN and at least one item"
            ),
        };

        // An unknown CHED is not a fault on this port: TRACES answers with no certificate.
        if (!cheds.TryGet(body.ChedCertificateId ?? "", out var ched) || !ched.Accessible)
        {
            return Respond(header, new ProcessedChedInformationResponseType());
        }

        var lines = CommodityLine.Of(ched.Source);
        var certificate = new CertificateChedType { SPSCertificate = ched.Certificate };

        if (!reserving)
        {
            return Respond(
                header,
                new ProcessedChedInformationResponseType
                {
                    ChedCertificate = certificate,
                    QuantityManagementSummary = Summary(ledger.For(ched.Id).Read(lines)),
                }
            );
        }

        RequestedItem[] requested = [.. items.Select(item => Requested(header, item))];

        if (!Reservable(ched))
        {
            // About the CHED, not any one item, so acceptance names no failed item.
            return Respond(
                header,
                Refused(
                    certificate,
                    ledger.For(ched.Id).Refuse(lines, declaration!, ReservationFailure.InappropriateStatus, null)
                )
            );
        }

        var outcome = ledger
            .For(ched.Id)
            .Reserve(
                lines,
                declaration!,
                requested,
                OfficeOf(body.CompetentCustomsOffice, request.CustomsOfficeReferenceNumber),
                DateTime.UtcNow
            );

        return Respond(
            header,
            outcome.Failure is null
                ? new ProcessedChedInformationResponseType
                {
                    ChedCertificate = certificate,
                    ReservationResult = true,
                    ReservationResultSpecified = true,
                    QuantityManagementSummary = Summary(outcome.Position),
                }
                : Refused(certificate, outcome)
        );
    }

    public Task<ChedClearanceResponse> chedClearanceRequestAsync(ChedClearanceRequest request)
    {
        var header = request.CertexHeader;
        var body = request.ChedClearanceRequest1 ?? throw SimulatorFaults.Customs(header, "Missing request body");
        var mrn = body.CustomsDocumentReference;

        if (string.IsNullOrEmpty(mrn))
        {
            throw SimulatorFaults.Customs(header, "CustomsDocumentReference is required");
        }

        if (!cheds.TryGet(body.ChedCertificateId ?? "", out var ched) || !ched.Accessible)
        {
            return Clearance(header, ClearanceOutcome.NotFound);
        }

        var outcome = body.GoodsClearanceInformation switch
        {
            GoodsClearanceInformationType.Item01 => ledger.For(ched.Id).Release(mrn, DateTime.UtcNow),
            GoodsClearanceInformationType.Item02 => ledger.For(ched.Id).Delete(mrn),
            var other => throw SimulatorFaults.Customs(header, $"Unknown GoodsClearanceInformation '{other}'"),
        };

        // A release still writes off when the CHED has moved on since the reservation, and says so.
        if (outcome == ClearanceOutcome.Executed
            && body.GoodsClearanceInformation == GoodsClearanceInformationType.Item01
            && !Reservable(ched))
        {
            outcome = ClearanceOutcome.ExecutedWithStatusWarning;
        }

        return Clearance(header, outcome);
    }

    public Task<ChedInterventionResponse> chedInterventionRequestAsync(ChedInterventionRequest request) =>
        throw SimulatorFaults.NotImplemented("ChedInterventionRequest");

    public Task<ChedCRFLResponse> chedCRFLRequestAsync(ChedCRFLRequest request) =>
        throw SimulatorFaults.NotImplemented("ChedCRFLRequest");

    /// <summary>
    /// A quantity always travels with its unit. Weight is taken where both are sent, since CHED lines
    /// carry weight unless they have none.
    /// </summary>
    private static RequestedItem Requested(CertexHeaderType? header, ConsignmentItemR6ForReservationType item)
    {
        var (quantity, unit) = item switch
        {
            { NetWeightQuantitySpecified: true, NetWeightUnitOfMeasureSpecified: true } => (
                item.NetWeightQuantity,
                item.NetWeightUnitOfMeasure
            ),
            { NetVolumeQuantitySpecified: true, NetVolumeUnitOfMeasureSpecified: true } => (
                item.NetVolumeQuantity,
                item.NetVolumeUnitOfMeasure
            ),
            _ => throw SimulatorFaults.Customs(header, "Each item needs a net weight or net volume with its unit"),
        };

        if (!int.TryParse(item.GoodsItemNumber, out var goodsItem) || !int.TryParse(item.CertificateLineNumber, out var line))
        {
            throw SimulatorFaults.Customs(header, "GoodsItemNumber and CertificateLineNumber must be numbers");
        }

        return new RequestedItem(goodsItem, line, item.ClassCode ?? "", unit.ToString(), quantity);
    }

    private static bool Reservable(StoredCertificate ched) =>
        StatusOf(ched) is { } status && s_reservableStatuses.Contains(status);

    private static string? StatusOf(StoredCertificate ched) =>
        ched.Certificate.SPSExchangedDocument?.StatusCode?.Value is { } code ? XmlEnums.ToWireValue(code) : null;

    /// <summary>
    /// A refusal still carries the ledger. The failed item names the declaration's goods item, and
    /// the CHED line only when that line exists — acceptance leaves it out for a line mismatch.
    /// </summary>
    private static ProcessedChedInformationResponseType Refused(
        CertificateChedType certificate,
        ReservationOutcome outcome
    ) =>
        new()
        {
            ChedCertificate = certificate,
            QuantityManagementSummary = Summary(outcome.Position),
            ReservationResult = false,
            ReservationResultSpecified = true,
            ReservationFailureReason = outcome.Failure!.Code,
            ReservationFailureConsignmentItem = outcome.FailedItem is not { } item
                ? null
                : new ReservationFailureConsignmentItemType
                {
                    GoodsItemNumber = item.GoodsItemNumber.ToString(),
                    DocumentLineItemNumber = outcome.Failure == ReservationFailure.LineNumbersMismatch
                        ? null
                        : item.CertificateLineNumber.ToString(),
                },
        };

    private static string OfficeOf(CompetentCustomsOfficeType? body, string? header) =>
        body?.ReferenceNumber ?? header ?? "";

    private static QuantityManagementCommoditySummaryEnhanced4ChedR51Type Summary(LedgerPosition position) =>
        new()
        {
            AvailableQuantity =
            [
                .. position.Available.Select(line => new ProductQuantityEnhancedPlusPlusType
                {
                    // TRACES reports the CHED's CN code in the HS subheading field, whatever its length.
                    CommodityCode = new CommodityCodeEnhanced4AvailableType { HarmonizedSystemSubheadingcode = line.CnCode },
                    SwSupportingDocument = new SWSupportingDocumentType
                    {
                        UnitOfMeasure = Unit(line.UnitOfMeasure),
                        Quantity = line.Quantity,
                        CertificateLineNumber = line.Number.ToString(),
                    },
                }),
            ],
            ReservedQuantity = [.. position.Reserved.Select(Allocated)],
            ConsumedQuantity = [.. position.Consumed.Select(Allocated)],
        };

    private static AllocatedProductQuantityByCustomsOfficeEnhanced4ChedR51Type Allocated(Allocation allocation) =>
        new()
        {
            GoodsItemNumber = allocation.GoodsItemNumber.ToString(),
            CommodityCode = new CommodityCodeEnhancedType { HarmonizedSystemSubheadingcode = allocation.ClassCode },
            SwSupportingDocument = new SWSupportingWRoundingDocumentType
            {
                UnitOfMeasure = Unit(allocation.Line.UnitOfMeasure),
                Quantity = new SWSupportingWRoundingDocumentTypeQuantity { Value = allocation.Quantity },
                CertificateLineNumber = allocation.Line.Number.ToString(),
            },
            EventDateTime = allocation.At,
            EventDateTimeSpecified = true,
            CompetentCustomsOffice = new CompetentCustomsOfficeType { ReferenceNumber = allocation.CustomsOffice },
            Item = allocation.Mrn,
            ItemElementName = ItemChoiceType2.MRN,
        };

    private static UniversalUnitOfMeasureType Unit(string code) => Enum.Parse<UniversalUnitOfMeasureType>(code);

    private static Task<ProcessedChedInformationResponse> Respond(
        CertexHeaderType? header,
        ProcessedChedInformationResponseType body
    )
    {
        // Every response acceptance returned carried OperationCode 0, an unknown CHED's included.
        body.OperationCode = NoOperation;
        return Task.FromResult(new ProcessedChedInformationResponse(Echo(header), body));
    }

    /// <summary>Acceptance sends no <c>StatusCode</c> here, on success or refusal.</summary>
    private static Task<ChedClearanceResponse> Clearance(CertexHeaderType? header, ClearanceOutcome outcome) =>
        Task.FromResult(
            new ChedClearanceResponse(
                Echo(header),
                new ChedQuantityManagementOutcomeType
                {
                    QuantityManagementOutcome = outcome.Code,
                    SendingDate = DateTime.UtcNow,
                    OperationCode = NoOperation,
                }
            )
        );

    private static CertexHeaderType Echo(CertexHeaderType? header) =>
        new() { MessageId = header?.MessageId, UniqRequesterPrefix = header?.UniqRequesterPrefix };
}
