using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// The enrichment these tests cover is what makes the simulator wire-compatible. TRACES returns a
/// document whose codes carry the display names it looked up, and Trade Gateway copies those names
/// straight through rather than deriving them — so a certificate built without them maps to a model
/// full of blanks, and nothing downstream would notice until a snapshot diff.
/// </summary>
public class ChedCertificateFactoryTests
{
    private static readonly ChedCertificateFactory s_factory = new(CodeLists.Seeded);

    private const string Id = "CHEDA.XI.2026.0000001";

    private static ChedControlModel Model => new() { Type = ChedType.A };

    [Fact]
    public void AnEmptyModelStillProducesAValidCertificate()
    {
        // The point of template-plus-overrides: a test supplies nothing and still gets a document
        // realistic enough to serve.
        var certificate = s_factory.Create(Model, Id);

        certificate.SPSExchangedDocument.ID.Value.Should().Be(Id);
        certificate.SPSConsignment.ConsignorSPSParty.Should().NotBeNull();
        certificate.SPSExchangedDocument.StatusCode.name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TheIdIsWrittenToTheSelfReferenceAsWellAsTheDocument()
    {
        // The CAW reference points the document at itself, URL included. Missing it is invisible
        // until something follows the link.
        var document = s_factory.Create(Model, Id).SPSExchangedDocument;

        var self = document.ReferenceSPSReferencedDocument.Single(reference =>
            reference.RelationshipTypeCode?.Value == XmlEnums.Parse<ReferenceTypeCodeContentType>("CAW")
        );

        self.ID.Value.Should().Be(Id);
        self.AttachmentBinaryObject.Should().AllSatisfy(attachment => attachment.uri.Should().EndWith(Id));
    }

    [Theory]
    [InlineData("NEW", "To be done (New)")]
    [InlineData("VALIDATED", "Issued (Validated)")]
    [InlineData("1", "To be done (New)")]
    public void StatusIsResolvedByAliasOrCodeAndCarriesItsDisplayName(string status, string expected)
    {
        var certificate = s_factory.Create(Model with { Status = status }, Id);

        certificate.SPSExchangedDocument.StatusCode.name.Should().Be(expected);
    }

    [Fact]
    public void ACnCodeExpandsToItsWholeDescriptionHierarchy()
    {
        var model = Model with { Commodities = [new CommodityModel { CnCode = "0101" }] };

        var line = Commodity(s_factory.Create(model, Id));

        line.ApplicableSPSClassification.Should()
            .ContainSingle(classification => classification.SystemID.Value == "CN")
            .Which.ClassName.Select(name => name.Value)
            .Should()
            .Equal("LIVE ANIMALS", "Live horses, asses, mules and hinnies");
    }

    [Fact]
    public void AnOriginCountryCodeGainsItsCountryName()
    {
        var model = Model with { Commodities = [new CommodityModel { OriginCountry = "AF" }] };

        var line = Commodity(s_factory.Create(model, Id));

        line.OriginSPSCountry.Single().Name.Single().Value.Should().Be("Afghanistan");
    }

    [Fact]
    public void PackagingCarriesTheUnecePackageTypeName()
    {
        var model = Model with { Commodities = [new CommodityModel { PackageType = "BX", PackageCount = 3 }] };

        var package = Commodity(s_factory.Create(model, Id)).PhysicalSPSPackage.Single();

        package.TypeCode.name.Should().Be("Box");
        package.ItemQuantity.Value.Should().Be(3);
    }

    [Fact]
    public void CommoditiesAreNumberedAfterTheSummaryLineTracesKeepsFirst()
    {
        var model = Model with
        {
            Commodities = [new CommodityModel { CnCode = "0101" }, new CommodityModel { CnCode = "0103" }],
        };

        var lines = s_factory.Create(model, Id).SPSConsignment.IncludedSPSConsignmentItem[0].IncludedSPSTradeLineItem;

        lines.Select(line => line.SequenceNumeric.Value).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void ADecisionIsWrittenAsTheOfficialInspectorsAuthentication()
    {
        var model = Model with
        {
            Decision = new DecisionModel { Conclusion = "NOT_ACCEPTABLE", PhysicalCheck = "NOT_SATISFACTORY" },
        };

        var clearance = s_factory
            .Create(model, Id)
            .SPSExchangedDocument.SignatorySPSAuthentication.Single(authentication =>
                authentication.TypeCode?.Value == XmlEnums.Parse<GovernmentActionCodeContentType>("1")
            );

        // A check contributes a pair: that it happened, and its result.
        clearance
            .IncludedSPSClause.Select(clause => clause.ID.Value)
            .Should()
            .Equal("DECISION_CONCLUSION", "PHYSICAL_CHECK", "PHYSICAL_CHECK_RESULT");

        // Each clause carries the code and the display text beside it.
        clearance
            .IncludedSPSClause[0]
            .Content.Select(content => content.Value)
            .Should()
            .Equal("NOT_ACCEPTABLE", "Not acceptable");
    }

    [Fact]
    public void TheApplicantsDeclarationSurvivesADecision()
    {
        // Adding the inspector's block must not drop the applicant's, which the template carries.
        var model = Model with { Decision = new DecisionModel { Conclusion = "NOT_ACCEPTABLE" } };

        var authentications = s_factory.Create(model, Id).SPSExchangedDocument.SignatorySPSAuthentication;

        authentications
            .Should()
            .Contain(authentication =>
                authentication.TypeCode.Value == XmlEnums.Parse<GovernmentActionCodeContentType>("4")
            );
    }

    [Fact]
    public void RefusalReasonsRideAsDocumentNotes()
    {
        var model = Model with
        {
            Decision = new DecisionModel
            {
                Conclusion = "NOT_ACCEPTABLE",
                NotAcceptableMeasure = "DESTRUCTION",
                RefusalReasons = ["MISSING_CERTIFICATE"],
            },
        };

        var notes = s_factory.Create(model, Id).SPSExchangedDocument.IncludedSPSNote;

        notes.Should().Contain(note => note.SubjectCode.Value == "NOT_ACCEPTABLE_MEASURE");
        notes.Should().Contain(note => note.SubjectCode.Value == "REFUSAL_REASON");
    }

    [Fact]
    public void AnUnknownCodeSaysWhichListToAddItTo()
    {
        // A blank display name would surface much later as an unexplained snapshot diff, so this
        // fails at the point the fixture is created and names the file to edit.
        var model = Model with { Commodities = [new CommodityModel { CnCode = "9999999999" }] };

        var act = () => s_factory.Create(model, Id);

        act.Should()
            .Throw<UnknownCodeException>()
            .WithMessage("*cn_code*")
            .And.Message.Should()
            .Contain("SeedData");
    }

    [Fact]
    public void OverridingAPartyKeepsTheCodesTheTemplateAlreadyEnriched()
    {
        var model = Model with { Consignor = new PartyModel { OperatorId = "999", Name = "New Name" } };

        var consignor = s_factory.Create(model, Id).SPSConsignment.ConsignorSPSParty;

        consignor.ID.Value.Should().Be("999");
        consignor.Name.Value.Should().Be("New Name");
        consignor.RoleCode.name.Should().NotBeNullOrEmpty();
        consignor.ID.schemeAgencyID.Should().NotBeNullOrEmpty("the template's scheme attributes should survive");
    }

    private static SPSTradeLineItemType Commodity(SPSCertificateType certificate) =>
        certificate.SPSConsignment.IncludedSPSConsignmentItem[0].IncludedSPSTradeLineItem.Single(line =>
            line.SequenceNumeric.Value == 1
        );
}
