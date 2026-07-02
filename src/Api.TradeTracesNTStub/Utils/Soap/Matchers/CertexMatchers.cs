using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

public static class CertexMatchers
{
    public static IMatcher[] ValidGetProcessedChedRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
                [
                    new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'ProcessedChedRequest']")
                ])
            .ToArray();
}