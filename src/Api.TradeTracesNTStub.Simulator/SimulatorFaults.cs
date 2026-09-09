using CoreWCF;

namespace Api.TradeTracesNTStub.Simulator;

public static class SimulatorFaults
{
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
