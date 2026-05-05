using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

internal static class OperatorMatchers
{
    public static IMatcher[] ValidCreateOperatorRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'CreateOperatorRequest']")
            ])
            .ToArray();

    public static IMatcher[] ValidFindOperatorRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'FindOperatorRequest']")
            ])
            .ToArray();
}