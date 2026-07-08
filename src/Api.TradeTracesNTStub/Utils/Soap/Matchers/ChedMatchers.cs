using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

public static class ChedMatchers
{
    public static IMatcher[] ValidGetChedCertificateRequestRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
                [
                    new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'Envelope']/*[local-name() = 'Body']/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() != 'CHEDPP.XI.2026.0000074']")
                ])
            .ToArray();

    public static IMatcher[] ValidPermissionDenied() =>
    MessageMatchers.ValidHeaders()
        .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'CHEDPP.XI.2026.0000074']")
            ])
        .ToArray();
    
    public static IMatcher[] ValidCreateAndSubmitChedForDecisionRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(MessageMatchers.ValidCompetentAuthorityHeaders())
            .Concat(
                [
                    new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'CreateAndSubmitChedForDecisionRequest']")
                ])
            .ToArray();
}