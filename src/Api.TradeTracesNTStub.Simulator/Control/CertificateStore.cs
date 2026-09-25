using System.Collections.Concurrent;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// The certificates of one kind the simulator is currently serving. In memory, so a restart is a
/// reset. Each kind has its own store, so a port can only ever see its own certificates.
/// </summary>
public abstract class CertificateStore(string countryCode)
{
    private readonly ConcurrentDictionary<string, StoredCertificate> _certificates = new(
        StringComparer.OrdinalIgnoreCase
    );

    private int _serial;

    public int Count => _certificates.Count;

    public IReadOnlyCollection<StoredCertificate> All => [.. _certificates.Values];

    public string NextId(string prefix) =>
        $"{prefix}.{countryCode}.{DateTime.UtcNow.Year}.{Interlocked.Increment(ref _serial):D7}";

    public void Put(StoredCertificate certificate) => _certificates[certificate.Id] = certificate;

    public bool TryGet(string id, out StoredCertificate certificate) =>
        _certificates.TryGetValue(id, out certificate!);

    public bool Remove(string id) => _certificates.TryRemove(id, out _);

    /// <summary>
    /// Leaves the serial where it is. One simulator serves every client pointed at it, so rewinding
    /// would re-issue an ID a caller still holds, and a certificate it expects to be gone would answer instead.
    /// </summary>
    public void Clear() => _certificates.Clear();
}

public sealed class ChedStore(string countryCode = "XI") : CertificateStore(countryCode);

public sealed class IntraStore(string countryCode = "XI") : CertificateStore(countryCode);

/// <summary>
/// <paramref name="Accessible"/> is simulator state, not certificate content: it decides whether the
/// SOAP face serves this certificate or refuses it. <paramref name="Source"/> is what the client sent,
/// kept so a PATCH has something to merge into, and never served.
/// </summary>
public record StoredCertificate(
    string Id,
    SPSCertificateType Certificate,
    bool Accessible,
    CertificateControlModel Source
);
