using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

public static class ChedMatchers
{
    public static IMatcher[] ValidGetChedCertificateRequestRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
                [
                    new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() != 'ched.permission.denied']")
                ])
            .ToArray();

    public static IMatcher[] PermissionDeniedErrorFromTraces() =>
        MessageMatchers.ValidHeaders()
            .Concat(
                [
                    new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetChedCertificateRequest']/*[local-name() = 'ID' and text() = 'ched.permission.denied']")
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

    public static IMatcher[] ValidFindChedCertificateRequest() => 
        MessageMatchers.ValidHeaders()
            .Concat(
                [
                    new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'FindChedCertificateRequest']")
                ])
            .ToArray();
}