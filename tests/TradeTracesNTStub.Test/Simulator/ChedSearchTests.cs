using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Ports;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// Search is the operation a real TRACES cannot be made to exercise, because we do not control the
/// data behind it: an offset past the end, a page larger than the result set, a range matching
/// nothing. These pin those boundaries, and pin that paging is stable — a caller walking the pages
/// must see every match once, never twice and never not at all.
/// </summary>
public abstract class ChedSearchTests(ISimulatorState state)
{
    public sealed class InMemory() : ChedSearchTests(new InMemoryState());

    public sealed class Mongo(MongoFixture mongo) : ChedSearchTests(mongo.NewState());

    private static readonly SpsCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    private static readonly DateTime s_noon = new(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly IChedStore _store = state.Cheds;

    [Fact]
    public async Task OnlyCertificatesUpdatedInsideTheRangeComeBack()
    {
        await StoreUpdatedAt(s_noon.AddDays(-2), s_noon, s_noon.AddDays(2));

        var results = await Find(Range(from: s_noon.AddDays(-1), to: s_noon.AddDays(1)));

        results.Select(result => result.UpdateDateTime).Should().Equal(s_noon);
    }

    [Fact]
    public async Task BothBoundsAreInclusive()
    {
        // A CHED updated exactly on a bound is in the range. The gateway pages by feeding one call's
        // end in as the next call's start, so an exclusive bound here would drop it from both.
        await StoreUpdatedAt(s_noon, s_noon.AddDays(1));

        var results = await Find(Range(from: s_noon, to: s_noon.AddDays(1)));

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnOmittedBoundIsNoBound()
    {
        // From and To have no Specified companion, so an omitted one arrives as 0001-01-01 rather
        // than as nothing. Read literally, an omitted To would match no certificate ever stored.
        await StoreUpdatedAt(s_noon.AddDays(-2), s_noon, s_noon.AddDays(2));

        (await Find(Range(from: s_noon))).Should().HaveCount(2);
        (await Find(Range(to: s_noon))).Should().HaveCount(2);
        (await Find(new DateTimeRange())).Should().HaveCount(3);
    }

    [Fact]
    public async Task ARequestWithNoRangeAtAllSearchesEverything()
    {
        await StoreUpdatedAt(s_noon.AddDays(-2), s_noon, s_noon.AddDays(2));

        (await Find(range: null)).Should().HaveCount(3);
    }

    [Fact]
    public async Task PagingTheWholeResultSetReturnsEveryMatchExactlyOnce()
    {
        // Two of the five share an update time: that is the case where an unstable sort would swap
        // them between calls, and the caller would see one twice and the other never.
        var stored = await StoreUpdatedAt(
            s_noon,
            s_noon,
            s_noon.AddHours(1),
            s_noon.AddHours(2),
            s_noon.AddHours(3)
        );

        var walked = new List<string>();

        for (var offset = 1; offset <= 5; offset += 2)
        {
            walked.AddRange((await Find(pageSize: 2, offset: offset)).Select(result => result.ID));
        }

        walked.Should().HaveCount(5).And.OnlyHaveUniqueItems();
        walked.Should().BeEquivalentTo(stored);
    }

    [Fact]
    public async Task BoundsAreExactToTheTick()
    {
        // A store that kept only milliseconds would round this to noon, and find it in a range the
        // search result's own UpdateDateTime shows it is outside.
        await StoreUpdatedAt(s_noon.AddTicks(4));

        (await Find(Range(to: s_noon.AddTicks(3)))).Should().BeEmpty();
        (await Find(Range(from: s_noon.AddTicks(4)))).Should().HaveCount(1);
        (await Find(Range(from: s_noon.AddTicks(5)))).Should().BeEmpty();
    }

    [Fact]
    public async Task ResultsComeBackNewestFirst()
    {
        await StoreUpdatedAt(s_noon, s_noon.AddHours(2), s_noon.AddHours(1));

        var results = await Find();

        results
            .Select(result => result.UpdateDateTime)
            .Should()
            .Equal(s_noon.AddHours(2), s_noon.AddHours(1), s_noon);
    }

    [Fact]
    public async Task AnOffsetPastTheEndIsAnEmptyPageNotAnError()
    {
        await StoreUpdatedAt(s_noon, s_noon.AddHours(1));

        (await Find(pageSize: 2, offset: 3)).Should().BeEmpty();
        (await Find(pageSize: 2, offset: 500)).Should().BeEmpty();
    }

    [Fact]
    public async Task APageLargerThanTheResultSetReturnsAllOfIt()
    {
        await StoreUpdatedAt(s_noon, s_noon.AddHours(1));

        (await Find(pageSize: 100)).Should().HaveCount(2);
    }

    [Fact]
    public async Task AResultSetThatIsAnExactMultipleOfThePageEndsWithAnEmptyPage()
    {
        await StoreUpdatedAt(s_noon, s_noon.AddHours(1), s_noon.AddHours(2), s_noon.AddHours(3));

        (await Find(pageSize: 2, offset: 1)).Should().HaveCount(2);
        (await Find(pageSize: 2, offset: 3)).Should().HaveCount(2);
        (await Find(pageSize: 2, offset: 5)).Should().BeEmpty();
    }

    [Fact]
    public async Task ARangeMatchingNothingIsAnEmptyResultNotAFault()
    {
        await StoreUpdatedAt(s_noon, s_noon.AddHours(1));

        var results = await Find(Range(from: s_noon.AddYears(1), to: s_noon.AddYears(2)));

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task TheEchoedPagingTellsTheCallerWhichWindowItGot()
    {
        await StoreUpdatedAt(s_noon, s_noon.AddHours(1), s_noon.AddHours(2));

        var result = await Search(new FindChedCertificateRequestType { pageSize = 2, offset = 2 });

        result.offset.Should().Be(2);
        result.pageSize.Should().Be(2);
        result.ChedCertificateResult.Should().HaveCount(2);
    }

    [Fact]
    public async Task ACertificateTheCallerMayNotSeeIsOutsideEveryRange()
    {
        await _store.PutAsync(AChed("CHEDA.XI.2026.0000001", s_noon, accessible: true));
        await _store.PutAsync(AChed("CHEDA.XI.2026.0000002", s_noon, accessible: false));

        (await Find()).Select(result => result.ID).Should().Equal("CHEDA.XI.2026.0000001");
    }

    private async Task<IReadOnlyList<ChedCertificateQueryResultType>> Find(
        DateTimeRange? range = null,
        int pageSize = 10,
        int offset = 1
    ) =>
        (
            await Search(
                new FindChedCertificateRequestType
                {
                    UpdateDateTimeRange = range,
                    pageSize = pageSize,
                    offset = offset,
                }
            )
        ).ChedCertificateResult;

    private async Task<FindChedCertificateResultType> Search(FindChedCertificateRequestType query) =>
        (
            await new ChedCertificateSimulator(_store).findChedCertificateAsync(
                new FindChedCertificateRequest { FindChedCertificateRequest1 = query }
            )
        ).FindChedCertificateResponse1;

    /// <summary>An omitted bound is left at its default, which is how it arrives off the wire.</summary>
    private static DateTimeRange Range(DateTime from = default, DateTime to = default) =>
        new() { From = from, To = to };

    private async Task<IReadOnlyList<string>> StoreUpdatedAt(params DateTime[] updates)
    {
        var ids = new List<string>();

        for (var i = 0; i < updates.Length; i++)
        {
            ids.Add($"CHEDA.XI.2026.{i + 1:D7}");
            await _store.PutAsync(AChed(ids[^1], updates[i], accessible: true));
        }

        return ids;
    }

    private static StoredCertificate AChed(string id, DateTime updated, bool accessible)
    {
        var certificate = s_builder.Build(CertificateKind.Ched, AChedA, id);

        // The builder stamps the update time with the clock, as TRACES does. Rewriting the note is
        // the only way to place a certificate at a chosen moment, and a range test needs that.
        certificate
            .SPSExchangedDocument.IncludedSPSNote.Single(note =>
                note.SubjectCode?.Value == SpsCertificateBuilder.LastUpdateNoteSubject
            )
            .Content[0]
            .Value = updated.ToString("O");

        return new StoredCertificate(id, certificate, accessible, AChedA);
    }

    private static CertificateControlModel AChedA =>
        new()
        {
            Status = "VALIDATED",
            ExchangedDocument = new ExchangedDocumentModel
            {
                IncludedNote = new Dictionary<string, string> { ["CHED_TYPE"] = "A" },
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
                        },
                    ],
                },
            },
        };
}
