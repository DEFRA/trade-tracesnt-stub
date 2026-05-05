using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

internal static class OrganicOperatorMatchers
{
    public static IMatcher[] ValidGetOrganicOperatorCertificateRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetOrganicOperatorCertificateRequest']")
            ])
            .ToArray();

    public static IMatcher[] ValidFindOrganicOperatorCertificateRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'FindOrganicOperatorCertificateRequest']")
            ])
            .ToArray();
}