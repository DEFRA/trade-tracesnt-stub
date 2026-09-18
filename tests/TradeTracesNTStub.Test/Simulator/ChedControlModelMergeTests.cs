using Api.TradeTracesNTStub.Simulator.Control.Models;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// The merge is why notes and clauses are keyed objects rather than lists. Applying a decision should
/// be sending a clearance block and a status, not restating the certificate — these tests pin that,
/// and pin what a patch must not quietly discard.
/// </summary>
public class ChedControlModelMergeTests
{
    private static ChedControlModel Stored =>
        new()
        {
            Id = "CHEDA.XI.2026.0000001",
            Status = "NEW",
            ExchangedDocument = new ExchangedDocumentModel
            {
                IncludedNote = new Dictionary<string, string> { ["CHED_TYPE"] = "A" },
                Declaration = new AuthenticationModel
                {
                    IncludedClause = new Dictionary<string, string> { ["PURPOSE"] = "FREE_CIRCULATION" },
                },
            },
            SpecifiedConsignment = new ConsignmentModel
            {
                ExportCountry = "AF",
                UnloadingBaseportLocation = new LocationModel { Identifier = "GBBEL", CountryId = "XI" },
            },
        };

    [Fact]
    public void ADecisionIsJustAClearanceBlockAndAStatus()
    {
        var patch = new ChedControlModel
        {
            Status = "VALIDATED",
            ExchangedDocument = new ExchangedDocumentModel
            {
                Clearance = new AuthenticationModel
                {
                    IncludedClause = new Dictionary<string, string>
                    {
                        ["DECISION_CONCLUSION"] = "ACCEPTABLE_FOR_FREE_CIRCULATION",
                    },
                },
            },
        };

        var merged = Stored.Merge(patch);

        merged.Status.Should().Be("VALIDATED");
        merged.ExchangedDocument.Clearance.Should().NotBeNull();

        // Everything the patch did not mention survives.
        merged.ExchangedDocument.Declaration!.IncludedClause.Should().ContainKey("PURPOSE");
        merged.ExchangedDocument.IncludedNote.Should().ContainKey("CHED_TYPE");
        merged.SpecifiedConsignment.ExportCountry.Should().Be("AF");
        merged.SpecifiedConsignment.UnloadingBaseportLocation!.Identifier.Should().Be("GBBEL");
    }

    [Fact]
    public void ClausesMergeKeyByKeyRatherThanReplacingTheBlock()
    {
        // Flipping one check result should not drop the rest of the decision.
        var decided = Stored.Merge(
            new ChedControlModel
            {
                ExchangedDocument = new ExchangedDocumentModel
                {
                    Clearance = new AuthenticationModel
                    {
                        IncludedClause = new Dictionary<string, string>
                        {
                            ["DECISION_CONCLUSION"] = "ACCEPTABLE_FOR_FREE_CIRCULATION",
                            ["PHYSICAL_CHECK_RESULT"] = "SATISFACTORY",
                        },
                    },
                },
            }
        );

        var amended = decided.Merge(
            new ChedControlModel
            {
                ExchangedDocument = new ExchangedDocumentModel
                {
                    Clearance = new AuthenticationModel
                    {
                        IncludedClause = new Dictionary<string, string> { ["PHYSICAL_CHECK_RESULT"] = "NOT_SATISFACTORY" },
                    },
                },
            }
        );

        amended.ExchangedDocument.Clearance!.IncludedClause.Should()
            .Contain("PHYSICAL_CHECK_RESULT", "NOT_SATISFACTORY")
            .And.ContainKey("DECISION_CONCLUSION");
    }

    [Fact]
    public void AnAbsentAccessibleFlagDoesNotReEnableAccess()
    {
        // The trap this guards: Accessible is nullable precisely so a patch that says nothing about
        // it leaves a deliberately unreadable CHED unreadable.
        var hidden = Stored with { Accessible = false };

        hidden.Merge(new ChedControlModel { Status = "VALIDATED" }).Accessible.Should().BeFalse();
    }

    [Fact]
    public void CommoditiesReplaceAsASet()
    {
        // Merging two lists positionally would be guesswork, so a patch that names commodities
        // replaces them outright.
        var stored = Stored with
        {
            SpecifiedConsignment = Stored.SpecifiedConsignment with
            {
                IncludedConsignmentItem = new ConsignmentItemModel
                {
                    IncludedTradeLineItem = [new TradeLineItemModel { OriginCountry = "AF" }],
                },
            },
        };

        var merged = stored.Merge(
            new ChedControlModel
            {
                SpecifiedConsignment = new ConsignmentModel
                {
                    IncludedConsignmentItem = new ConsignmentItemModel
                    {
                        IncludedTradeLineItem = [new TradeLineItemModel { OriginCountry = "GB" }],
                    },
                },
            }
        );

        merged
            .SpecifiedConsignment.IncludedConsignmentItem!.IncludedTradeLineItem.Should()
            .ContainSingle()
            .Which.OriginCountry.Should()
            .Be("GB");
    }
}
