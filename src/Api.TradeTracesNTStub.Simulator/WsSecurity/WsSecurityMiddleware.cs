using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.TradeTracesNTStub.Simulator.WsSecurity;

/// <summary>
/// Rejects requests to the SOAP service paths that do not carry valid WS-Security credentials for
/// that path's account, before CoreWCF sees them.
/// </summary>
/// <remarks>
/// A middleware rather than a CoreWCF dispatch inspector: the check is the same XPath-over-the-envelope
/// work the WireMock stub already does, it applies uniformly to all five ports, and the request path
/// is all it needs to know which account should have signed.
/// </remarks>
public class WsSecurityMiddleware(RequestDelegate next, WsSecurityValidator validator, ILogger<WsSecurityMiddleware> logger)
{
    /// <summary>
    /// The fault TRACES returns for a failed authentication, captured from the acceptance environment.
    /// An untyped sender fault, so the gateway surfaces it as a 502 rather than a 403 — 403 is reserved
    /// for the typed per-certificate <c>PermissionDenied</c> faults.
    /// </summary>
    private const string UnauthenticatedFault = """
        <?xml version='1.0' encoding='UTF-8'?>
        <env:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/">
          <env:Header/>
          <env:Body>
            <env:Fault>
              <faultcode>env:Client</faultcode>
              <faultstring>UnauthenticatedException</faultstring>
            </env:Fault>
          </env:Body>
        </env:Envelope>
        """;

    public async Task Invoke(HttpContext context)
    {
        if (!TracesNtServices.CredentialKeyByPath.TryGetValue(context.Request.Path, out var credentialKey))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        string envelope;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            envelope = await reader.ReadToEndAsync(context.RequestAborted);
        }
        context.Request.Body.Position = 0;

        var result = validator.Validate(envelope, credentialKey);
        if (result == WsSecurityResult.Valid)
        {
            await next(context);
            return;
        }

        logger.LogWarning(
            "Rejected {Path}: {Reason}. Expected the {CredentialKey} account.",
            context.Request.Path,
            result,
            credentialKey
        );

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/xml; charset=utf-8";
        await context.Response.WriteAsync(UnauthenticatedFault, context.RequestAborted);
    }
}
