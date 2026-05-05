using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

internal static class MessageMatchers
{
    private const string HeaderXPath = "/*[local-name() = 'Envelope']/*[local-name() = 'Header']";
    private const string SecurityHeaderXPath = HeaderXPath + "/*[local-name() = 'Security']";
    public const string BodyXPath = "/*[local-name() = 'Envelope']/*[local-name() = 'Body']";

    /// <summary>
    /// Required and valid header values:
    /// <list type="bullet">
    /// <item>
    /// <description>WebServiceClientId element must be present</description>
    /// </item>
    /// <item>
    /// <description>Security element must be present
    /// <list type="number">
    /// <item>
    /// <description>Must contain UsernameToken element
    /// <list type="bullet">
    /// <item>
    /// <description>Must contain Username, Password, Nonce and Created elements</description>
    /// </item>
    /// <item>
    /// <description>Created must be a timestamp that falls within the Timestamp element's Created and Expires range</description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// <item>
    /// <description>Must contain Timestamp element
    /// <list type="number">
    /// <item>
    /// <description>Must contain Created and Expires</description>
    /// </item>
    /// <item>
    /// <description>Created cannot be older than 60 seconds and must be before Expires</description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>Should be used with MatchOperator.And</remarks>
    /// <returns></returns>
    public static IMatcher[] ValidHeaders() =>
        [
            new XPathMatcher(HeaderXPath + "/*[local-name() = 'Security']"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Username' and text()]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Password' and text()]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Nonce' and text()]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Created' and text()]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Created' and text()]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Expires' and text()]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Created' and xs:dateTime(text()) >= (current-dateTime() - xs:dayTimeDuration('PT60S'))]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Created' and xs:dateTime(text()) < (xs:dateTime(" + SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Expires']/text()))]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Created' and xs:dateTime(text()) >= (xs:dateTime(" + SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Created']/text()))]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Created' and xs:dateTime(text()) < (xs:dateTime(" + SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Expires']/text()))]"),
            new XPathMatcher(HeaderXPath + "/*[local-name() = 'WebServiceClientId' and text()]")
        ];

    public static IMatcher[] ValidCompetentAuthorityHeaders() =>
        [
            new XPathMatcher(HeaderXPath + "/*[local-name() = 'BodyIdentity']"),
            new XPathMatcher(HeaderXPath + "/*[local-name() = 'BodyIdentity']/*[local-name() = 'AuthorityActivityAccessIdentifier' and text()]")
        ];
    
    /// <summary>
    /// Any of the required header values are missing or not valid
    /// </summary>
    /// <remarks>Should be used with MatchOperator.Or</remarks>
    /// <returns></returns>
    public static IMatcher[] InvalidHeaders() =>
        [
            new XPathMatcher("/*[local-name() = 'Envelope']/*[local-name() = 'Header' and not(descendant::*[local-name() = 'Security'])]"),
            new XPathMatcher(HeaderXPath + "/*[local-name() = 'Security' and not(descendant::*[local-name() = 'UsernameToken'])]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Username' and not(text())]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Password' and not(text())]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Nonce' and not(text())]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Created' and not(text())]"),
            new XPathMatcher(HeaderXPath + "/*[local-name() = 'Security' and not(descendant::*[local-name() = 'Timestamp'])]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Created' and not(text())]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Expires' and not(text())]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Created' and xs:dateTime(text()) < (current-dateTime() - xs:dayTimeDuration('PT60S'))]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Created' and xs:dateTime(text()) >= (xs:dateTime(" + SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Expires']/text()))]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Created' and xs:dateTime(text()) < (xs:dateTime(" + SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Created']/text()))]"),
            new XPathMatcher(SecurityHeaderXPath + "/*[local-name() = 'UsernameToken']/*[local-name() = 'Created' and xs:dateTime(text()) >= (xs:dateTime(" + SecurityHeaderXPath + "/*[local-name() = 'Timestamp']/*[local-name() = 'Expires']/text()))]"),
            new XPathMatcher(HeaderXPath + "/*[local-name() = 'WebServiceClientId' and not(text())]")
        ];
}