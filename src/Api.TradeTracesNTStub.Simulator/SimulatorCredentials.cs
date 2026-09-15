namespace Api.TradeTracesNTStub.Simulator;

/// <summary>
/// The closed set of accounts the simulator accepts. Mirrors the gateway's
/// <c>TracesNtCredentialKeys</c> — the customs port authenticates as a different account from the
/// certificate and reference-data ports, and the simulator has to enforce that separation or the
/// misconfiguration it exists to catch would pass silently.
/// </summary>
public static class SimulatorCredentialKeys
{
    public const string Default = "Default";
    public const string Customs = "Customs";

    public static readonly string[] All = [Default, Customs];
}

/// <summary>One account's expected WS-Security credentials, bound from <c>Simulator:Credentials:{Key}</c>.</summary>
public record SimulatorCredentials
{
    public string Username { get; init; } = "";

    /// <summary>The secret behind the WS-Security PasswordDigest. Never log this.</summary>
    public string AuthenticationKey { get; init; } = "";

    public string WebServiceClientId { get; init; } = "";
}
