using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// One test per thing the simulator derives. Trade Gateway copies these into its own model without
/// ever looking one up, so a rule that stopped firing would reach a consumer as a blank label.
/// </summary>
public class ChedCertificateBuilderTests
{
    private const string Id = "CHEDA.XI.2026.0000001";

    private static readonly SpsCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    /// <summary>The least a CHED can be: the type note, and a border post to resolve the authority from.</summary>
    private static CertificateControlModel Minimal =>
        new()
        {
            ExchangedDocument = new ExchangedDocumentModel
            {
                IncludedNote = new Dictionary<string, string> { ["CHED_TYPE"] = "A" },
            },
            SpecifiedConsignment = new ConsignmentModel
            {
                UnloadingBaseportLocation = new LocationModel { Identifier = "GBBEL", CountryId = "XI" },
            },
        };

    private static SPSCertificateType Build(CertificateControlModel model) => s_builder.Build(CertificateKind.Ched, model, Id);

    [Fact]
    public void TheDocumentNameAndTypeComeFromTheChedTypeNote()
    {
        var document = Build(Minimal).SPSExchangedDocument;

        document.Name[0].Value.Should().Be("CHED-A - Common Health Entry Document for Animal");
        document.TypeCode.name.Should().Be("Health certificate (CHED - Common Health Entry Document)");
    }

    [Fact]
    public void WithoutAChedTypeNoteNothingCanBeBuilt()
    {
        var act = () => Build(Minimal with { ExchangedDocument = new ExchangedDocumentModel() });

        act.Should().Throw<UnknownCodeException>().WithMessage("*CHED_TYPE*");
    }

    [Theory]
    [InlineData("NEW", "To be done (New)")]
    [InlineData("VALIDATED", "Issued (Validated)")]
    [InlineData("1", "To be done (New)")]
    public void StatusResolvesByAliasOrCode(string status, string expected) =>
        Build(Minimal with { Status = status }).SPSExchangedDocument.StatusCode.name.Should().Be(expected);

    [Fact]
    public void TheIssuingAuthorityIsSynthesisedFromTheBorderControlPost()
    {
        // IssuerSPSParty appears in every retrieved CHED and in no submission at all, so there is
        // nothing to copy — it has to be built from the post the consignment arrives at.
        var issuer = Build(Minimal).SPSExchangedDocument.IssuerSPSParty;

        issuer.Should().NotBeNull();
        issuer!.Name.Value.Should().Be("Belfast Port BCP");
        issuer.RoleCode.name.Should().Be("Officer (Identification of Applicant)");
        issuer.TypeCode.Should().ContainSingle().Which.name.Should().Be("Authority");
    }

    [Fact]
    public void ABorderControlPostExpandsToItsSixPositionalNames()
    {
        // Order is the only thing distinguishing these on the wire — country, city, activity id,
        // authority name, address, UN/LOCODE — so the order is what is asserted.
        var names = Build(Minimal).SPSConsignment.UnloadingBaseportSPSLocation.Name.Select(name => name.Value);

        names
            .Should()
            .Equal(
                "XI",
                "County Down",
                "XIBEL1-DAERA",
                "Belfast Port BCP",
                "Belfast Port BCP, Dock Road, Belfast",
                "GBBEL"
            );
    }

    [Fact]
    public void ARegisteredOperatorGainsItsNameAndActivityCodes()
    {
        var model = Minimal with
        {
            SpecifiedConsignment = Minimal.SpecifiedConsignment with
            {
                ConsignorParty = new PartyModel { Identifier = "770198" },
            },
        };

        var consignor = Build(model).SPSConsignment.ConsignorSPSParty;

        consignor.Name.Value.Should().Be("Simulator Exporters Ltd");
        consignor.TypeCode.Select(code => code.name).Should().Contain("Animal exporter");
    }

    [Fact]
    public void PartyRoleComesFromTheSlotNotTheOperator()
    {
        // TRACES overwrites whatever role a submission sends, deciding it from where the party sits.
        var model = Minimal with
        {
            SpecifiedConsignment = Minimal.SpecifiedConsignment with
            {
                ConsignorParty = new PartyModel { Identifier = "770198" },
                ConsigneeParty = new PartyModel { Identifier = "899361" },
            },
        };

        var consignment = Build(model).SPSConsignment;

        consignment.ConsignorSPSParty.RoleCode.name.Should().Be("Exporter");
        consignment.ConsigneeSPSParty.RoleCode.name.Should().Be("Consignee (Importer)");
    }

    [Fact]
    public void AnUnregisteredOperatorSkipsTheRegistryEntirely()
    {
        // The escape hatch: a name and no identifier is how TRACES models an operator created on the
        // fly, and how a test uses an operator nobody has seeded.
        var model = Minimal with
        {
            SpecifiedConsignment = Minimal.SpecifiedConsignment with
            {
                ConsignorParty = new PartyModel { Name = "Nobody Seeded Ltd" },
            },
        };

        var consignor = Build(model).SPSConsignment.ConsignorSPSParty;

        consignor.Name.Value.Should().Be("Nobody Seeded Ltd");
        consignor.ID.Should().BeNull();
    }

