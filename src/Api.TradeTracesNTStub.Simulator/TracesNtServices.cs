namespace Api.TradeTracesNTStub.Simulator;

/// <summary>
/// The five TRACES service paths, and which credential set each authenticates against.
/// </summary>
/// <remarks>
/// The gateway builds every endpoint as <c>{TracesNt:BaseUrl}/{ServicePath}</c>, so these paths must
/// match the ones registered in the gateway's <c>TracesClientRegistrationExtensions</c> exactly —
/// they are the reason <c>TRACESNT__BASEURL</c> is the only setting a consumer has to change.
/// </remarks>
public static class TracesNtServices
{
    public const string SimulatorFaultNamespace = "http://defra.gov.uk/tracesnt/simulator";

    /// <summary>Where the CHED typed faults live. Must match the generated contract's FaultContract.</summary>
    public const string ChedV2Namespace = "http://ec.europa.eu/tracesnt/certificate/ched/v2";

    public const string Ched = "ChedCertificateServiceV2";
    public const string EuIntra = "EuIntraCertificateServiceV1";
    public const string Docom = "DocomCertificateRetrievalServiceV1";
    public const string ReferenceData = "ReferenceDataServiceV1";
    public const string CustomsCertexChed = "CustomsCertexChedServiceV06";

    /// <summary>Service path to credential key. The customs port authenticates as a different account.</summary>
    public static readonly IReadOnlyDictionary<string, string> CredentialKeyByPath = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [$"/{Ched}"] = SimulatorCredentialKeys.Default,
        [$"/{EuIntra}"] = SimulatorCredentialKeys.Default,
        [$"/{Docom}"] = SimulatorCredentialKeys.Default,
        [$"/{ReferenceData}"] = SimulatorCredentialKeys.Default,
        [$"/{CustomsCertexChed}"] = SimulatorCredentialKeys.Customs,
    };
}
