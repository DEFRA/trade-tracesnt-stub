using Api.TradeTracesNTStub.Simulator.Control.Templates;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// The templates are captured TRACES responses, deserialised into the generated contract types. If
/// that round trip ever broke — a schema change, a mis-scoped namespace — the control API would
/// silently start serving a half-empty certificate, so it is asserted directly.
/// </summary>
public class ChedTemplateTests
{
    [Fact]
    public void ChedATemplateDeserialisesIntoTheGeneratedContract()
    {
        var certificate = ChedTemplates.Load("CHEDA");

        certificate.SPSExchangedDocument.Should().NotBeNull();
        certificate.SPSConsignment.Should().NotBeNull();
    }

    [Fact]
    public void ChedATemplateKeepsTheDisplayNamesTracesLookedUp()
    {
        // The gateway copies name= straight into CodedValue.name and never derives it, so a template
        // that lost these would produce a certificate the gateway maps with blank labels.
        var document = ChedTemplates.Load("CHEDA").SPSExchangedDocument;

        document.StatusCode.name.Should().Be("To be done (New)");
        document.TypeCode.name.Should().Be("Health certificate (CHED - Common Health Entry Document)");
    }

    [Fact]
    public void EachLoadReturnsAFreshCertificate()
    {
        // Callers mutate what they get back; a shared instance would leak one fixture into the next.
        var first = ChedTemplates.Load("CHEDA");
        var second = ChedTemplates.Load("CHEDA");

        first.Should().NotBeSameAs(second);
        first.SPSExchangedDocument.Should().NotBeSameAs(second.SPSExchangedDocument);
    }

    [Fact]
    public void AnUnknownTemplateNameSaysWhatIsAvailable()
    {
        var act = () => ChedTemplates.Load("CHEDZ");

        act.Should().Throw<ArgumentException>().WithMessage("*CHEDA*");
    }
}
