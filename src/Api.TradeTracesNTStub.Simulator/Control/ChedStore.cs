using System.Collections.Concurrent;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// The CHEDs the simulator is currently serving.
/// </summary>
/// <remarks>
/// In memory, and deliberately so: a test resets it rather than cleaning up after itself, and a
/// restart is a reset. Certificates are held as the deserialised TRACES graph rather than the control
/// model they were built from, which is what keeps the SOAP face the only way to read state back.
/// </remarks>
public sealed class ChedStore
{
    private readonly ConcurrentDictionary<string, StoredChed> _cheds = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Ids => [.. _cheds.Keys];

    public int Count => _cheds.Count;

    public void Put(StoredChed ched) => _cheds[ched.Id] = ched;

    public bool TryGet(string id, out StoredChed ched) => _cheds.TryGetValue(id, out ched!);

    public bool Remove(string id) => _cheds.TryRemove(id, out _);

    public void Clear() => _cheds.Clear();
}

/// <summary>
/// One stored CHED. <paramref name="Accessible"/> is simulator state rather than part of the
/// certificate: it decides whether the SOAP face serves this CHED or refuses it, which is how a test
/// covers the permission-denied path without a magic ID.
/// </summary>
public record StoredChed(string Id, SPSCertificateType Certificate, bool Accessible);
