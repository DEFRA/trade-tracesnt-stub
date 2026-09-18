using System.Collections.Concurrent;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// The CHEDs the simulator is currently serving. In memory, so a restart is a reset.
/// </summary>
public sealed class ChedStore(string countryCode = "XI")
{
    private readonly ConcurrentDictionary<string, StoredChed> _cheds = new(StringComparer.OrdinalIgnoreCase);

    private int _serial;

    public IReadOnlyCollection<string> Ids => [.. _cheds.Keys];

    public IReadOnlyCollection<StoredChed> All => [.. _cheds.Values];

    public int Count => _cheds.Count;

    public string NextId(string chedType) =>
        $"CHED{chedType}.{countryCode}.{DateTime.UtcNow.Year}.{Interlocked.Increment(ref _serial):D7}";

    public void Put(StoredChed ched) => _cheds[ched.Id] = ched;

    public bool TryGet(string id, out StoredChed ched) => _cheds.TryGetValue(id, out ched!);

    public bool Remove(string id) => _cheds.TryRemove(id, out _);

    /// <summary>
    /// Leaves the serial where it is. Test classes run in parallel, so rewinding it would re-issue
    /// an ID another test still holds, and a CHED it expects to be gone would answer instead.
    /// </summary>
    public void Clear() => _cheds.Clear();
}

/// <summary>
/// <paramref name="Accessible"/> is simulator state, not certificate content: it decides whether the
/// SOAP face serves this CHED or refuses it. <paramref name="Source"/> is what the client sent, kept
/// so a PATCH has something to merge into, and never served.
/// </summary>
public record StoredChed(string Id, SPSCertificateType Certificate, bool Accessible, ChedControlModel Source);
