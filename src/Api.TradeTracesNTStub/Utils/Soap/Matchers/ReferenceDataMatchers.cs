using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

internal static class ReferenceDataMatchers
{
    public static IMatcher[] ValidGetClassificationSectionsRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetClassificationSectionsRequest']")
            ])
            .ToArray();

    public static IMatcher[] ValidGetClassificationTreesRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetClassificationTreesRequest']")
            ])
            .ToArray();

    public static IMatcher[] ValidGetClassificationTreeRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetClassificationTreeRequest']/*[local-name() = 'TreeID' and text()]")
            ])
            .ToArray();

    public static IMatcher[] ValidGetClassificationTreeNodeDetailRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetClassificationTreeNodeDetailRequest']")
            ])
            .ToArray();
}