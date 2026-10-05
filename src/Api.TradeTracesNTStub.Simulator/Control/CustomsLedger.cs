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
public interface ICustomsLedger
{
    /// <summary>
    /// Runs one operation on a CHED's ledger as a unit. Reserving checks what is available and then
    /// takes it, so two declarations racing for the last of a line must not both succeed: no other
    /// change to this CHED's ledger may land between the operation's read and its write.
    /// </summary>
    /// <remarks>The operation may run more than once, so it must change nothing but the ledger.</remarks>
    Task<T> ApplyAsync<T>(string chedId, Func<ChedLedger, T> operation);

    Task ForgetAsync(string chedId);

    Task ClearAsync();
}

/// <summary>In memory, so a restart is a reset, and every instance of the stub holds its own.</summary>
public sealed class InMemoryCustomsLedger : ICustomsLedger
{
    private readonly ConcurrentDictionary<string, Held> _ledgers = new(StringComparer.OrdinalIgnoreCase);

    public Task<T> ApplyAsync<T>(string chedId, Func<ChedLedger, T> operation)
    {
        var held = _ledgers.GetOrAdd(chedId, _ => new Held(new ChedLedger([])));

        lock (held.Lock)
        {
            return Task.FromResult(operation(held.Ledger));
        }
    }

    public Task ForgetAsync(string chedId)
    {
        _ledgers.TryRemove(chedId, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _ledgers.Clear();
        return Task.CompletedTask;
    }

    private sealed record Held(ChedLedger Ledger)
    {
        public Lock Lock { get; } = new();
    }
}

/// <summary>
/// The quantity rules for one CHED, over its allocations. Not safe to share between threads: whatever
/// stores the allocations runs each operation as a unit (<see cref="ICustomsLedger.ApplyAsync{T}"/>).
/// </summary>
public sealed class ChedLedger(IEnumerable<Allocation> allocations)
{
    private readonly List<Allocation> _allocations = [.. allocations];

    public IReadOnlyList<Allocation> Allocations => _allocations;

    public LedgerPosition Read(IReadOnlyList<CommodityLine> lines) => Position(lines);

    /// <summary>
    /// States the declaration's whole position: a second reservation for the same MRN replaces the
    /// first rather than adding to it. Nothing is taken unless every item passes. A refusal for want of
    /// quantity also ends what the declaration held before; any other refusal leaves it alone.
    /// </summary>
    public ReservationOutcome Reserve(
        IReadOnlyList<CommodityLine> lines,
        string mrn,
        IReadOnlyList<RequestedItem> items,
        string customsOffice,
        DateTime now
    )
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
                _ when !MeasurementUnit.TryConvert(item.Quantity, item.UnitOfMeasure, line.UnitOfMeasure, out _) =>
                    ReservationFailure.MeasurementUnitMismatch,
                _ when Requested(items, line) > line.Quantity - Allocated(others, line) =>
                    ReservationFailure.QuantitiesInsufficient,
                _ => null,
            };

