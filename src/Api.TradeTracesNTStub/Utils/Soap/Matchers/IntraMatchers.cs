using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

internal static class IntraMatchers
{
    /// <summary>
    /// GetEuIntraCertificateRequest ID value is present and required headers are present and valid
    /// </summary>
    /// <remarks>Should be used with MatchOperator.And</remarks>
    /// <returns></returns>
    public static IMatcher[] ValidGetEuIntraCertificateRequest() =>
        MessageMatchers.ValidHeaders().Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and text()]")
            ]).ToArray();
    
    /// <summary>
    /// GetEuIntraCertificateRequest ID value is missing
    /// </summary>
    /// <remarks>Should be used with MatchOperator.And</remarks>
    /// <returns></returns>
    public static IMatcher[] InvalidGetEuIntraCertificateRequest() =>
        MessageMatchers.ValidHeaders().Concat(
        [
            new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetEuIntraCertificateRequest']/*[local-name() = 'ID' and not(text())]")
        ]).ToArray();
    
    /// <summary>
    /// GetEuIntraPdfCertificateRequest ID value is present and required headers are present and valid
    /// </summary>
    /// <remarks>Should be used with MatchOperator.And</remarks>
    /// <returns></returns>
    public static IMatcher[] ValidGetEuIntraPdfCertificateRequest() =>
        MessageMatchers.ValidHeaders().Concat(
        [
            new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetEuIntraPdfCertificateRequest']/*[local-name() = 'ID' and text()]")
        ]).ToArray();
    
    public static IMatcher[] ValidFindEuIntraCertificateRequest() =>
        MessageMatchers.ValidHeaders().Concat(
        [
            new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'FindEuIntraCertificateRequest']")
        ]).ToArray();
}