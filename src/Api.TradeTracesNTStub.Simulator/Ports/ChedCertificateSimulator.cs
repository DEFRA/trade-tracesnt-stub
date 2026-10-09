using Api.TradeTracesNTStub.Simulator.Control;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class ChedCertificateSimulator(IChedStore store) : ChedCertificatePort
{
    /// <summary>What TRACES's own consumers default to when a search names no page size.</summary>
    private const int DefaultPageSize = 10;

    /// <summary>
    /// Serves a CHED the control API stored. The language argument is ignored — the simulator holds
    /// one language per certificate.
    /// </summary>
    public async Task<GetChedCertificateResponse> getChedCertificateAsync(GetChedCertificateRequest request)
    {
        var id = request.GetChedCertificateRequest1?.ID ?? "";
        var ched = await store.FindAsync(id) ?? throw SimulatorFaults.ChedNotFound(id);

        if (!ched.Accessible)
        {
            throw SimulatorFaults.ChedPermissionDenied(id);
        }

        return new GetChedCertificateResponse(new ChedCertificateType { SPSCertificate = ched.Certificate });
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

    /// <summary>
    /// The stored CHEDs a caller may see, narrowed to an update-date range, newest first, paged with
    /// 1-based offsets. Update date is the only criterion honoured: the request carries two dozen
    /// more, and honouring some but not others would leave a caller unable to tell which applied. A
    /// test that wants one CHED asks for it by ID.
    /// </summary>
    public async Task<FindChedCertificateResponse> findChedCertificateAsync(FindChedCertificateRequest request)
    {
        var query = request.FindChedCertificateRequest1;
        var pageSize = query?.pageSize is > 0 ? query.pageSize : DefaultPageSize;
        var skip = Math.Max(0, (query?.offset ?? 1) - 1);
        var range = query?.UpdateDateTimeRange;

        // Only the page asked for is read: on a shared environment the store holds tens of thousands.
        var page = await store.SearchAsync(ChedSummary.Bound(range?.From), ChedSummary.Bound(range?.To), skip, pageSize);

        return new FindChedCertificateResponse(
            new FindChedCertificateResultType
            {
                ChedCertificateResult = [.. page.Select(ched => ChedSummary.Of(ched.Certificate))],
                offset = query?.offset ?? 1,
                pageSize = pageSize,
            }
        );
    }

    public Task<GetChedCertificateStatusResponse> getChedCertificateStatusAsync(
        GetChedCertificateStatusRequest request
    ) => throw SimulatorFaults.NotImplemented("getChedCertificateStatus");
}