            if (failure is not null)
            {
                return Refuse(lines, mrn, failure, item);
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
                item.UnitOfMeasure,
                customsOffice,
                now,
                Consumed: false
            ))
        );

        return ReservationOutcome.Reserved(Position(lines));
    }

    /// <summary>Also for refusals decided outside the ledger, such as the CHED's status.</summary>
    public ReservationOutcome Refuse(
        IReadOnlyList<CommodityLine> lines,
        string mrn,
        ReservationFailure failure,
        RequestedItem? item
    )
    {
        // Acceptance drops the declaration's old hold when the new one does not fit (05), but keeps it
        // when the request fails a check on its commodity, line or unit (03, 07, 10): only a request
        // that got as far as the quantities has replaced anything.
        if (failure == ReservationFailure.QuantitiesInsufficient)
        {
            _allocations.RemoveAll(a => !a.Consumed && SameMrn(a, mrn));
        }

        return ReservationOutcome.Refused(Position(lines), failure, item);
    }

    /// <summary>The declaration has cleared: its reservation becomes consumed and never comes back.</summary>
    public ClearanceOutcome Release(string mrn, DateTime now)
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

    /// <summary>The declaration is withdrawn: its reservation goes, and the quantity is available again.</summary>
    public ClearanceOutcome Delete(string mrn) =>
        _allocations.RemoveAll(a => !a.Consumed && SameMrn(a, mrn)) == 0
            ? Unreserved(mrn)
            : ClearanceOutcome.Executed;

    /// <summary>Nothing is held for this declaration: either it never reserved, or it has already cleared.</summary>
    private ClearanceOutcome Unreserved(string mrn) =>
        _allocations.Any(a => a.Consumed && SameMrn(a, mrn))
            ? ClearanceOutcome.AlreadyConsumed
            : ClearanceOutcome.NotFound;

    private LedgerPosition Position(IReadOnlyList<CommodityLine> lines) =>
        new(
            [.. lines.Select(line => line with { Quantity = line.Quantity - Allocated(_allocations, line) })],
            [.. _allocations.Where(a => !a.Consumed)],
            [.. _allocations.Where(a => a.Consumed)]
        );

    private static decimal Allocated(IEnumerable<Allocation> allocations, CommodityLine line) =>
        allocations.Where(a => a.Line.Number == line.Number).Sum(a => a.LineQuantity);

    /// <summary>
    /// What the declaration asks of this line, in the line's own unit. An item in a unit that cannot
    /// convert is left out here; it is refused on its own account.
    /// </summary>
    private static decimal Requested(IEnumerable<RequestedItem> items, CommodityLine line) =>
        items
            .Where(i => i.CertificateLineNumber == line.Number)
            .Sum(item =>
                MeasurementUnit.TryConvert(item.Quantity, item.UnitOfMeasure, line.UnitOfMeasure, out var converted)
                    ? converted
                    : 0m
            );

    private static bool SameMrn(Allocation allocation, string mrn) =>
        string.Equals(allocation.Mrn, mrn, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// One commodity on a CHED as customs sees it. Lines are numbered from 1 in the order the CHED
/// lists them. The quantity is the net weight, or the net volume where the weight carries no unit:
/// a CHED-A counts its animals as a net volume in pieces (<c>H87</c>) beside a unitless weight of 0.
/// </summary>
public record CommodityLine(int Number, string CnCode, string UnitOfMeasure, decimal Quantity)
{
    public static IReadOnlyList<CommodityLine> Of(CertificateControlModel ched) =>
        [
            .. (ched.SpecifiedConsignment.IncludedConsignmentItem?.IncludedTradeLineItem ?? [])
                .Select((item, index) => (item, number: index + 1, measure: MeasureOf(item)))
                .Where(line => line.measure?.UnitCode is not null)
                .Select(line => new CommodityLine(
                    line.number,
                    line.item.ApplicableClassification.GetValueOrDefault("CN", ""),
                    line.measure!.UnitCode!,
                    line.measure.Value
                )),
        ];

    private static MeasureModel? MeasureOf(TradeLineItemModel item) =>
        item.NetWeight?.UnitCode is null ? item.NetVolume : item.NetWeight;
}

public record RequestedItem(
    int GoodsItemNumber,
    int CertificateLineNumber,
    string ClassCode,
    string UnitOfMeasure,
    decimal Quantity
);

/// <param name="ClassCode">
/// As the declaration gave it. TRACES reports it back, as a six-digit HS subheading, rather than the
/// line's own code: <c>040100</c> against a <c>0401</c> line is listed as <c>040100</c>.
/// </param>
/// <param name="Quantity">
/// As the declaration gave it, in <paramref name="UnitOfMeasure"/>: TRACES reports 1 g against a
/// kilogram line as 1 GRM, and takes 0.001 kg from the line.
/// </param>
public record Allocation(
    string Mrn,
    int GoodsItemNumber,
    CommodityLine Line,
    string ClassCode,
    decimal Quantity,
    string UnitOfMeasure,
    string CustomsOffice,
    DateTime At,
    bool Consumed
)
{
    /// <summary>The same quantity in the line's unit, which is what comes off the line.</summary>
    public decimal LineQuantity =>
        MeasurementUnit.TryConvert(Quantity, UnitOfMeasure, Line.UnitOfMeasure, out var converted)
            ? converted
            : throw new InvalidOperationException($"{UnitOfMeasure} was reserved against a {Line.UnitOfMeasure} line");
}

/// <summary>
/// Which units a declaration may mix against a CHED line. TRACES converts between grams, kilograms and
/// tonnes, and counts pieces (<c>H87</c>) on their own. Any other code converts only to itself.
/// </summary>
public static class MeasurementUnit
{
    private static readonly Dictionary<string, (string Family, decimal Factor)> s_units = new()
    {
        ["GRM"] = ("mass", 0.001m),
        ["KGM"] = ("mass", 1m),
        ["TNE"] = ("mass", 1000m),
        ["H87"] = ("pieces", 1m),
    };

    public static bool TryConvert(decimal quantity, string from, string to, out decimal converted)
    {
        var (fromFamily, fromFactor) = s_units.GetValueOrDefault(from, (from, 1m));
        var (toFamily, toFactor) = s_units.GetValueOrDefault(to, (to, 1m));

        converted = fromFamily == toFamily ? quantity * fromFactor / toFactor : 0m;
        return fromFamily == toFamily;
    }
}

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
