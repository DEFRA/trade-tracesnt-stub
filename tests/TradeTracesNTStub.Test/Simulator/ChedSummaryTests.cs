using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Ports;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// A search result is a second view of a certificate the simulator already built, so what these pin
/// is that the two agree: every field below is read back out of the document rather than held
/// separately, and a projection that quietly stopped finding one would show a blank column.
/// </summary>
public class ChedSummaryTests
{
    private static readonly ChedCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    private static ChedControlModel AChedA =>
        new()
        {
            Status = "VALIDATED",
            ExchangedDocument = new ExchangedDocumentModel
            {
                IncludedNote = new Dictionary<string, string> { ["CHED_TYPE"] = "A" },
                Declaration = new AuthenticationModel
                {
                    ActualDateTime = new DateTimeOffset(2026, 4, 21, 10, 0, 0, TimeSpan.Zero),
                },
                Clearance = new AuthenticationModel
                {
                    ActualDateTime = new DateTimeOffset(2026, 4, 23, 9, 0, 0, TimeSpan.Zero),
                },
            },
            SpecifiedConsignment = new ConsignmentModel
            {
                AvailabilityDueDateTime = new DateTimeOffset(2026, 4, 22, 23, 0, 0, TimeSpan.Zero),
                ExportCountry = "AF",
                ImportCountry = "XI",
                ConsignorParty = new PartyModel
                {
                    Identifier = "770198",
                    PostalAddress = new AddressModel { CountryId = "XI" },
                },
                ConsigneeParty = new PartyModel
                {
                    Identifier = "899361",
                    PostalAddress = new AddressModel { CountryId = "XI" },
                },
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

    private static ChedCertificateQueryResultType Summary(ChedControlModel model) =>
        ChedSummary.Of(s_builder.Build(model, "CHEDA.XI.2026.0000001"));

    [Fact]
    public void TheTypeAndStatusCarryTheirDisplayNames()
    {
        var summary = Summary(AChedA);

        summary.ID.Should().Be("CHEDA.XI.2026.0000001");
        summary.Type!.Value.Should().Be("A");
        summary.Type.name.Should().Be("CHED-A - Common Health Entry Document for Animal");
        summary.Status.name.Should().Be("Issued (Validated)");
    }

    [Fact]
    public void TheBorderControlPostSplitsIntoItsCodeAndUnLocode()
    {
        // Both come out of the baseport's positional names, which is the only place they exist.
        var summary = Summary(AChedA);

        summary.BCPCode!.Value.Should().Be("XIBEL1-DAERA");
        summary.BCPUnLocode!.Value.Should().Be("GBBEL");
    }

    [Fact]
    public void TheCommodityIsTheFirstRealLineNotTheTotals()
    {
        // Sequence zero is the totals line and describes no goods, so a summary built from it would
        // show an empty commodity for every CHED.
        var summary = Summary(AChedA);

        summary.CommodityApplicableSPSClassification.Should().ContainSingle();
        summary.CommodityApplicableSPSClassification[0].ClassCode.Value.Should().Be("0101");
        summary.CountryOfOrigin.Select(country => country.Value).Should().Equal("AF");
    }

    [Fact]
    public void PartiesContributeTheirResolvedNamesAndCountries()
    {
        var summary = Summary(AChedA);

        summary.ConsignorName.Should().Be("Simulator Exporters Ltd");
        summary.CountryOfConsignor!.Value.Should().Be("XI");
        summary.CountryOfEntry!.Value.Should().Be("XI");
        summary.CountryOfDispatch!.Value.Should().Be("AF");
    }

    [Fact]
    public void TheSignatoryTimestampsAreReadBackFromTheAuthentications()
    {
        var summary = Summary(AChedA);

        summary.DeclarationDateTimeSpecified.Should().BeTrue();
        summary.DeclarationDateTime.Should().Be(new DateTime(2026, 4, 21, 10, 0, 0));
        summary.DecisionDateTimeSpecified.Should().BeTrue();
        summary.DecisionDateTime.Should().Be(new DateTime(2026, 4, 23, 9, 0, 0));
        summary.PriorNotificationDateTime.Should().Be(new DateTime(2026, 4, 22, 23, 0, 0));
    }

    [Fact]
    public void AnUndecidedChedReportsNoDecisionDateRatherThanTheEpoch()
    {
        // The generated type writes the element whenever Specified is set, so a missing clearance has
        // to clear the flag or every new CHED claims it was decided in year one.
        var summary = Summary(
            AChedA with
            {
                ExchangedDocument = AChedA.ExchangedDocument with { Clearance = null },
            }
        );

        summary.DecisionDateTimeSpecified.Should().BeFalse();
        summary.DeclarationDateTimeSpecified.Should().BeTrue();
    }
}
