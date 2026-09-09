using CoreWCF;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator;

public static class SimulatorFaults
{
    /// <summary>
    /// The CHED does not exist. A typed fault, because the gateway matches on the detail type and
    /// turns this one into a 404 — an untyped fault would surface as a 502 instead.
    /// </summary>
    public static FaultException<ChedCertificateNotFoundExceptionType> ChedNotFound(string id) =>
        new(
            new ChedCertificateNotFoundExceptionType { CertificateIdentifier = id },
            new FaultReason("Certificate not found"),
            FaultCode.CreateSenderFaultCode("ChedCertificateNotFoundException", TracesNtServices.ChedV2Namespace)
        );

    /// <summary>
    /// The caller may not see this CHED. Also typed: this is the only fault the gateway maps to 403,
    /// which is why a rejected WS-Security header (untyped, a 502) cannot stand in for it.
    /// </summary>
    public static FaultException<ChedCertificatePermissionDeniedExceptionType> ChedPermissionDenied(string id) =>
        new(
            new ChedCertificatePermissionDeniedExceptionType { CertificateIdentifier = id },
            new FaultReason("Permission denied"),
            FaultCode.CreateSenderFaultCode(
                "ChedCertificatePermissionDeniedException",
                TracesNtServices.ChedV2Namespace
            )
        );

    /// <summary>
    /// The fault returned by every operation the simulator has a contract for but no behaviour behind.
    /// Deliberately a receiver fault naming the operation, so it cannot be confused with an
    /// authentication failure (a sender fault, <c>UnauthenticatedException</c>) or with a certificate
    /// that genuinely does not exist (a typed <c>*NotFoundException</c> fault).
    /// </summary>
    public static FaultException NotImplemented(string operation) =>
        new(
            new FaultReason($"NotImplementedException: {operation} is not implemented by the TRACES NT simulator"),
            FaultCode.CreateReceiverFaultCode("NotImplementedException", TracesNtServices.SimulatorFaultNamespace)
        );
}
