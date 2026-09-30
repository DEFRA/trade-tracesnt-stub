using System.Collections.Concurrent;
using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// Customs quantity state for every CHED the customs port has touched: which declarations hold
/// which quantities, and which of them have been released. What each line started with is not held
/// here. It comes from the stored CHED's commodities every time, so seeding a CHED through the
/// control API is also seeding its quantities, and the two cannot disagree.
/// </summary>
/// <remarks>
/// Each refusal and outcome here was observed on TRACES acceptance, except releasing against a CHED
/// that has since changed status (outcome 04), which acceptance gave no way to set up. The captures
/// are under <c>tests/TradeTracesNTStub.Test/Simulator/Captures/Customs</c>.
/// </remarks>
public sealed class CustomsLedger
{
    private readonly ConcurrentDictionary<string, ChedLedger> _ledgers = new(StringComparer.OrdinalIgnoreCase);

    public ChedLedger For(string chedId) => _ledgers.GetOrAdd(chedId, _ => new ChedLedger());

    public void Forget(string chedId) => _ledgers.TryRemove(chedId, out _);

    public void Clear() => _ledgers.Clear();
}

public sealed class ChedLedger
{
    // Reserving checks what is available and then takes it. Two declarations racing for the last of
    // a line must not both succeed, so every operation on one CHED runs under this lock.
    private readonly Lock _lock = new();
    private readonly List<Allocation> _allocations = [];

    public LedgerPosition Read(IReadOnlyList<CommodityLine> lines)
    {
        lock (_lock)
        {
            return Position(lines);
        }
    }

    /// <summary>
    /// States the declaration's whole position: a second reservation for the same MRN replaces the
    /// first rather than adding to it. Nothing is taken unless every item passes, and a refusal ends
    /// whatever the declaration held before — see <see cref="Refuse"/>.
    /// </summary>
    public ReservationOutcome Reserve(
        IReadOnlyList<CommodityLine> lines,
        string mrn,
        IReadOnlyList<RequestedItem> items,
        string customsOffice,
        DateTime now
    )
    {
        lock (_lock)
        {
            if (_allocations.Any(a => a.Consumed && SameMrn(a, mrn)))
            {
                return ReservationOutcome.Refused(Position(lines), ReservationFailure.WriteOffExists, null);
            }

            var others = _allocations.Where(a => !SameMrn(a, mrn)).ToList();

            foreach (var item in items)
            {
                var line = lines.FirstOrDefault(l => l.Number == item.CertificateLineNumber);

                var failure = line switch
                {
                    null => ReservationFailure.LineNumbersMismatch,
                    _ when !item.ClassCode.StartsWith(line.CnCode, StringComparison.Ordinal) =>
                        ReservationFailure.CnCodesMismatch,
                    _ when item.UnitOfMeasure != line.UnitOfMeasure => ReservationFailure.MeasurementUnitMismatch,
                    _ when Requested(items, line) > line.Quantity - Allocated(others, line) =>
                        ReservationFailure.QuantitiesInsufficient,
                    _ => null,
                };

                if (failure is not null)
                {
                    return Refused(lines, mrn, failure, item);
                }
            }

            _allocations.RemoveAll(a => SameMrn(a, mrn));
            _allocations.AddRange(
                items.Select(item => new Allocation(
                    mrn,
                    item.GoodsItemNumber,
                    lines.First(l => l.Number == item.CertificateLineNumber),
                    item.ClassCode,
                    item.Quantity,
                    customsOffice,
                    now,
                    Consumed: false
                ))
            );

            return ReservationOutcome.Reserved(Position(lines));
        }
    }

    /// <summary>
    /// A refused reservation still ends the declaration's current hold: TRACES treats the attempt as
    /// the declaration's new position, and that position failed. For refusals decided outside the
    /// ledger, such as the CHED's status.
    /// </summary>
    public ReservationOutcome Refuse(
        IReadOnlyList<CommodityLine> lines,
        string mrn,
        ReservationFailure failure,
        RequestedItem? item
    )
    {
        lock (_lock)
        {
            return Refused(lines, mrn, failure, item);
        }
    }

    private ReservationOutcome Refused(
        IReadOnlyList<CommodityLine> lines,
        string mrn,
        ReservationFailure failure,
        RequestedItem? item
    )
    {
        _allocations.RemoveAll(a => !a.Consumed && SameMrn(a, mrn));
        return ReservationOutcome.Refused(Position(lines), failure, item);
    }

    /// <summary>The declaration has cleared: its reservation becomes consumed and never comes back.</summary>
    public ClearanceOutcome Release(string mrn, DateTime now)
    {
        lock (_lock)
        {
            var reserved = _allocations.Where(a => !a.Consumed && SameMrn(a, mrn)).ToList();

            if (reserved.Count == 0)
            {
                return Unreserved(mrn);
            }

            foreach (var allocation in reserved)
            {
                _allocations[_allocations.IndexOf(allocation)] = allocation with { Consumed = true, At = now };
            }

            return ClearanceOutcome.Executed;
        }
    }

