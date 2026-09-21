using Api.TradeTracesNTStub.Simulator;
using Api.TradeTracesNTStub.Simulator.Control;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// The control API shares a host with five SOAP services and the WireMock stub. A prefix collision
/// would not fail loudly — one of them would simply stop answering — so it is asserted rather than
/// left to inspection.
/// </summary>
public class ControlApiPathTests
{
    [Fact]
    public void TheControlPrefixCollidesWithNoSoapServicePath()
    {
        TracesNtServices
            .CredentialKeyByPath.Keys.Should()
            .AllSatisfy(path =>
                path.StartsWith(ChedControlEndpoints.Prefix, StringComparison.OrdinalIgnoreCase)
                    .Should()
                    .BeFalse($"{path} must not sit under the control API prefix")
            );
    }

    [Fact]
    public void TheControlPrefixCollidesWithNothingElseTheHostOwns()
    {
        string[] taken = ["/mock", "/proxy", "/health"];

        taken
            .Should()
            .AllSatisfy(path =>
                path.StartsWith(ChedControlEndpoints.Prefix, StringComparison.OrdinalIgnoreCase).Should().BeFalse()
            );

        taken.Should().NotContain(ChedControlEndpoints.Prefix);
        ChedControlEndpoints.Prefix.Should().StartWith("/");
    }
}
