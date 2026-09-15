using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class DocomCertificateRetrievalSimulator : DocomCertificateRetrievalPort
{
    public Task<GetDocomCertificateResponse> getDocomCertificateAsync(GetDocomCertificateRequest request) =>
        throw SimulatorFaults.NotImplemented("getDocomCertificate");

    public Task<GetDocomPdfCertificateResponse> getDocomPdfCertificateAsync(GetDocomPdfCertificateRequest request) =>
        throw SimulatorFaults.NotImplemented("getDocomPdfCertificate");

    public Task<FindDocomCertificateResponse> findDocomCertificateAsync(FindDocomCertificateRequest request) =>
        throw SimulatorFaults.NotImplemented("findDocomCertificate");
}