    /// <summary>The declaration is withdrawn: its reservation goes, and the quantity is available again.</summary>
    public ClearanceOutcome Delete(string mrn)
    {
        lock (_lock)
        {
            return _allocations.RemoveAll(a => !a.Consumed && SameMrn(a, mrn)) == 0
                ? Unreserved(mrn)
                : ClearanceOutcome.Executed;
        }
    }

    /// <summary>Nothing is held for this declaration: either it never reserved, or it has already cleared.</summary>
    private ClearanceOutcome Unreserved(string mrn) =>
        _allocations.Any(a => a.Consumed && SameMrn(a, mrn)) ? ClearanceOutcome.AlreadyConsumed : ClearanceOutcome.NotFound;

    private LedgerPosition Position(IReadOnlyList<CommodityLine> lines) =>
        new(
            [.. lines.Select(line => line with { Quantity = line.Quantity - Allocated(_allocations, line) })],
            [.. _allocations.Where(a => !a.Consumed)],
            [.. _allocations.Where(a => a.Consumed)]
        );

    private static decimal Allocated(IEnumerable<Allocation> allocations, CommodityLine line) =>
        allocations.Where(a => a.Line.Number == line.Number).Sum(a => a.Quantity);

    private static decimal Requested(IEnumerable<RequestedItem> items, CommodityLine line) =>
        items.Where(i => i.CertificateLineNumber == line.Number).Sum(i => i.Quantity);

    private static bool SameMrn(Allocation allocation, string mrn) =>
        string.Equals(allocation.Mrn, mrn, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// One commodity on a CHED as customs sees it. Lines are numbered from 1 in the order the CHED
/// lists them, and the quantity is the net weight, or the net volume where there is no weight.
/// </summary>
public record CommodityLine(int Number, string CnCode, string UnitOfMeasure, decimal Quantity)
{
    public static IReadOnlyList<CommodityLine> Of(CertificateControlModel ched) =>
        [
            .. (ched.SpecifiedConsignment.IncludedConsignmentItem?.IncludedTradeLineItem ?? [])
                .Select((item, index) => (item, number: index + 1, measure: item.NetWeight ?? item.NetVolume))
                .Where(line => line.measure?.UnitCode is not null)
                .Select(line => new CommodityLine(
                    line.number,
                    line.item.ApplicableClassification.GetValueOrDefault("CN", ""),
                    line.measure!.UnitCode!,
                    line.measure.Value
                )),
        ];
}

public record RequestedItem(
    int GoodsItemNumber,
    int CertificateLineNumber,
    string ClassCode,
    string UnitOfMeasure,
    decimal Quantity
);

/// <param name="ClassCode">
/// As the declaration gave it, which TRACES reports back rather than the line's own code: a
/// reservation made as <c>040100</c> against a <c>0401</c> line is listed as <c>040100</c>.
/// </param>
public record Allocation(
    string Mrn,
    int GoodsItemNumber,
    CommodityLine Line,
    string ClassCode,
    decimal Quantity,
    string CustomsOffice,
    DateTime At,
    bool Consumed
);

/// <summary>What each line has left, and what is held against it.</summary>
public record LedgerPosition(
    IReadOnlyList<CommodityLine> Available,
    IReadOnlyList<Allocation> Reserved,
    IReadOnlyList<Allocation> Consumed
);

/// <summary>A code from the <c>ReservationFailureReason</c> list in the CERTEX guidelines.</summary>
public record ReservationFailure(string Code)
{
    public static readonly ReservationFailure CnCodesMismatch = new("03");
    public static readonly ReservationFailure InappropriateStatus = new("04");
    public static readonly ReservationFailure QuantitiesInsufficient = new("05");
    public static readonly ReservationFailure WriteOffExists = new("06");
    public static readonly ReservationFailure LineNumbersMismatch = new("07");
    public static readonly ReservationFailure MeasurementUnitMismatch = new("10");
}

/// <summary>
/// A refusal still reports the ledger as it stands. <paramref name="FailedItem"/> is absent when the
/// refusal is about the whole declaration rather than one of its items.
/// </summary>
public record ReservationOutcome(LedgerPosition Position, ReservationFailure? Failure, RequestedItem? FailedItem)
{
    public static ReservationOutcome Reserved(LedgerPosition position) => new(position, null, null);

    public static ReservationOutcome Refused(
        LedgerPosition position,
        ReservationFailure failure,
        RequestedItem? item
    ) => new(position, failure, item);
}

/// <summary>A code from the <c>QuantityManagementOutcome</c> list in the CERTEX guidelines.</summary>
public record ClearanceOutcome(string Code)
{
    public static readonly ClearanceOutcome Executed = new("01");
    public static readonly ClearanceOutcome NotFound = new("02");
    public static readonly ClearanceOutcome AlreadyConsumed = new("03");

    /// <summary>Written off all the same, but the CHED was no longer in a state to clear against.</summary>
    public static readonly ClearanceOutcome ExecutedWithStatusWarning = new("04");
}
