using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class ChedCertificateSimulator : ChedCertificatePort
{
    public Task<GetChedCertificateResponse> getChedCertificateAsync(GetChedCertificateRequest request) =>
        throw SimulatorFaults.NotImplemented("getChedCertificate");

    public Task<GetChedFollowUpResponse> getChedFollowUpAsync(GetChedFollowUpRequest request) =>
        throw SimulatorFaults.NotImplemented("getChedFollowUp");

    public Task<GetChedNonComplianceDetailsResponse> getChedNonComplianceDetailsAsync(
        GetChedNonComplianceDetailsRequest request
    ) => throw SimulatorFaults.NotImplemented("getChedNonComplianceDetails");

    public Task<GetChedLaboratoryTestsResponse> getChedLaboratoryTestsAsync(GetChedLaboratoryTestsRequest request) =>
        throw SimulatorFaults.NotImplemented("getChedLaboratoryTests");

    public Task<GetChedPdfCertificateResponse> getChedPdfCertificateAsync(GetChedPdfCertificateRequest request) =>
        throw SimulatorFaults.NotImplemented("getChedPdfCertificate");

    public Task<GetChedSignedPdfCertificateResponse> getChedSignedPdfCertificateAsync(
        GetChedSignedPdfCertificateRequest request
    ) => throw SimulatorFaults.NotImplemented("getChedSignedPdfCertificate");

    public Task<GetChedSignedXmlCertificateResponse> getChedSignedXmlCertificateAsync(
        GetChedSignedXmlCertificateRequest request
    ) => throw SimulatorFaults.NotImplemented("getChedSignedXmlCertificate");

    public Task<FindChedCertificateResponse> findChedCertificateAsync(FindChedCertificateRequest request) =>
        throw SimulatorFaults.NotImplemented("findChedCertificate");

    public Task<GetChedCertificateStatusResponse> getChedCertificateStatusAsync(
        GetChedCertificateStatusRequest request
    ) => throw SimulatorFaults.NotImplemented("getChedCertificateStatus");
}
