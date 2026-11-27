using Api.TradeTracesNTStub.Simulator.Control;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Api.TradeTracesNTStub.Utils.Mongo;

/// <summary>
/// One versioned document per CHED. An operation reads the document, runs the ledger's rules over it,
/// and writes back only if nobody else has written since; otherwise it starts again from a fresh read.
/// That makes check-then-reserve atomic across every instance of the stub, without a transaction.
/// </summary>
public sealed class MongoCustomsLedger(IMongoDbClientFactory mongo) : ICustomsLedger
{
    // Enough for a handful of instances contending for one CHED; more than that is a bug, not load.
    private const int MaxAttempts = 10;

    private readonly IMongoCollection<LedgerDocument> _ledgers = mongo.GetCollection<LedgerDocument>("ledgers");

    public async Task<T> ApplyAsync<T>(string chedId, Func<ChedLedger, T> operation)
    {
        var key = chedId.ToUpperInvariant();

        for (var attempt = 1; ; attempt++)
        {
            var stored = await _ledgers.Find(Is(key)).FirstOrDefaultAsync();
            List<Allocation> before = [.. stored?.Allocations.Select(FromDocument) ?? []];
            var ledger = new ChedLedger(before);

            var result = operation(ledger);

            // A read, or a refusal that took nothing away, has nothing to write.
            if (ledger.Allocations.SequenceEqual(before) || await TryWrite(key, stored?.Version, ledger.Allocations))
            {
                return result;
            }

            if (attempt == MaxAttempts)
            {
                throw new InvalidOperationException(
                    $"The customs ledger for {chedId} changed under {MaxAttempts} attempts in a row"
                );
            }
        }
    }

    public Task ForgetAsync(string chedId) => _ledgers.DeleteOneAsync(Is(chedId.ToUpperInvariant()));

    public Task ClearAsync() => _ledgers.DeleteManyAsync(FilterDefinition<LedgerDocument>.Empty);

    /// <summary>False when another write landed between the read at <paramref name="version"/> and this one.</summary>
    private async Task<bool> TryWrite(string key, int? version, IReadOnlyList<Allocation> allocations)
    {
        var document = new LedgerDocument(key, (version ?? 0) + 1, [.. allocations.Select(ToDocument)]);

        if (version is null)
        {
            try
            {
                await _ledgers.InsertOneAsync(document);
                return true;
            }
            catch (MongoWriteException exception)
                when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        // Not an upsert: a ledger forgotten since the read (its CHED deleted) must stay forgotten.
        var replaced = await _ledgers.ReplaceOneAsync(
            Is(key) & Builders<LedgerDocument>.Filter.Eq(d => d.Version, version),
            document
        );

        return replaced.MatchedCount == 1;
    }

    private static FilterDefinition<LedgerDocument> Is(string key) => Builders<LedgerDocument>.Filter.Eq(d => d.Id, key);

    private static AllocationDocument ToDocument(Allocation allocation) =>
        new(
            allocation.Mrn,
            allocation.GoodsItemNumber,
            allocation.Line.Number,
            allocation.Line.CnCode,
            allocation.Line.UnitOfMeasure,
            allocation.Line.Quantity,
            allocation.ClassCode,
            allocation.Quantity,
            allocation.UnitOfMeasure,
            allocation.CustomsOffice,
            allocation.At,
            allocation.Consumed
        );

    private static Allocation FromDocument(AllocationDocument document) =>
        new(
            document.Mrn,
            document.GoodsItemNumber,
            new CommodityLine(document.LineNumber, document.LineCnCode, document.LineUnitOfMeasure, document.LineQuantity),
            document.ClassCode,
            document.Quantity,
            document.UnitOfMeasure,
            document.CustomsOffice,
            document.At,
            document.Consumed
        );

    private sealed record LedgerDocument(string Id, int Version, List<AllocationDocument> Allocations);

    // Quantities as Decimal128: a double would not hold 0.001 kg exactly, and the ledger subtracts them.
    private sealed record AllocationDocument(
        string Mrn,
        int GoodsItemNumber,
        int LineNumber,
        string LineCnCode,
        string LineUnitOfMeasure,
        [property: BsonRepresentation(BsonType.Decimal128)] decimal LineQuantity,
        string ClassCode,
        [property: BsonRepresentation(BsonType.Decimal128)] decimal Quantity,
        string UnitOfMeasure,
        string CustomsOffice,
        DateTime At,
        bool Consumed
    );
}
