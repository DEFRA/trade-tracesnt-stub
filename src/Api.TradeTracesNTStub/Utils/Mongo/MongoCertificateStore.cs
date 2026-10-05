using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Ports;
using MongoDB.Driver;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Utils.Mongo;

/// <summary>
/// Certificates of one kind in their own collection, so state survives a restart and every instance of
/// the stub serves the same certificates.
/// </summary>
/// <remarks>
/// The certificate is kept as the XML the SOAP face serves rather than rebuilt from its source on read.
/// The builder stamps the update time as it builds and search filters on that time, so a rebuild would
/// move every certificate each time it was read. Keeping the XML also means a seed-data change cannot
/// alter, or break, a certificate already stored.
/// </remarks>
public sealed class MongoCertificateStore(IMongoDbClientFactory mongo, string collection, string countryCode = "XI")
    : IChedStore, IIntraStore
{
    public const string Cheds = "cheds";
    public const string Intras = "intras";

    // Built once: an XmlSerializer given a root override generates an assembly per instance, and those
    // are never unloaded. The root name only has to agree between write and read.
    private static readonly XmlSerializer s_xml = new(
        typeof(SPSCertificateType),
        new XmlRootAttribute("SPSCertificate")
    );

    // As the control API reads it, so the source comes back exactly as the client sent it.
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IMongoCollection<CertificateDocument> _certificates =
        mongo.GetCollection<CertificateDocument>(collection);

    private readonly IMongoCollection<Serial> _serials = mongo.GetCollection<Serial>("serials");

    public async Task<string> NextIdAsync(string prefix)
    {
        var scope = CertificateId.Scope(prefix, countryCode);

        // Counted in Mongo rather than in the process, so a restart or a second instance carries on
        // from the last ID issued instead of issuing it again. One document per scope, so CHED and
        // INTRA share the collection without sharing a count.
        var serial = await _serials.FindOneAndUpdateAsync(
            Builders<Serial>.Filter.Eq(s => s.Id, scope),
            Builders<Serial>.Update.Inc(s => s.Value, 1),
            new FindOneAndUpdateOptions<Serial> { IsUpsert = true, ReturnDocument = ReturnDocument.After }
        );

        return CertificateId.Issue(scope, serial.Value);
    }

    public Task PutAsync(StoredCertificate certificate) =>
        _certificates.ReplaceOneAsync(Is(certificate.Id), ToDocument(certificate), new ReplaceOptions { IsUpsert = true });

    public async Task<StoredCertificate?> FindAsync(string id) =>
        await _certificates.Find(Is(id)).FirstOrDefaultAsync() is { } document ? FromDocument(document) : null;

    public async Task<bool> RemoveAsync(string id) => (await _certificates.DeleteOneAsync(Is(id))).DeletedCount > 0;

    public async Task<IReadOnlyList<StoredCertificate>> SearchAsync(
        DateTime? updatedFrom,
        DateTime? updatedTo,
        int skip,
        int take
    )
    {
        var filter = Builders<CertificateDocument>.Filter;
        var visible = filter.Eq(d => d.Accessible, true);

        if (updatedFrom is { } from)
        {
            visible &= filter.Gte(d => d.UpdatedTicks, from.Ticks);
        }

        if (updatedTo is { } to)
        {
            visible &= filter.Lte(d => d.UpdatedTicks, to.Ticks);
        }

        // Filtered, sorted and paged in Mongo, over the index CreateIndexesAsync builds, so a search
        // deserialises one page rather than every certificate stored.
        var page = await _certificates
            .Find(visible)
            .Sort(Builders<CertificateDocument>.Sort.Descending(d => d.UpdatedTicks).Descending(d => d.CertificateId))
            .Skip(skip)
            .Limit(take)
            .ToListAsync();

        return [.. page.Select(FromDocument)];
    }

    /// <summary>
    /// Run at start-up, not per store, so a deploy that cannot reach Mongo fails before it serves anything.
    /// Creating an index that already exists does nothing, so this is safe on every start.
    /// </summary>
    public static Task CreateIndexesAsync(IMongoDbClientFactory mongo, string collection, CancellationToken token) =>
        mongo
            .GetCollection<CertificateDocument>(collection)
            .Indexes.CreateOneAsync(
                // Equality, then the range and sort search uses, then the tie-break: one index serves all three.
                new CreateIndexModel<CertificateDocument>(
                    Builders<CertificateDocument>
                        .IndexKeys.Ascending(d => d.Accessible)
                        .Descending(d => d.UpdatedTicks)
                        .Descending(d => d.CertificateId),
                    new CreateIndexOptions { Name = "search" }
                ),
                cancellationToken: token
            );

    public async Task<int> CountAsync() =>
        (int)await _certificates.CountDocumentsAsync(FilterDefinition<CertificateDocument>.Empty);

    /// <summary>Leaves <c>serials</c> alone, for the reason <see cref="ICertificateStore.ClearAsync"/> gives.</summary>
    public Task ClearAsync() => _certificates.DeleteManyAsync(FilterDefinition<CertificateDocument>.Empty);

    // IDs are looked up ignoring case, and Mongo's _id does not.
    private static FilterDefinition<CertificateDocument> Is(string id) =>
        Builders<CertificateDocument>.Filter.Eq(d => d.Id, id.ToUpperInvariant());

    private static CertificateDocument ToDocument(StoredCertificate certificate)
    {
        using var writer = new StringWriter();
        s_xml.Serialize(writer, certificate.Certificate);

        return new CertificateDocument(
            certificate.Id.ToUpperInvariant(),
            certificate.Id,
            certificate.Accessible,
            ChedSummary.UpdatedAt(certificate.Certificate).Ticks,
            JsonSerializer.Serialize(certificate.Source, s_json),
            writer.ToString()
        );
    }

    private static StoredCertificate FromDocument(CertificateDocument document)
    {
        using var reader = new StringReader(document.Certificate);

        return new StoredCertificate(
            document.CertificateId,
            (SPSCertificateType)s_xml.Deserialize(reader)!,
            document.Accessible,
            JsonSerializer.Deserialize<CertificateControlModel>(document.Source, s_json)!
        );
    }

    /// <param name="UpdatedTicks">
    /// UTC ticks rather than a BSON date, which keeps only milliseconds: a certificate updated at
    /// <c>.0004</c> would land on the wrong side of a bound the SOAP response shows it inside.
    /// </param>
    private sealed record CertificateDocument(
        string Id,
        string CertificateId,
        bool Accessible,
        long UpdatedTicks,
        string Source,
        string Certificate
    );

    private sealed record Serial(string Id, int Value);
}
