using System.Net;
using System.Net.Http.Json;
using System.ServiceModel;
using System.Text.Json;
using TracesNT;
using TracesNT.ClientBehaviours;
using TracesNT.WebServices;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// The round trip the control API and CHED retrieval exist to make possible: a CHED created through
/// plain JSON, then read back through the gateway's own generated SOAP client.
/// </summary>
/// <remarks>
/// There is no read-back endpoint on the control API by design, so this is the only test that proves
/// data was actually persisted rather than merely echoed. It drives the SOAP side with the gateway's
/// clients for the same reason <see cref="GatewayClientTests"/> does — a binding mismatch shows up
/// here as a transport error rather than a fault.
/// </remarks>
[Trait("Category", "IntegrationTest")]
public class ChedRetrievalTests
{
    private const string BaseUrl = "http://localhost:8085";

    private static readonly TracesNtCredentials s_credentials = new()
    {
        Username = "simulator-user",
        AuthenticationKey = "simulator-auth-key",
        WebServiceClientId = "simulator-client-id",
    };

    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    private static HttpClient Control() => new() { BaseAddress = new Uri(BaseUrl) };

    [Fact]
    public async Task ACheddCreatedThroughTheControlApiIsServedOverSoap()
    {
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var id = await CreateChed(
            control,
            new
            {
                type = "A",
                status = "VALIDATED",
                borderControlPost = "GBBEL",
                commodities = new[]
                {
                    new
                    {
                        cnCode = "0101",
                        originCountry = "AF",
                        packageType = "BX",
                        packageCount = 2,
                    },
                },
            },
            token
        );

        var certificate = await GetCertificate(id);

        certificate.Should().NotBeNull();
        certificate!.SPSExchangedDocument.ID.Value.Should().Be(id);
    }

    [Fact]
    public async Task TheServedCertificateCarriesTheDisplayNamesTracesWouldHaveLookedUp()
    {
        // Trade Gateway copies these straight into CodedValue.name and never derives them, so a
        // certificate without them maps to a model full of blanks. This is the assertion that
        // protects that, and it is why the control API enriches rather than storing bare codes.
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var id = await CreateChed(
            control,
            new
            {
                type = "A",
                status = "VALIDATED",
                commodities = new[] { new { cnCode = "0101", originCountry = "AF" } },
            },
            token
        );

        var document = (await GetCertificate(id))!.SPSExchangedDocument;
        var line = (await GetCertificate(id))!
            .SPSConsignment.IncludedSPSConsignmentItem[0]
            .IncludedSPSTradeLineItem.Single(item => item.SequenceNumeric.Value == 1);

        document.StatusCode.name.Should().Be("Issued (Validated)");
        line.OriginSPSCountry[0].Name[0].Value.Should().Be("Afghanistan");
        line.ApplicableSPSClassification.Should()
            .ContainSingle(classification => classification.SystemID.Value == "CN")
            .Which.ClassName.Select(name => name.Value)
            .Should()
            .Contain("Live horses, asses, mules and hinnies");
    }

    [Fact]
    public async Task AnUnknownChedIsATypedNotFoundFault()
    {
        // Typed, because the gateway matches on the detail type to return 404. An untyped fault
        // would reach the caller as a 502 instead.
        var act = () => GetCertificate("CHEDA.XI.2026.9999999");

        await act.Should().ThrowAsync<FaultException<ChedCertificateNotFoundExceptionType>>();
    }

    [Fact]
    public async Task AChedMarkedInaccessibleIsATypedPermissionDeniedFault()
    {
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var id = await CreateChed(control, new { type = "A", accessible = false }, token);

        var act = () => GetCertificate(id);

        (await act.Should().ThrowAsync<FaultException<ChedCertificatePermissionDeniedExceptionType>>())
            .Which.Detail.CertificateIdentifier.Should()
            .Be(id);
    }

    [Fact]
    public async Task ResetRemovesCheds()
    {
        // The failure this exists to prevent is an order-dependent suite, so it is covered explicitly.
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var id = await CreateChed(control, new { type = "A" }, token);

        (await control.PostAsync("/control/reset", null, token)).EnsureSuccessStatusCode();

        var act = () => GetCertificate(id);

        await act.Should().ThrowAsync<FaultException<ChedCertificateNotFoundExceptionType>>();
    }

    [Fact]
    public async Task ResetToAFixtureSetLoadsIt()
    {
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var response = await control.PostAsync("/control/reset?fixtureSet=baseline", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // The set deliberately contains a CHED that cannot be read, standing in for the FORBIDDEN
        // magic ID the gateway's own CHED tests use.
        var act = () => GetCertificate("FORBIDDEN");

        await act.Should().ThrowAsync<FaultException<ChedCertificatePermissionDeniedExceptionType>>();
    }

    [Fact]
    public async Task AnUnknownCodeIsRejectedWithAMessageSayingHowToFixIt()
    {
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var response = await control.PostAsJsonAsync(
            "/control/cheds",
            new { type = "A", commodities = new[] { new { cnCode = "9999999999" } } },
            token
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(token))
            .Should()
            .Contain("cn_code")
            .And.Contain("SeedData");
    }

    private static async Task<string> CreateChed(HttpClient control, object model, CancellationToken token)
    {
        var response = await control.PostAsJsonAsync("/control/cheds", model, token);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(token));

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(s_json, token);

        return created.GetProperty("id").GetString()!;
    }

    /// <summary>Shared with <see cref="TestKitTests"/> so the client setup lives in one place.</summary>
    internal static async Task<SPSCertificateType?> GetCertificate(string id)
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferPoolSize = int.MaxValue,
        };

        var client = new ChedCertificatePortClient(
            binding,
            new EndpointAddress($"{BaseUrl}/ChedCertificateServiceV2")
        );
        client.Endpoint.EndpointBehaviors.Add(new WsSecurityEndpointBehavior(s_credentials));

        var response = await client.getChedCertificateAsync(
            new SecurityHeaderType(),
            s_credentials.WebServiceClientId,
            ISO2AlphaLanguageCodeContentType.en,
            [],
            new GetChedCertificateRequestType { ID = id }
        );

        return response.GetChedCertificateResponse1?.SPSCertificate;
    }
}