    [Fact]
    public void ACnCodeExpandsToItsWholeDescriptionHierarchy()
    {
        // A client could not supply this even if it wanted to — one code becomes the whole path.
        var classification = Commodity(new Dictionary<string, string> { ["CN"] = "0101" })
            .ApplicableSPSClassification.Single();

        classification.SystemName[0].Value.Should().Be("CN Code (Combined Nomenclature)");
        classification
            .ClassName.Select(name => name.Value)
            .Should()
            .Equal("LIVE ANIMALS", "Live horses, asses, mules and hinnies");
    }

    [Fact]
    public void AClauseGainsTheDisplayTextBesideItsCode()
    {
        var model = Minimal with
        {
            ExchangedDocument = Minimal.ExchangedDocument with
            {
                Declaration = new AuthenticationModel
                {
                    IncludedClause = new Dictionary<string, string> { ["PURPOSE"] = "FREE_CIRCULATION" },
                },
            },
        };

        var clause = Build(model).SPSExchangedDocument.SignatorySPSAuthentication.Single().IncludedSPSClause.Single();

        clause.ID.Value.Should().Be("PURPOSE");
        clause.Content.Select(content => content.Value).Should().Equal("FREE_CIRCULATION", "For free circulation");
    }

    [Fact]
    public void TheDeclarationAndClearanceBecomeTwoAuthentications()
    {
        var model = Minimal with
        {
            ExchangedDocument = Minimal.ExchangedDocument with
            {
                Declaration = new AuthenticationModel(),
                Clearance = new AuthenticationModel
                {
                    IncludedClause = new Dictionary<string, string> { ["DECISION_CONCLUSION"] = "NOT_ACCEPTABLE" },
                },
            },
        };

        var authentications = Build(model).SPSExchangedDocument.SignatorySPSAuthentication;

        authentications.Select(authentication => authentication.TypeCode.name).Should().Equal(
            "Inspection (Identification of Applicant)",
            "Clearance (Official inspector)"
        );
    }

    [Fact]
    public void ACertificateAlwaysCarriesTheSelfReferenceAndASupportingDocument()
    {
        // The self-reference is retrieval-only, and the supporting document is the one deliberate
        // default — a retrieved CHED always has one, and no test should have to supply it.
        var references = Build(Minimal).SPSExchangedDocument.ReferenceSPSReferencedDocument;

        references
            .Should()
            .HaveCount(2)
            .And.Contain(reference => reference.ID.Value == Id, "the CHED references itself");

        references.Single(reference => reference.ID.Value != Id).ID.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ASuppliedSupportingDocumentReplacesTheDefault()
    {
        var model = Minimal with
        {
            ExchangedDocument = Minimal.ExchangedDocument with
            {
                ReferenceDocument =
                [
                    new ReferencedDocumentModel { Identifier = "MY-REF", IssuingCountry = "GB" },
                ],
            },
        };

        Build(model)
            .SPSExchangedDocument.ReferenceSPSReferencedDocument.Select(reference => reference.ID.Value)
            .Should()
            .Contain("MY-REF");
    }

    [Fact]
    public void CommoditiesAreNumberedAfterTheTotalsLine()
    {
        var model = WithItem(
            new ConsignmentItemModel
            {
                ConsignmentTotals = new TradeLineItemModel(),
                IncludedTradeLineItem =
                [
                    new TradeLineItemModel { ApplicableClassification = new Dictionary<string, string> { ["CN"] = "0101" } },
                    new TradeLineItemModel { ApplicableClassification = new Dictionary<string, string> { ["CN"] = "0103" } },
                ],
            }
        );

        var lines = Build(model).SPSConsignment.IncludedSPSConsignmentItem[0].IncludedSPSTradeLineItem;

        lines.Select(line => line.SequenceNumeric.Value).Should().Equal(0, 1, 2);
        lines[0].Description[0].Value.Should().Be("Consignment totals and summary");
    }

    [Fact]
    public void AnUnknownCodeSaysWhichFileToAddItTo()
    {
        // With no template underneath, a silent blank is easy to produce — so this fails at the point
        // the fixture is created rather than surfacing later as an unexplained diff.
        var act = () => Commodity(new Dictionary<string, string> { ["CN"] = "9999999999" });

        act.Should().Throw<UnknownCodeException>().WithMessage("*cn_code*").And.Message.Should().Contain("SeedData");
    }

    [Fact]
    public void AnUnknownOperatorPointsAtTheWayOut()
    {
        var model = Minimal with
        {
            SpecifiedConsignment = Minimal.SpecifiedConsignment with
            {
                ConsignorParty = new PartyModel { Identifier = "not-seeded" },
            },
        };

        var act = () => Build(model);

        act.Should().Throw<UnknownCodeException>().WithMessage("*operators.json*");
    }

    private static SPSTradeLineItemType Commodity(Dictionary<string, string> classifications)
    {
        var model = WithItem(
            new ConsignmentItemModel
            {
                IncludedTradeLineItem = [new TradeLineItemModel { ApplicableClassification = classifications }],
            }
        );

        return Build(model).SPSConsignment.IncludedSPSConsignmentItem[0].IncludedSPSTradeLineItem.Single();
    }

    private static CertificateControlModel WithItem(ConsignmentItemModel item) =>
        Minimal with
        {
            SpecifiedConsignment = Minimal.SpecifiedConsignment with { IncludedConsignmentItem = item },
        };
}
