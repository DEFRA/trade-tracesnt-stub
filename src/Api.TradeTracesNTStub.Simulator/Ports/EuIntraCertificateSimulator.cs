using Api.TradeTracesNTStub.Simulator.Control;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class EuIntraCertificateSimulator(IIntraStore store) : EuIntraCertificatePort
{
    /// <summary>What TRACES's own consumers default to when a search names no page size.</summary>
    private const int DefaultPageSize = 10;

    /// <summary>
    /// Serves an INTRA the control API stored. The language argument is ignored — the simulator holds
    /// one language per certificate.
    /// </summary>
    public async Task<GetEuIntraCertificateResponse> getEuIntraCertificateAsync(
        GetEuIntraCertificateRequest request
    )
    {
        var id = request.GetEuIntraCertificateRequest1?.ID ?? "";
        var intra = await store.FindAsync(id) ?? throw SimulatorFaults.IntraNotFound(id);

        if (!intra.Accessible)
        {
            throw SimulatorFaults.IntraPermissionDenied(id);
        }

        return new GetEuIntraCertificateResponse(new EuIntraCertificateType { SPSCertificate = intra.Certificate });
    }

    public Task<GetEuIntraPdfCertificateResponse> getEuIntraPdfCertificateAsync(
        GetEuIntraPdfCertificateRequest request
    ) => throw SimulatorFaults.NotImplemented("getEuIntraPdfCertificate");

    public Task<GetEuIntraSignedPdfCertificateResponse> getEuIntraSignedPdfCertificateAsync(
        GetEuIntraSignedPdfCertificateRequest request
    ) => throw SimulatorFaults.NotImplemented("getEuIntraSignedPdfCertificate");

    /// <summary>
    /// The stored INTRAs a caller may see, by the same rules as CHED search: narrowed to an update-date
    /// range and nothing else, newest first, paged with 1-based offsets.
    /// </summary>
    public async Task<FindEuIntraCertificateResponse> findEuIntraCertificateAsync(
        FindEuIntraCertificateRequest request
    )
    {
        var query = request.FindEuIntraCertificateRequest1;
        var pageSize = query?.pageSize is > 0 ? query.pageSize : DefaultPageSize;
        var skip = Math.Max(0, (query?.offset ?? 1) - 1);
        var range = query?.UpdateDateTimeRange;

        var page = await store.SearchAsync(ChedSummary.Bound(range?.From), ChedSummary.Bound(range?.To), skip, pageSize);

        return new FindEuIntraCertificateResponse(
            new FindEuIntraCertificateResultType
            {
                EuIntraCertificateResult = [.. page.Select(intra => IntraSummary.Of(intra.Certificate))],
                offset = query?.offset ?? 1,
                pageSize = pageSize,
            }
        );
    }

    public Task<GetEuIntraCertificateStatusResponse> getEuIntraCertificateStatusAsync(
        GetEuIntraCertificateStatusRequest request
    ) => throw SimulatorFaults.NotImplemented("getEuIntraCertificateStatus");
}
