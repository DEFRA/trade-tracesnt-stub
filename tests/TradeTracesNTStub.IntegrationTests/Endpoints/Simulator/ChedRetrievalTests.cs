using System.Net;
using System.Net.Http.Json;
using System.ServiceModel;
using System.Text.Json;
using TracesNT;
using TracesNT.ClientBehaviours;
using TracesNT.WebServices;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// A CHED created through plain JSON, read back through the gateway's own SOAP client. With no
/// read-back endpoint on the control API, this is the only proof data is persisted, not echoed.
/// </summary>
[Trait("Category", "IntegrationTest")]
[Collection(SimulatorStateCollection.Name)]
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

    /// <summary>
    /// A CHED-A carrying what a submitter carries. Written out rather than built so these tests
    /// exercise the HTTP contract directly; the TestKit builders are covered in <see cref="TestKitTests"/>.
    /// </summary>
    private static object AChedA(bool accessible = true, string cnCode = "0101") =>
        new
        {
            status = "VALIDATED",
            accessible,
            exchangedDocument = new
            {
                includedNote = new Dictionary<string, string> { ["CHED_TYPE"] = "A" },
                declaration = new
                {
                    includedClause = new Dictionary<string, string>
                    {
                        ["PURPOSE"] = "FREE_CIRCULATION",
                        ["GOODS_CERTIFIED_AS"] = "FATTENING",
                    },
                },
            },
            specifiedConsignment = new
            {
                exportCountry = "AF",
                importCountry = "XI",
                consignorParty = new { identifier = "770198", postalAddress = new { countryId = "XI" } },
                unloadingBaseportLocation = new { identifier = "GBBEL", countryId = "XI" },
                includedConsignmentItem = new
                {
                    natureIdCargo = "12",
                    includedTradeLineItem = new[]
                    {
                        new
                        {
                            applicableClassification = new Dictionary<string, string> { ["CN"] = cnCode },
                            originCountry = "AF",
                            physicalReferencedLogisticsPackage = new { typeCode = "BX", itemQuantity = 2 },
                        },
                    },
                },
            },
        };

    [Fact]
    public async Task ACheddCreatedThroughTheControlApiIsServedOverSoap()
    {
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var id = await CreateChed(control, AChedA(), token);

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

        var id = await CreateChed(control, AChedA(), token);

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

        var id = await CreateChed(control, AChedA(accessible: false), token);

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

        var id = await CreateChed(control, AChedA(), token);

        await Reset(control, token);

        var act = () => GetCertificate(id);

        await act.Should().ThrowAsync<FaultException<ChedCertificateNotFoundExceptionType>>();
    }

    [Fact]
    public async Task AnUnknownCodeIsRejectedWithAMessageSayingHowToFixIt()
    {
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var response = await control.PostAsJsonAsync(
            "/control/cheds",
            AChedA(cnCode: "9999999999"),
            token
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(token))
            .Should()
            .Contain("cn_code")
            .And.Contain("SeedData");
    }

    [Fact]
    public async Task FindReturnsWhatWasStoredAndSkipsWhatIsNotAccessible()
    {
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        await Reset(control, token);

        var visible = await CreateChed(control, AChedA(), token);
        await CreateChed(control, AChedA(accessible: false), token);

        var results = await Find(pageSize: 100);

        results.Select(result => result.ID).Should().Equal(visible);
        results.Single().Status.name.Should().Be("Issued (Validated)");
    }

    [Fact]
    public async Task FindPagesWithOneBasedOffsets()
    {
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        await Reset(control, token);

        var older = await CreateChed(control, AChedA(), token);
        var newer = await CreateChed(control, AChedA(), token);

        var firstPage = await Find(pageSize: 1);
        var secondPage = await Find(pageSize: 1, offset: 2);

        // Newest first, and offset 2 is the second result rather than the third — 1-based, as the
        // generated clients assume. An off-by-one here would silently skip a CHED on every page.
        firstPage.Select(result => result.ID).Should().Equal(newer);
        secondPage.Select(result => result.ID).Should().Equal(older);
    }

    private static async Task Reset(HttpClient control, CancellationToken token) =>
        (await control.PostAsync("/control/reset", null, token)).EnsureSuccessStatusCode();

    [Fact]
    public async Task FindNarrowsToTheUpdateDateRange()
    {
        // The simulator stamps the update time as it stores, so the moment between the two creates
        // is a boundary the test knows without the control API having to expose one. A range opening
        // there must hold the second CHED and not the first.
        var token = TestContext.Current.CancellationToken;
        using var control = Control();

        var before = await CreateChed(control, AChedA(), token);
        var between = DateTime.UtcNow;
        var after = await CreateChed(control, AChedA(), token);

        var matching = await Find(pageSize: 100, from: between);
        var missing = await Find(pageSize: 100, from: between.AddYears(1), to: between.AddYears(2));

        matching.Select(result => result.ID).Should().Contain(after).And.NotContain(before);
        missing.Should().BeEmpty();
    }

    private static async Task<IReadOnlyList<ChedCertificateQueryResultType>> Find(
        int pageSize = 10,
        int offset = 1,
        DateTime from = default,
        DateTime to = default
    )
    {
        var response = await Client()
            .findChedCertificateAsync(
                new SecurityHeaderType(),
                s_credentials.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new FindChedCertificateRequestType
                {
                    UpdateDateTimeRange = new DateTimeRange { From = from, To = to },
                    pageSize = pageSize,
                    offset = offset,
                }
            );

        return response.FindChedCertificateResponse1?.ChedCertificateResult ?? [];
    }

    private static async Task<string> CreateChed(HttpClient control, object model, CancellationToken token)
    {
        var response = await control.PostAsJsonAsync("/control/cheds", model, token);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(token));

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(s_json, token);

        return created.GetProperty("id").GetString()!;
    }

    /// <summary>The gateway's own client, bound exactly as the gateway binds it.</summary>
    private static ChedCertificatePortClient Client()
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

        return client;
    }

    /// <summary>Shared with <see cref="TestKitTests"/> so the client setup lives in one place.</summary>
    internal static async Task<SPSCertificateType?> GetCertificate(string id)
    {
        var response = await Client().getChedCertificateAsync(
            new SecurityHeaderType(),
            s_credentials.WebServiceClientId,
            ISO2AlphaLanguageCodeContentType.en,
            [],
            new GetChedCertificateRequestType { ID = id }
        );

        return response.GetChedCertificateResponse1?.SPSCertificate;
    }
}
