using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Api.TradeTracesNTStub.Simulator.WsSecurity;

public enum WsSecurityResult
{
    Valid,

    /// <summary>No <c>wsse:Security</c> header, or it is missing parts the profile requires.</summary>
    MissingSecurityHeader,

    /// <summary>The <c>wsu:Timestamp</c> window has passed, or the token was created outside it.</summary>
    ExpiredTimestamp,

    /// <summary>No account with that username, or the digest does not match the account's key.</summary>
    BadCredentials,

    /// <summary>Valid credentials, but for the account that does not serve this port.</summary>
    WrongAccount,
}

/// <summary>
/// Validates the WS-Security UsernameToken the gateway sends, built in its
/// <c>ClientBehaviours/WsSecurityHeader</c>.
/// </summary>
/// <remarks>
/// Validating rather than waving through is the whole point: a permissive simulator would hide the
/// misconfiguration we most want to catch, which is a port authenticating as the wrong account.
/// </remarks>
public class WsSecurityValidator(IReadOnlyDictionary<string, SimulatorCredentials> credentialsByKey)
{
    private static readonly XNamespace s_soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace s_wsse =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace s_wsu =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    /// <summary>
    /// Tolerance on the timestamp window. The gateway sends a two-minute window; a little slack
    /// absorbs clock skew between containers without making the expiry check meaningless.
    /// </summary>
    private static readonly TimeSpan s_clockSkew = TimeSpan.FromSeconds(30);

    public WsSecurityResult Validate(string envelope, string expectedCredentialKey)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(envelope);
        }
        catch (System.Xml.XmlException)
        {
            return WsSecurityResult.MissingSecurityHeader;
        }

        var header = document.Root?.Element(s_soap + "Header");
        var security = header?.Element(s_wsse + "Security");
        var token = security?.Element(s_wsse + "UsernameToken");

        var username = token?.Element(s_wsse + "Username")?.Value;
        var digest = token?.Element(s_wsse + "Password")?.Value;
        var nonce = token?.Element(s_wsse + "Nonce")?.Value;
        var tokenCreated = token?.Element(s_wsu + "Created")?.Value;
        // Matched on local name because the namespace varies by port: the customs contract declares
        // WebServiceClientId in .../sanco/tracesnt/base/v3, the other four in v4.
        var clientId = header
            ?.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "WebServiceClientId")
            ?.Value;

        if (
            string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(digest)
            || string.IsNullOrWhiteSpace(nonce)
            || string.IsNullOrWhiteSpace(tokenCreated)
            || string.IsNullOrWhiteSpace(clientId)
        )
            return WsSecurityResult.MissingSecurityHeader;

        var timestamp = security!.Element(s_wsu + "Timestamp");
        var timestampResult = ValidateTimestamp(timestamp, tokenCreated);
        if (timestampResult != WsSecurityResult.Valid)
            return timestampResult;

        // Which account the credentials belong to is decided before which account the port wants, so a
        // valid Customs token on a Default port reports WrongAccount rather than BadCredentials.
        var matched = credentialsByKey.FirstOrDefault(pair =>
            pair.Value.Username == username && DigestMatches(digest, nonce, tokenCreated, pair.Value.AuthenticationKey)
        );

        if (matched.Key is null)
            return WsSecurityResult.BadCredentials;

        if (matched.Key != expectedCredentialKey)
            return WsSecurityResult.WrongAccount;

        return matched.Value.WebServiceClientId == clientId
            ? WsSecurityResult.Valid
            : WsSecurityResult.WrongAccount;
    }

    private static WsSecurityResult ValidateTimestamp(XElement? timestamp, string tokenCreated)
    {
        if (timestamp is null)
            return WsSecurityResult.MissingSecurityHeader;

        if (
            !TryParseUtc(timestamp.Element(s_wsu + "Created")?.Value, out var created)
            || !TryParseUtc(timestamp.Element(s_wsu + "Expires")?.Value, out var expires)
            || !TryParseUtc(tokenCreated, out var tokenCreatedAt)
        )
            return WsSecurityResult.MissingSecurityHeader;

        var now = DateTimeOffset.UtcNow;

        if (created > expires || now > expires + s_clockSkew || now < created - s_clockSkew)
            return WsSecurityResult.ExpiredTimestamp;

        // The token must have been minted inside the window it claims to be valid for.
        return tokenCreatedAt < created - s_clockSkew || tokenCreatedAt > expires
            ? WsSecurityResult.ExpiredTimestamp
            : WsSecurityResult.Valid;
    }

    private static bool TryParseUtc(string? value, out DateTimeOffset parsed) =>
        DateTimeOffset.TryParse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out parsed
        );

    /// <summary>
    /// WS-Security UsernameToken PasswordDigest: <c>Base64(SHA-1(nonce + created + authentication_key))</c>,
    /// recomputed exactly as the gateway's <c>WsSecurityHeader.ComputePasswordDigest</c> builds it.
    /// SHA-1 is mandated by the WS-Security profile rather than chosen.
    /// </summary>
    private static bool DigestMatches(string digest, string nonce, string created, string authenticationKey)
    {
        byte[] nonceBytes;
        try
        {
            nonceBytes = Convert.FromBase64String(nonce);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] combined =
        [
            .. nonceBytes,
            .. Encoding.UTF8.GetBytes(created),
            .. Encoding.UTF8.GetBytes(authenticationKey),
        ];

        var expected = Convert.ToBase64String(SHA1.HashData(combined)); //NOSONAR - SHA1 is required by the WS-Security profile

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(digest)
        );
    }
}
