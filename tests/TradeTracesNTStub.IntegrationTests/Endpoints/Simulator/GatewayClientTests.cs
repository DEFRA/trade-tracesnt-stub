using System.ServiceModel;
using System.ServiceModel.Channels;
using TracesNT;
using TracesNT.ClientBehaviours;
using TracesNT.WebServices;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Simulator;

/// <summary>
/// Drives the simulator with Trade Gateway's own generated WCF clients, configured exactly as the
/// gateway configures them. This is the test that proves the epic's premise: the gateway reaches the
/// simulator by pointing <c>TRACESNT__BASEURL</c> at it and changing nothing else.
/// </summary>
/// <remarks>
/// A binding or address mismatch surfaces here as a transport or serialisation error rather than a
/// SOAP fault, so asserting that a well-formed fault comes back is what makes the test meaningful.
/// </remarks>
[Trait("Category", "IntegrationTest")]
public class GatewayClientTests
{
    private const string BaseUrl = "http://localhost:8085";

    private static readonly TracesNtCredentials s_default = new()
    {
        Username = "simulator-user",
        AuthenticationKey = "simulator-auth-key",
        WebServiceClientId = "simulator-client-id",
    };

    private static readonly TracesNtCredentials s_customs = new()
    {
        Username = "simulator-customs-user",
        AuthenticationKey = "simulator-customs-auth-key",
        WebServiceClientId = "simulator-customs-client-id",
    };

    [Fact]
    public async Task ChedPort_IsReachable_AndReportsNotImplemented()
    {
        var client = Client<ChedCertificatePortClient, ChedCertificatePort>(
            "ChedCertificateServiceV2",
            s_default,
            (binding, address) => new ChedCertificatePortClient(binding, address)
        );

        var act = () =>
            client.getChedCertificateAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetChedCertificateRequestType { ID = "CHEDA.GB.2026.0000001" }
            );

        await ShouldReportNotImplemented(act, "getChedCertificate");
    }

    [Fact]
    public async Task EuIntraPort_IsReachable_AndReportsNotImplemented()
    {
        var client = Client<EuIntraCertificatePortClient, EuIntraCertificatePort>(
            "EuIntraCertificateServiceV1",
            s_default,
            (binding, address) => new EuIntraCertificatePortClient(binding, address)
        );

        var act = () =>
            client.getEuIntraCertificateAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetEuIntraCertificateRequestType { ID = "INTRA.GB.2026.0000001" }
            );

        await ShouldReportNotImplemented(act, "getEuIntraCertificate");
    }

    [Fact]
    public async Task DocomPort_IsReachable_AndReportsNotImplemented()
    {
        var client = Client<DocomCertificateRetrievalPortClient, DocomCertificateRetrievalPort>(
            "DocomCertificateRetrievalServiceV1",
            s_default,
            (binding, address) => new DocomCertificateRetrievalPortClient(binding, address)
        );

        var act = () =>
            client.getDocomCertificateAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetDocomCertificateRequestType { ID = "DOCOM.GB.2026.0000001" }
            );

        await ShouldReportNotImplemented(act, "getDocomCertificate");
    }

    [Fact]
    public async Task ReferenceDataPort_IsReachable_AndReportsNotImplemented()
    {
        var client = Client<ReferenceDataPortClient, ReferenceDataPort>(
            "ReferenceDataServiceV1",
            s_default,
            (binding, address) => new ReferenceDataPortClient(binding, address)
        );

        var act = () =>
            client.getClassificationSectionsAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                new GetClassificationSectionsRequestType()
            );

        await ShouldReportNotImplemented(act, "getClassificationSections");
    }

    [Fact]
    public async Task CustomsPort_IsReachable_AndReportsNotImplemented()
    {
        var client = Client<CustomsCertexChedPortClient, CustomsCertexChedPort>(
            "CustomsCertexChedServiceV06",
            s_customs,
            (binding, address) => new CustomsCertexChedPortClient(binding, address)
        );

        var act = () =>
            client.processedChedRequestAsync(
                new SecurityHeaderType(),
                s_customs.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                "GBTEST01",
                new CertexHeaderType(),
                new ProcessedChedRequestType()
            );

        await ShouldReportNotImplemented(act, "ProcessedChedRequest");
    }

    [Fact]
    public async Task CustomsPort_RejectsTheDefaultAccount()
    {
        // The customs port authenticates as its own account. Nothing in the response surface reveals
        // which account was used, so this cross-account case is the only way to catch the misconfiguration.
        var client = Client<CustomsCertexChedPortClient, CustomsCertexChedPort>(
            "CustomsCertexChedServiceV06",
            s_default,
            (binding, address) => new CustomsCertexChedPortClient(binding, address)
        );

        var act = () =>
            client.processedChedRequestAsync(
                new SecurityHeaderType(),
                s_default.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                "GBTEST01",
                new CertexHeaderType(),
                new ProcessedChedRequestType()
            );

        (await act.Should().ThrowAsync<FaultException>()).Which.Message.Should().Contain("UnauthenticatedException");
    }

    [Fact]
    public async Task ChedPort_RejectsAWrongAuthenticationKey()
    {
        var wrongKey = s_default with { AuthenticationKey = "not-the-key" };
        var client = Client<ChedCertificatePortClient, ChedCertificatePort>(
            "ChedCertificateServiceV2",
            wrongKey,
            (binding, address) => new ChedCertificatePortClient(binding, address)
        );

        var act = () =>
            client.getChedCertificateAsync(
                new SecurityHeaderType(),
                wrongKey.WebServiceClientId,
                ISO2AlphaLanguageCodeContentType.en,
                [],
                new GetChedCertificateRequestType { ID = "CHEDA.GB.2026.0000001" }
            );

        (await act.Should().ThrowAsync<FaultException>()).Which.Message.Should().Contain("UnauthenticatedException");
    }

    private static async Task ShouldReportNotImplemented(Func<Task> act, string operation)
    {
        var fault = (await act.Should().ThrowAsync<FaultException>()).Which;

        fault.Message.Should().Contain(operation).And.Contain("not implemented");

        // Distinguishable from an authentication failure, which is a sender fault.
        fault.Message.Should().NotContain("UnauthenticatedException");
    }

    /// <summary>Mirrors the gateway's own client registration: same binding, same WS-Security behaviour.</summary>
    private static TClient Client<TClient, TChannel>(
        string servicePath,
        TracesNtCredentials credentials,
        Func<Binding, EndpointAddress, TClient> create
    )
        where TClient : ClientBase<TChannel>
        where TChannel : class
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            MaxBufferPoolSize = int.MaxValue,
        };

        var client = create(binding, new EndpointAddress($"{BaseUrl}/{servicePath}"));
        client.Endpoint.EndpointBehaviors.Add(new WsSecurityEndpointBehavior(credentials));

        return client;
    }
}
