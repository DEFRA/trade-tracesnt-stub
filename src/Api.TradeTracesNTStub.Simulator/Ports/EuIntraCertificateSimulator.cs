using Api.TradeTracesNTStub.Simulator.Control;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class EuIntraCertificateSimulator(IntraStore store) : EuIntraCertificatePort
{
    /// <summary>
    /// Serves an INTRA the control API stored. The language argument is ignored — the simulator holds
    /// one language per certificate.
    /// </summary>
    public Task<GetEuIntraCertificateResponse> getEuIntraCertificateAsync(GetEuIntraCertificateRequest request)
    {
        var id = request.GetEuIntraCertificateRequest1?.ID ?? "";

        if (!store.TryGet(id, out var intra))
        {
            throw SimulatorFaults.IntraNotFound(id);
        }

        if (!intra.Accessible)
        {
            throw SimulatorFaults.IntraPermissionDenied(id);
        }

        return Task.FromResult(
            new GetEuIntraCertificateResponse(new EuIntraCertificateType { SPSCertificate = intra.Certificate })
        );
    }

    public Task<GetEuIntraPdfCertificateResponse> getEuIntraPdfCertificateAsync(
        GetEuIntraPdfCertificateRequest request
    ) => throw SimulatorFaults.NotImplemented("getEuIntraPdfCertificate");

    public Task<GetEuIntraSignedPdfCertificateResponse> getEuIntraSignedPdfCertificateAsync(
        GetEuIntraSignedPdfCertificateRequest request
    ) => throw SimulatorFaults.NotImplemented("getEuIntraSignedPdfCertificate");

    public Task<FindEuIntraCertificateResponse> findEuIntraCertificateAsync(FindEuIntraCertificateRequest request) =>
        throw SimulatorFaults.NotImplemented("findEuIntraCertificate");

    public Task<GetEuIntraCertificateStatusResponse> getEuIntraCertificateStatusAsync(
        GetEuIntraCertificateStatusRequest request
    ) => throw SimulatorFaults.NotImplemented("getEuIntraCertificateStatus");
}
