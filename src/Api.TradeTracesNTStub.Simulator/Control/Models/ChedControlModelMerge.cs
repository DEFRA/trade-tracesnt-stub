namespace Api.TradeTracesNTStub.Simulator.Control.Models;

/// <summary>
/// Applies a partial CHED onto a stored one. Absent fields are left alone — JSON cannot tell absent
/// from null here, so a patch cannot clear one; use <c>PUT</c>.
/// </summary>
/// <remarks>
/// Hand-written rather than reflected over because the cases are not uniform: notes and clauses merge
/// key by key, parties and commodity lists replace wholesale. A generic deep merge would hide that.
/// </remarks>
public static class ChedControlModelMerge
{
    public static ChedControlModel Merge(this ChedControlModel stored, ChedControlModel patch) =>
        stored with
        {
            Status = patch.Status ?? stored.Status,
            Accessible = patch.Accessible ?? stored.Accessible,
            ExchangedDocument = MergeDocument(stored.ExchangedDocument, patch.ExchangedDocument),
            SpecifiedConsignment = MergeConsignment(stored.SpecifiedConsignment, patch.SpecifiedConsignment),
        };

    private static ExchangedDocumentModel MergeDocument(ExchangedDocumentModel stored, ExchangedDocumentModel patch) =>
        stored with
        {
            IncludedNote = MergeEntries(stored.IncludedNote, patch.IncludedNote),
            ReferenceDocument = patch.ReferenceDocument ?? stored.ReferenceDocument,
            Declaration = MergeAuthentication(stored.Declaration, patch.Declaration),
            Clearance = MergeAuthentication(stored.Clearance, patch.Clearance),
        };

    /// <summary>
    /// An authentication merges clause by clause, so a patch can flip one check result without
    /// resending the decision.
    /// </summary>
    private static AuthenticationModel? MergeAuthentication(AuthenticationModel? stored, AuthenticationModel? patch)
    {
        if (patch is null)
        {
            return stored;
        }

        if (stored is null)
        {
            return patch;
        }

        return stored with
        {
            ActualDateTime = patch.ActualDateTime ?? stored.ActualDateTime,
            IncludedClause = MergeEntries(stored.IncludedClause, patch.IncludedClause),
        };
    }

    private static ConsignmentModel MergeConsignment(ConsignmentModel stored, ConsignmentModel patch) =>
        stored with
        {
            AvailabilityDueDateTime = patch.AvailabilityDueDateTime ?? stored.AvailabilityDueDateTime,
            ExportCountry = patch.ExportCountry ?? stored.ExportCountry,
            ImportCountry = patch.ImportCountry ?? stored.ImportCountry,
            ConsignorParty = patch.ConsignorParty ?? stored.ConsignorParty,
            ConsigneeParty = patch.ConsigneeParty ?? stored.ConsigneeParty,
            DeliveryParty = patch.DeliveryParty ?? stored.DeliveryParty,
            CustomsTransitAgentParty = patch.CustomsTransitAgentParty ?? stored.CustomsTransitAgentParty,
            UnloadingBaseportLocation = patch.UnloadingBaseportLocation ?? stored.UnloadingBaseportLocation,
            MainCarriageLogisticsTransportMovement =
                patch.MainCarriageLogisticsTransportMovement ?? stored.MainCarriageLogisticsTransportMovement,
            // Commodities replace as a set: merging two lists positionally would be guesswork.
            IncludedConsignmentItem = patch.IncludedConsignmentItem ?? stored.IncludedConsignmentItem,
        };

    private static IReadOnlyDictionary<string, string> MergeEntries(
        IReadOnlyDictionary<string, string> stored,
        IReadOnlyDictionary<string, string> patch
    )
    {
        if (patch.Count == 0)
        {
            return stored;
        }

        var merged = new Dictionary<string, string>(stored, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in patch)
        {
            merged[key] = value;
        }

        return merged;
    }
}
