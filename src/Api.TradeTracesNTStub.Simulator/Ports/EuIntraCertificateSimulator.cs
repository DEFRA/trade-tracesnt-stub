using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class EuIntraCertificateSimulator : EuIntraCertificatePort
{
    public Task<GetEuIntraCertificateResponse> getEuIntraCertificateAsync(GetEuIntraCertificateRequest request) =>
        throw SimulatorFaults.NotImplemented("getEuIntraCertificate");

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
