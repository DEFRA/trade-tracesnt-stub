using System.Xml.Serialization;
using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// What every store must keep, whatever holds the certificates. The rest of the suite exercises the
/// stores through the ports; these pin the behaviours a port would not notice until much later.
/// </summary>
public abstract class CertificateStoreTests(ISimulatorState state)
{
    public sealed class InMemory() : CertificateStoreTests(new InMemoryState());

    public sealed class Mongo(MongoFixture mongo) : CertificateStoreTests(mongo.NewState());

    private const string Id = "CHEDA.XI.2026.0000001";

    private static readonly SpsCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    private static readonly XmlSerializer s_xml = new(
        typeof(SPSCertificateType),
        new XmlRootAttribute("SPSCertificate")
    );

    private readonly IChedStore _store = state.Cheds;

    [Fact]
    public async Task AnIdIsItsPrefixCountryAndYearThenTheSerial()
    {
        (await _store.NextIdAsync("CHEDP")).Should().Be($"CHEDP.XI.{DateTime.UtcNow.Year}.0000001");
    }

    [Fact]
    public async Task EachPrefixCountsOnItsOwn()
    {
        // The serial counts per prefix, country and year (confirmed on CDMS-1681), not per store.
        string[] ids =
        [
            await _store.NextIdAsync("CHEDA"),
            await _store.NextIdAsync("CHEDP"),
            await _store.NextIdAsync("CHEDA"),
        ];

        ids.Select(Serial).Should().Equal(1, 1, 2);
    }

    [Fact]
    public async Task ClearingDoesNotRewindTheSerial()
    {
        var before = await _store.NextIdAsync("CHEDA");
        await _store.ClearAsync();

        var after = await _store.NextIdAsync("CHEDA");

        Serial(after).Should().Be(Serial(before) + 1, "a reset must never re-issue an ID a caller still holds");
    }

    [Fact]
    public async Task AnotherInstanceCarriesOnFromTheLastIdIssued()
    {
        var first = await _store.NextIdAsync("CHEDA");

        var next = await state.Another().Cheds.NextIdAsync("CHEDA");

        Serial(next).Should().Be(Serial(first) + 1);
    }

    [Fact]
    public async Task InstancesIssuingAtOnceNeverIssueTheSameId()
    {
        var stores = Enumerable.Range(0, 8).Select(_ => state.Another().Cheds).ToList();

        var ids = await Task.WhenAll(
            stores.SelectMany(store => Enumerable.Range(0, 10).Select(_ => Task.Run(() => store.NextIdAsync("CHEDA"))))
        );

        ids.Should().OnlyHaveUniqueItems().And.HaveCount(80);
    }

    [Fact]
    public async Task ACertificateReadsBackExactlyAsStored()
    {
        // Read twice, because the update time search filters on must not move between reads.
        var stored = AChed();
        await _store.PutAsync(stored);

        var first = (await _store.FindAsync(Id))!;
        var second = (await _store.FindAsync(Id))!;

        // Compared as a consumer reads it off the wire. XmlSerializer reads an empty text node back as
        // null, so <Content></Content> comes back as <Content />: the same element to any XML reader.
        Served(first).Should().Be(Served(stored));
        Served(second).Should().Be(Served(stored));
        first.Source.Should().BeEquivalentTo(stored.Source);
        first.Accessible.Should().BeFalse();
        first.Id.Should().Be(Id);
    }

    [Fact]
    public async Task IdsAreLookedUpIgnoringCase()
    {
        await _store.PutAsync(AChed());

        (await _store.FindAsync(Id.ToLowerInvariant())).Should().NotBeNull();
        (await _store.RemoveAsync(Id.ToLowerInvariant())).Should().BeTrue();
        (await _store.FindAsync(Id)).Should().BeNull();
    }

    [Fact]
    public async Task ClearingRemovesEveryCertificate()
    {
        await _store.PutAsync(AChed());

        await _store.ClearAsync();

        (await _store.CountAsync()).Should().Be(0);
        (await _store.FindAsync(Id)).Should().BeNull();
    }

    private static int Serial(string id) => int.Parse(id.Split('.')[^1]);

    private static string Xml(SPSCertificateType certificate)
    {
        using var writer = new StringWriter();
        s_xml.Serialize(writer, certificate);
        return writer.ToString();
    }

    /// <summary>The certificate as a consumer that read it off the wire would serialise it again.</summary>
    private static string Served(StoredCertificate stored)
    {
        using var reader = new StringReader(Xml(stored.Certificate));
        return Xml((SPSCertificateType)s_xml.Deserialize(reader)!);
    }

    private static StoredCertificate AChed()
    {
        var model = new CertificateControlModel
        {
            Status = "VALIDATED",
            Accessible = false,
            ExchangedDocument = new ExchangedDocumentModel
            {
                IncludedNote = new Dictionary<string, string> { ["CHED_TYPE"] = "A" },
                Declaration = new AuthenticationModel
                {
                    ActualDateTime = new DateTimeOffset(2026, 4, 22, 12, 0, 0, TimeSpan.FromHours(2)),
                    IncludedClause = new Dictionary<string, string> { ["PURPOSE"] = "FREE_CIRCULATION" },
                },
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
                        new TradeLineItemModel
                        {
                            ApplicableClassification = new Dictionary<string, string> { ["CN"] = "0101" },
                            OriginCountry = "AF",
                            NetVolume = new MeasureModel { Value = 10, UnitCode = "H87" },
                        },
                    ],
                },
            },
        };

        return new StoredCertificate(Id, s_builder.Build(CertificateKind.Ched, model, Id), false, model);
    }
}
