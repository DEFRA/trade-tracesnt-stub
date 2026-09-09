using Api.TradeTracesNTStub.Simulator.Control;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class ChedCertificateSimulator(ChedStore store) : ChedCertificatePort
{
    /// <summary>
    /// Serves a CHED the control API stored.
    /// </summary>
    /// <remarks>
    /// The language argument is accepted and ignored: it is a non-nullable enum, so it is always on
    /// the wire, but the simulator holds one language per certificate and does not vary by it.
    /// </remarks>
    public Task<GetChedCertificateResponse> getChedCertificateAsync(GetChedCertificateRequest request)
    {
        var id = request.GetChedCertificateRequest1?.ID ?? "";

        if (!store.TryGet(id, out var ched))
        {
            throw SimulatorFaults.ChedNotFound(id);
        }

        if (!ched.Accessible)
        {
            throw SimulatorFaults.ChedPermissionDenied(id);
        }

        return Task.FromResult(
            new GetChedCertificateResponse(new ChedCertificateType { SPSCertificate = ched.Certificate })
        );
    }

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
