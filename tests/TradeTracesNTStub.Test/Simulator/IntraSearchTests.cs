using Api.TradeTracesNTStub.Simulator.Control;
using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Ports;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// INTRA search shares its store and its rules with CHED, and <see cref="ChedSearchTests"/> pins those
/// against both stores. These pin what the INTRA port adds: that it applies the same protocol rules,
/// and that its summary reads each field back out of the certificate.
/// </summary>
public class IntraSearchTests
{
    private static readonly SpsCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    private static readonly DateTime s_noon = new(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly IIntraStore _store = new InMemoryCertificateStore();

    [Fact]
    public async Task OnlyIntrasUpdatedInsideTheRangeComeBackNewestFirst()
    {
        await StoreUpdatedAt(s_noon.AddDays(-2), s_noon, s_noon.AddHours(1), s_noon.AddDays(2));

        var results = await Find(new DateTimeRange { From = s_noon.AddDays(-1), To = s_noon.AddDays(1) });

        results.Select(result => result.UpdateDateTime).Should().Equal(s_noon.AddHours(1), s_noon);
    }

    [Fact]
    public async Task AnOmittedBoundIsNoBound()
    {
        await StoreUpdatedAt(s_noon.AddDays(-2), s_noon, s_noon.AddDays(2));

        (await Find(new DateTimeRange { From = s_noon })).Should().HaveCount(2);
        (await Find(new DateTimeRange())).Should().HaveCount(3);
        (await Find(range: null)).Should().HaveCount(3);
    }

    [Fact]
    public async Task OffsetsAreOneBased()
    {
        var stored = await StoreUpdatedAt(s_noon, s_noon.AddHours(1));

        (await Find(pageSize: 1, offset: 1)).Select(result => result.ID).Should().Equal(stored[1]);
        (await Find(pageSize: 1, offset: 2)).Select(result => result.ID).Should().Equal(stored[0]);
        (await Find(pageSize: 1, offset: 3)).Should().BeEmpty();
    }

    [Fact]
    public async Task TheEchoedPagingTellsTheCallerWhichWindowItGot()
    {
        await StoreUpdatedAt(s_noon, s_noon.AddHours(1), s_noon.AddHours(2));

        var result = await Search(new FindEuIntraCertificateRequestType { pageSize = 2, offset = 2 });

        result.offset.Should().Be(2);
        result.pageSize.Should().Be(2);
        result.EuIntraCertificateResult.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnIntraTheCallerMayNotSeeIsOutsideEveryRange()
    {
        await _store.PutAsync(AnIntra("INTRA.XI.2026.0000001", s_noon, accessible: true));
        await _store.PutAsync(AnIntra("INTRA.XI.2026.0000002", s_noon, accessible: false));

        (await Find()).Select(result => result.ID).Should().Equal("INTRA.XI.2026.0000001");
    }

    [Fact]
    public async Task TheSummaryIsReadBackFromTheCertificate()
    {
        await StoreUpdatedAt(s_noon);

        var summary = (await Find()).Single();

        summary.ID.Should().Be("INTRA.XI.2026.0000001");
        summary.Status.name.Should().Be("Issued (Validated)");
        summary.CommodityApplicableSPSClassification[0].ClassCode.Value.Should().Be("0101");
        summary.CountryOfOrigin.Select(country => country.Value).Should().Equal("AF");
        summary.CountryOfDispatch!.Value.Should().Be("AF");
        summary.CountryOfDestination!.Value.Should().Be("XI");
        summary.ConsignorName.Should().Be("Simulator Exporters Ltd");
        summary.CountryOfConsignor!.Value.Should().Be("XI");
        summary.DeclarationDateTimeSpecified.Should().BeTrue();
        summary.DeclarationDateTime.Should().Be(new DateTime(2026, 4, 21, 10, 0, 0));
        summary.CertificationDateTimeSpecified.Should().BeFalse();
    }

    private async Task<IReadOnlyList<EuIntraCertificateQueryResultType>> Find(
        DateTimeRange? range = null,
        int pageSize = 10,
        int offset = 1
    ) =>
        (
            await Search(
                new FindEuIntraCertificateRequestType
                {
                    UpdateDateTimeRange = range,
                    pageSize = pageSize,
                    offset = offset,
                }
            )
        ).EuIntraCertificateResult;

    private async Task<FindEuIntraCertificateResultType> Search(FindEuIntraCertificateRequestType query) =>
        (
            await new EuIntraCertificateSimulator(_store).findEuIntraCertificateAsync(
                new FindEuIntraCertificateRequest { FindEuIntraCertificateRequest1 = query }
            )
        ).FindEuIntraCertificateResponse1;

    private async Task<IReadOnlyList<string>> StoreUpdatedAt(params DateTime[] updates)
    {
        var ids = new List<string>();

        for (var i = 0; i < updates.Length; i++)
        {
            ids.Add($"INTRA.XI.2026.{i + 1:D7}");
            await _store.PutAsync(AnIntra(ids[^1], updates[i], accessible: true));
        }

        return ids;
    }

    private static StoredCertificate AnIntra(string id, DateTime updated, bool accessible)
    {
        var certificate = s_builder.Build(CertificateKind.Intra, AnIntraModel, id);

        // The builder stamps the update time with the clock; rewriting the note places it at a chosen moment.
        certificate
            .SPSExchangedDocument.IncludedSPSNote.Single(note =>
                note.SubjectCode?.Value == SpsCertificateBuilder.LastUpdateNoteSubject
            )
            .Content[0]
            .Value = updated.ToString("O");

        return new StoredCertificate(id, certificate, accessible, AnIntraModel);
    }

    private static CertificateControlModel AnIntraModel =>
        new()
        {
            Status = "VALIDATED",
            ExchangedDocument = new ExchangedDocumentModel
            {
                Name = "64/432 (2016/2008) F1 Bovine",
                Declaration = new AuthenticationModel
                {
                    ActualDateTime = new DateTimeOffset(2026, 4, 21, 10, 0, 0, TimeSpan.Zero),
                },
            },
            SpecifiedConsignment = new ConsignmentModel
            {
                ExportCountry = "AF",
                ImportCountry = "XI",
                ConsignorParty = new PartyModel
                {
                    Identifier = "770198",
                    PostalAddress = new AddressModel { CountryId = "XI" },
                },
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
