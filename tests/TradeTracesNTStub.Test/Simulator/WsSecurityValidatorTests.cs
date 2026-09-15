using System.Security.Cryptography;
using System.Text;
using Api.TradeTracesNTStub.Simulator;
using Api.TradeTracesNTStub.Simulator.WsSecurity;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// Nothing in a response reveals which account a call authenticated as, so these tests build the
/// UsernameToken the way the gateway's <c>WsSecurityHeader</c> does — recomputing the digest rather
/// than asserting on a canned string — and exercise the cross-account case in both directions. A
/// simulator that ignored credentials entirely would pass every other test here.
/// </summary>
public class WsSecurityValidatorTests
{
    private const string DefaultUser = "simulator-user";
    private const string DefaultKey = "simulator-auth-key";
    private const string DefaultClientId = "simulator-client-id";

    private const string CustomsUser = "simulator-customs-user";
    private const string CustomsKey = "simulator-customs-auth-key";
    private const string CustomsClientId = "simulator-customs-client-id";

    private readonly WsSecurityValidator _validator = new(
        new Dictionary<string, SimulatorCredentials>
        {
            [SimulatorCredentialKeys.Default] = new()
            {
                Username = DefaultUser,
                AuthenticationKey = DefaultKey,
                WebServiceClientId = DefaultClientId,
            },
            [SimulatorCredentialKeys.Customs] = new()
            {
                Username = CustomsUser,
                AuthenticationKey = CustomsKey,
                WebServiceClientId = CustomsClientId,
            },
        }
    );

    [Fact]
    public void ValidDefaultCredentials_AreAccepted()
    {
        var envelope = Envelope(DefaultUser, DefaultKey, DefaultClientId);

        _validator.Validate(envelope, SimulatorCredentialKeys.Default).Should().Be(WsSecurityResult.Valid);
    }

    [Fact]
    public void ValidCustomsCredentials_AreAccepted()
    {
        var envelope = Envelope(CustomsUser, CustomsKey, CustomsClientId);

        _validator.Validate(envelope, SimulatorCredentialKeys.Customs).Should().Be(WsSecurityResult.Valid);
    }

    [Fact]
    public void WrongAuthenticationKey_IsRejected()
    {
        var envelope = Envelope(DefaultUser, "not-the-key", DefaultClientId);

        _validator.Validate(envelope, SimulatorCredentialKeys.Default).Should().Be(WsSecurityResult.BadCredentials);
    }

    [Fact]
    public void UnknownUsername_IsRejected()
    {
        var envelope = Envelope("nobody", DefaultKey, DefaultClientId);

        _validator.Validate(envelope, SimulatorCredentialKeys.Default).Should().Be(WsSecurityResult.BadCredentials);
    }

    [Fact]
    public void MissingSecurityHeader_IsRejected()
    {
        const string envelope = """
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Header/>
              <s:Body/>
            </s:Envelope>
            """;

        _validator
            .Validate(envelope, SimulatorCredentialKeys.Default)
            .Should()
            .Be(WsSecurityResult.MissingSecurityHeader);
    }

    [Fact]
    public void MissingWebServiceClientId_IsRejected()
    {
        var envelope = Envelope(DefaultUser, DefaultKey, clientId: null);

        _validator
            .Validate(envelope, SimulatorCredentialKeys.Default)
            .Should()
            .Be(WsSecurityResult.MissingSecurityHeader);
    }

    [Fact]
    public void ExpiredTimestamp_IsRejected()
    {
        var longAgo = DateTimeOffset.UtcNow.AddMinutes(-30);
        var envelope = Envelope(DefaultUser, DefaultKey, DefaultClientId, created: longAgo);

        _validator.Validate(envelope, SimulatorCredentialKeys.Default).Should().Be(WsSecurityResult.ExpiredTimestamp);
    }

    [Fact]
    public void TokenCreatedOutsideTheTimestampWindow_IsRejected()
    {
        var now = DateTimeOffset.UtcNow;
        var envelope = Envelope(DefaultUser, DefaultKey, DefaultClientId, created: now, tokenCreated: now.AddHours(-1));

        _validator.Validate(envelope, SimulatorCredentialKeys.Default).Should().Be(WsSecurityResult.ExpiredTimestamp);
    }

    [Fact]
    public void DefaultCredentialsOnTheCustomsPort_AreRejected()
    {
        var envelope = Envelope(DefaultUser, DefaultKey, DefaultClientId);

        _validator.Validate(envelope, SimulatorCredentialKeys.Customs).Should().Be(WsSecurityResult.WrongAccount);
    }

    [Fact]
    public void CustomsCredentialsOnADefaultPort_AreRejected()
    {
        var envelope = Envelope(CustomsUser, CustomsKey, CustomsClientId);

        _validator.Validate(envelope, SimulatorCredentialKeys.Default).Should().Be(WsSecurityResult.WrongAccount);
    }

    /// <summary>
    /// Builds the header the gateway's <c>WsSecurityHeader</c> writes: a UsernameToken whose password is
    /// <c>Base64(SHA-1(nonce + created + authenticationKey))</c>, inside a two-minute wsu:Timestamp.
    /// </summary>
    private static string Envelope(
        string username,
        string authenticationKey,
        string? clientId,
        DateTimeOffset? created = null,
        DateTimeOffset? tokenCreated = null
    )
    {
        var start = created ?? DateTimeOffset.UtcNow;
        var createdText = start.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var expiresText = start.AddMinutes(2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var tokenCreatedText = (tokenCreated ?? start).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        var nonce = RandomNumberGenerator.GetBytes(16);
        byte[] combined =
        [
            .. nonce,
            .. Encoding.UTF8.GetBytes(tokenCreatedText),
            .. Encoding.UTF8.GetBytes(authenticationKey),
        ];
        var digest = Convert.ToBase64String(SHA1.HashData(combined)); //NOSONAR - SHA1 is required by the WS-Security profile

        var clientIdElement =
            clientId is null
                ? ""
                : $"""<WebServiceClientId xmlns="http://ec.europa.eu/sanco/tracesnt/base/v4">{clientId}</WebServiceClientId>""";

        return $"""
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Header>
                <wsse:Security xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
                  <wsse:UsernameToken wsu:Id="UT">
                    <wsse:Username>{username}</wsse:Username>
                    <wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">{digest}</wsse:Password>
                    <wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">{Convert.ToBase64String(nonce)}</wsse:Nonce>
                    <wsu:Created>{tokenCreatedText}</wsu:Created>
                  </wsse:UsernameToken>
                  <wsu:Timestamp wsu:Id="TS">
                    <wsu:Created>{createdText}</wsu:Created>
                    <wsu:Expires>{expiresText}</wsu:Expires>
                  </wsu:Timestamp>
                </wsse:Security>
                {clientIdElement}
              </s:Header>
              <s:Body/>
            </s:Envelope>
            """;
    }
}
