using Api.TradeTracesNTStub.Simulator.Control;
using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class ChedCertificateSimulator(ChedStore store) : ChedCertificatePort
{
    /// <summary>What TRACES's own consumers default to when a search names no page size.</summary>
    private const int DefaultPageSize = 10;

    /// <summary>
    /// Serves a CHED the control API stored. The language argument is ignored — the simulator holds
    /// one language per certificate.
    /// </summary>
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

    /// <summary>
    /// The stored CHEDs a caller may see, narrowed to an update-date range, newest first, paged with
    /// 1-based offsets. Update date is the only criterion honoured: the request carries two dozen
    /// more, and honouring some but not others would leave a caller unable to tell which applied. A
    /// test that wants one CHED asks for it by ID.
    /// </summary>
    public Task<FindChedCertificateResponse> findChedCertificateAsync(FindChedCertificateRequest request)
    {
        var query = request.FindChedCertificateRequest1;
        var pageSize = query?.pageSize is > 0 ? query.pageSize : DefaultPageSize;
        var skip = Math.Max(0, (query?.offset ?? 1) - 1);

        // Not accessible means the caller may not see it, in search as much as in retrieval.
        var results = store
            .All.Where(ched => ched.Accessible)
            .Select(ched => ChedSummary.Of(ched.Certificate))
            .Where(summary => UpdatedWithin(query?.UpdateDateTimeRange, summary.UpdateDateTime))
            .OrderByDescending(summary => summary.UpdateDateTime)
            // The ID breaks ties. Two CHEDs stamped in the same tick could otherwise swap places
            // between two calls, and a caller paging through would see one twice and the other never.
            .ThenByDescending(summary => summary.ID, StringComparer.Ordinal)
            .Skip(skip)
            .Take(pageSize);

        return Task.FromResult(
            new FindChedCertificateResponse(
                new FindChedCertificateResultType
                {
                    ChedCertificateResult = [.. results],
                    offset = query?.offset ?? 1,
                    pageSize = pageSize,
                }
            )
        );
    }

    /// <summary>
    /// Both bounds are inclusive, and a bound left at its default is no bound — <c>From</c> and
    /// <c>To</c> are plain datetimes with no companion <c>Specified</c> flag, so an omitted one
    /// arrives as <c>0001-01-01</c> and a <c>To</c> read literally would match nothing. Nothing DG
    /// SANTE publish makes the range half-open, and inventing an exclusive end here would hide a
    /// gateway paging bug rather than expose it.
    /// </summary>
    private static bool UpdatedWithin(DateTimeRange? range, DateTime updated)
    {
        if (range is null)
        {
            return true;
        }

        var moment = Utc(updated);

        return (range.From == default || moment >= Utc(range.From))
            && (range.To == default || moment <= Utc(range.To));
    }

    /// <summary>
    /// The search bounds arrive at the host's offset while stored update times are UTC, and comparing
    /// the two unconverted compares wall-clock readings from different clocks.
    /// </summary>
    private static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

    public Task<GetChedCertificateStatusResponse> getChedCertificateStatusAsync(
        GetChedCertificateStatusRequest request
    ) => throw SimulatorFaults.NotImplemented("getChedCertificateStatus");
}
