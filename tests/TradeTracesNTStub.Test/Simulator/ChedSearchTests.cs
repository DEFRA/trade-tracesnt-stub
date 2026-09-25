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
public class ChedSearchTests
{
    private static readonly SpsCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    private static readonly DateTime s_noon = new(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void OnlyCertificatesUpdatedInsideTheRangeComeBack()
    {
        var store = AStoreUpdatedAt(s_noon.AddDays(-2), s_noon, s_noon.AddDays(2));

        var results = Find(store, Range(from: s_noon.AddDays(-1), to: s_noon.AddDays(1)));

        results.Select(result => result.UpdateDateTime).Should().Equal(s_noon);
    }

    [Fact]
    public void BothBoundsAreInclusive()
    {
        // A CHED updated exactly on a bound is in the range. The gateway pages by feeding one call's
        // end in as the next call's start, so an exclusive bound here would drop it from both.
        var store = AStoreUpdatedAt(s_noon, s_noon.AddDays(1));

        var results = Find(store, Range(from: s_noon, to: s_noon.AddDays(1)));

        results.Should().HaveCount(2);
    }

    [Fact]
    public void AnOmittedBoundIsNoBound()
    {
        // From and To have no Specified companion, so an omitted one arrives as 0001-01-01 rather
        // than as nothing. Read literally, an omitted To would match no certificate ever stored.
        var store = AStoreUpdatedAt(s_noon.AddDays(-2), s_noon, s_noon.AddDays(2));

        Find(store, Range(from: s_noon)).Should().HaveCount(2);
        Find(store, Range(to: s_noon)).Should().HaveCount(2);
        Find(store, new DateTimeRange()).Should().HaveCount(3);
    }

    [Fact]
    public void ARequestWithNoRangeAtAllSearchesEverything()
    {
        var store = AStoreUpdatedAt(s_noon.AddDays(-2), s_noon, s_noon.AddDays(2));

        Find(store, range: null).Should().HaveCount(3);
    }

    [Fact]
    public void PagingTheWholeResultSetReturnsEveryMatchExactlyOnce()
    {
        // Two of the five share an update time: that is the case where an unstable sort would swap
        // them between calls, and the caller would see one twice and the other never.
        var store = AStoreUpdatedAt(
            s_noon,
            s_noon,
            s_noon.AddHours(1),
            s_noon.AddHours(2),
            s_noon.AddHours(3)
        );

        var walked = new List<string>();

        for (var offset = 1; offset <= 5; offset += 2)
        {
            walked.AddRange(Find(store, pageSize: 2, offset: offset).Select(result => result.ID));
        }

        walked.Should().HaveCount(5).And.OnlyHaveUniqueItems();
        walked.Should().BeEquivalentTo(store.All.Select(ched => ched.Id));
    }

    [Fact]
    public void ResultsComeBackNewestFirst()
    {
        var store = AStoreUpdatedAt(s_noon, s_noon.AddHours(2), s_noon.AddHours(1));

        var results = Find(store);

        results
            .Select(result => result.UpdateDateTime)
            .Should()
            .Equal(s_noon.AddHours(2), s_noon.AddHours(1), s_noon);
    }

    [Fact]
    public void AnOffsetPastTheEndIsAnEmptyPageNotAnError()
    {
        var store = AStoreUpdatedAt(s_noon, s_noon.AddHours(1));

        Find(store, pageSize: 2, offset: 3).Should().BeEmpty();
        Find(store, pageSize: 2, offset: 500).Should().BeEmpty();
    }

    [Fact]
    public void APageLargerThanTheResultSetReturnsAllOfIt()
    {
        var store = AStoreUpdatedAt(s_noon, s_noon.AddHours(1));

        Find(store, pageSize: 100).Should().HaveCount(2);
    }

    [Fact]
    public void AResultSetThatIsAnExactMultipleOfThePageEndsWithAnEmptyPage()
    {
        var store = AStoreUpdatedAt(s_noon, s_noon.AddHours(1), s_noon.AddHours(2), s_noon.AddHours(3));

        Find(store, pageSize: 2, offset: 1).Should().HaveCount(2);
        Find(store, pageSize: 2, offset: 3).Should().HaveCount(2);
        Find(store, pageSize: 2, offset: 5).Should().BeEmpty();
    }

    [Fact]
    public void ARangeMatchingNothingIsAnEmptyResultNotAFault()
    {
        var store = AStoreUpdatedAt(s_noon, s_noon.AddHours(1));

        var results = Find(store, Range(from: s_noon.AddYears(1), to: s_noon.AddYears(2)));

        results.Should().BeEmpty();
    }

    [Fact]
    public void TheEchoedPagingTellsTheCallerWhichWindowItGot()
    {
        var store = AStoreUpdatedAt(s_noon, s_noon.AddHours(1), s_noon.AddHours(2));

        var result = Search(store, new FindChedCertificateRequestType { pageSize = 2, offset = 2 });

        result.offset.Should().Be(2);
        result.pageSize.Should().Be(2);
        result.ChedCertificateResult.Should().HaveCount(2);
    }

    [Fact]
    public void ACertificateTheCallerMayNotSeeIsOutsideEveryRange()
    {
        var store = new ChedStore();
        store.Put(AChed("CHEDA.XI.2026.0000001", s_noon, accessible: true));
        store.Put(AChed("CHEDA.XI.2026.0000002", s_noon, accessible: false));

        Find(store).Select(result => result.ID).Should().Equal("CHEDA.XI.2026.0000001");
    }

    private static IReadOnlyList<ChedCertificateQueryResultType> Find(
        ChedStore store,
        DateTimeRange? range = null,
        int pageSize = 10,
        int offset = 1
    ) =>
        Search(
            store,
            new FindChedCertificateRequestType
            {
                UpdateDateTimeRange = range,
                pageSize = pageSize,
                offset = offset,
            }
        ).ChedCertificateResult;

    private static FindChedCertificateResultType Search(ChedStore store, FindChedCertificateRequestType query) =>
        new ChedCertificateSimulator(store)
            .findChedCertificateAsync(new FindChedCertificateRequest { FindChedCertificateRequest1 = query })
            .Result.FindChedCertificateResponse1;

    /// <summary>An omitted bound is left at its default, which is how it arrives off the wire.</summary>
    private static DateTimeRange Range(DateTime from = default, DateTime to = default) =>
        new() { From = from, To = to };

    private static ChedStore AStoreUpdatedAt(params DateTime[] updates)
    {
        var store = new ChedStore();

        for (var i = 0; i < updates.Length; i++)
        {
            store.Put(AChed($"CHEDA.XI.2026.{i + 1:D7}", updates[i], accessible: true));
        }

        return store;
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
