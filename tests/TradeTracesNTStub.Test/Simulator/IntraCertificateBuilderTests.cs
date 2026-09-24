using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// An INTRA is built by the same code as a CHED, so <see cref="ChedCertificateBuilderTests"/> covers
/// the shared rules. These pin only what <see cref="CertificateKind.Intra"/> changes — each a label a
/// consumer would otherwise receive in its CHED form.
/// </summary>
public class IntraCertificateBuilderTests
{
    private const string Id = "INTRA.XI.2026.0000001";

    private static readonly SpsCertificateBuilder s_builder = new(
        CodeLists.Seeded,
        Registry<OperatorEntry>.Load("operators.json"),
        Registry<AuthorityEntry>.Load("authorities.json")
    );

    /// <summary>The least an INTRA can be: its certificate model.</summary>
    private static CertificateControlModel Minimal =>
        new() { ExchangedDocument = new ExchangedDocumentModel { Name = "64/432 (2016/2008) F1 Bovine" } };

    private static SPSCertificateType Build(CertificateControlModel model) =>
        s_builder.Build(CertificateKind.Intra, model, Id);

    [Fact]
    public void TheDocumentNameIsTheCertificateModelAndTheTypeIsAnIntra()
    {
        var document = Build(Minimal).SPSExchangedDocument;

        document.Name[0].Value.Should().Be("64/432 (2016/2008) F1 Bovine");
        document.TypeCode.Value.Should().Be(XmlEnums.Parse<DocumentNameCodeContentType>("856"));
        document.TypeCode.name.Should().Be("Inspection certificate (EU Intra Certificate)");
    }

    [Fact]
    public void WithoutACertificateModelNothingCanBeBuilt()
    {
        var act = () => Build(new CertificateControlModel());

        act.Should().Throw<UnknownCodeException>().WithMessage("*exchangedDocument.name*");
    }

    [Fact]
    public void NotesAreDrawnFromTheIntraList()
    {
        var notes = Build(Minimal).SPSExchangedDocument.IncludedSPSNote;

        notes.Should().ContainSingle().Which.SubjectCode.listID.Should().Be("eu_intra_note_subject_code");
    }

    [Fact]
    public void TheSelfReferenceIsAnIntraAndNoSupportingDocumentIsFabricated()
    {
        var reference = Build(Minimal).SPSExchangedDocument.ReferenceSPSReferencedDocument.Should().ContainSingle().Subject;

        reference.TypeCode.name.Should().Be("Inspection certificate (EU Intra Certificate)");
        reference.AttachmentBinaryObject[0].uri.Should().EndWith($"/certificate/eu-intra/{Id}");
    }

    [Fact]
    public void PartiesTakeTheIntraRolesAndOperatorScheme()
    {
        var operatorParty = new PartyModel { Identifier = "770198" };
        var model = Minimal with
        {
            SpecifiedConsignment = new ConsignmentModel
            {
                ConsignorParty = operatorParty,
                ConsigneeParty = operatorParty,
                DespatchParty = operatorParty,
            },
        };

        var consignment = Build(model).SPSConsignment;

        consignment.ConsignorSPSParty.RoleCode.name.Should().Be("Consignor");
        consignment.DespatchSPSParty.RoleCode.name.Should().Be("Despatch party (Place of dispatch)");
        consignment.ConsignorSPSParty.ID.schemeID.Should().Be("operator_activity_id");
    }

    [Fact]
    public void ClausesAreFiledUnderTheIntraLists()
    {
        var model = Minimal with
        {
            ExchangedDocument = Minimal.ExchangedDocument with
            {
                Declaration = new AuthenticationModel
                {
                    IncludedClause = new Dictionary<string, string> { ["CERTIFIED_AS_OR_FOR"] = "RELAYING" },
                },
                Clearance = new AuthenticationModel
                {
                    IncludedClause = new Dictionary<string, string> { ["SIGNATORY_PERSON_EMAIL"] = "vet@example.com" },
                },
            },
        };

        var authentications = Build(model).SPSExchangedDocument.SignatorySPSAuthentication;

        authentications[0].IncludedSPSClause[0].ID.schemeID.Should().Be("eu_intra_consignment_clause");
        authentications[1].IncludedSPSClause[0].ID.schemeID.Should().Be("eu_intra_certification_clause");
        authentications[1].TypeCode.name.Should().Be("Clearance (Certification by certifying officer)");
    }
}
