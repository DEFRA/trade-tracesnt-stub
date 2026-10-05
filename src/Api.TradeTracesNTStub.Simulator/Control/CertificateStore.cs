using System.Collections.Concurrent;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Ports;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// The certificates of one kind the simulator is currently serving. Each kind has its own store, so a
/// port can only ever see its own certificates. IDs are looked up ignoring case.
/// </summary>
public interface ICertificateStore
{
    /// <summary>
    /// The next ID under <paramref name="prefix"/>, e.g. <c>CHEDP.XI.2026.0000001</c>. The serial counts per
    /// prefix, country and year, so <c>CHEDA.XI.2026</c> and <c>CHEDP.XI.2026</c> each start at 1.
    /// </summary>
    Task<string> NextIdAsync(string prefix);

    Task PutAsync(StoredCertificate certificate);

    Task<StoredCertificate?> FindAsync(string id);

    Task<bool> RemoveAsync(string id);

    /// <summary>
    /// One page of the certificates a caller may see, narrowed to an update-time range, newest first.
    /// Both bounds are UTC, inclusive, and absent when null: nothing DG SANTE publish makes the range
    /// half-open, and inventing an exclusive end would hide a gateway paging bug rather than expose it.
    /// The ID breaks ties, so two certificates stamped in the same tick cannot swap places between two
    /// calls, which would show a caller paging through one of them twice and the other never.
    /// </summary>
    /// <remarks>Not accessible means the caller may not see it, in search as much as in retrieval.</remarks>
    Task<IReadOnlyList<StoredCertificate>> SearchAsync(
        DateTime? updatedFrom,
        DateTime? updatedTo,
        int skip,
        int take
    );

    Task<int> CountAsync();

    /// <summary>
    /// Leaves the serial where it is. One simulator serves every client pointed at it, so rewinding
    /// would re-issue an ID a caller still holds, and a certificate it expects to be gone would answer instead.
    /// </summary>
    Task ClearAsync();
}

// Two types rather than one so dependency injection hands each port only its own kind's store.
public interface IChedStore : ICertificateStore;

public interface IIntraStore : ICertificateStore;

public static class CertificateId
{
    /// <summary>
    /// What a serial counts within: the ID without its serial, e.g. <c>CHEDP.XI.2026</c>. Read once per ID
    /// and used for both the count and the ID, so the year cannot differ between them at midnight on New
    /// Year's Eve, and a new year starts a new count.
    /// </summary>
    public static string Scope(string prefix, string countryCode) => $"{prefix}.{countryCode}.{DateTime.UtcNow.Year}";

    public static string Issue(string scope, int serial) => $"{scope}.{serial:D7}";
}

/// <summary>In memory, so a restart is a reset, and every instance of the stub holds its own.</summary>
public sealed class InMemoryCertificateStore(string countryCode = "XI") : IChedStore, IIntraStore
{
    private readonly ConcurrentDictionary<string, Entry> _certificates = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, int> _serials = new();

    public Task<string> NextIdAsync(string prefix)
    {
        var scope = CertificateId.Scope(prefix, countryCode);
        return Task.FromResult(CertificateId.Issue(scope, _serials.AddOrUpdate(scope, 1, (_, serial) => serial + 1)));
    }

    public Task PutAsync(StoredCertificate certificate)
    {
        _certificates[certificate.Id] = new Entry(certificate, ChedSummary.UpdatedAt(certificate.Certificate));
        return Task.CompletedTask;
    }

    public Task<StoredCertificate?> FindAsync(string id) =>
        Task.FromResult(_certificates.GetValueOrDefault(id)?.Certificate);

    public Task<bool> RemoveAsync(string id) => Task.FromResult(_certificates.TryRemove(id, out _));

    public Task<IReadOnlyList<StoredCertificate>> SearchAsync(
        DateTime? updatedFrom,
        DateTime? updatedTo,
        int skip,
        int take
    ) =>
        Task.FromResult<IReadOnlyList<StoredCertificate>>(
            [
                .. _certificates
                    .Values.Where(entry =>
                        entry.Certificate.Accessible
                        && (updatedFrom is null || entry.Updated >= updatedFrom)
                        && (updatedTo is null || entry.Updated <= updatedTo)
                    )
                    .OrderByDescending(entry => entry.Updated)
                    .ThenByDescending(entry => entry.Certificate.Id, StringComparer.Ordinal)
                    .Skip(skip)
                    .Take(take)
                    .Select(entry => entry.Certificate),
            ]
        );

    public Task<int> CountAsync() => Task.FromResult(_certificates.Count);

    public Task ClearAsync()
    {
        _certificates.Clear();
        return Task.CompletedTask;
    }

    /// <param name="Updated">Read once, as stored, so a certificate cannot move between two searches.</param>
    private sealed record Entry(StoredCertificate Certificate, DateTime Updated);
}

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
