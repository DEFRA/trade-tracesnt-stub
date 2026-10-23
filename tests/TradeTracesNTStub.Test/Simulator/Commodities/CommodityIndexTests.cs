using Api.TradeTracesNTStub.Simulator.Control.Commodities;
using Api.TradeTracesNTStub.Simulator.Control.Lookups;

namespace TradeTracesNTStub.Test.Simulator.Commodities;

public class CommodityIndexTests
{
    private readonly CommodityIndex _index = new CommodityIndex(CommodityTreeSample.Tree());

    [Fact]
    public void ACodeIsNamedByItsCodedAncestorsThenItself()
    {
        // As TRACES serves 0101 in a real CHED: "LIVE ANIMALS", then the heading's own description.
        _index
            .NamesOf("010410")
            .Should()
            .Equal(
                new LocalisedName("LIVE ANIMALS", "en"),
                new LocalisedName("Live sheep and goats", "en"),
                new LocalisedName("Sheep", "en")
            );
    }

    [Fact]
    public void ALabelGroupsCodesButIsNotInTheirPath()
    {
        _index.NamesOf("01061900").Select(name => name.Value).Should().NotContain("Cats, Dogs and ferrets.");
    }

    [Fact]
    public void ACodeTwiceInTheTreeTakesItsFirstOccurrence()
    {
        _index.NamesOf("01061900").Select(name => name.Value).Should().Equal("LIVE ANIMALS", "Other");
    }

    [Fact]
    public void ACodeNotMarkedSelectableIsStillValid()
    {
        // The CHED-D tree marks nothing selectable, so selectability can't decide validity.
        _index.Contains("0104").Should().BeTrue();
        _index.Contains("01").Should().BeTrue();
    }

    [Fact]
    public void ACodeNotInTheTreeNamesTheTreeAndHowToRefreshIt()
    {
        var act = () => _index.NamesOf("9999");

        act.Should()
            .Throw<UnknownCodeException>()
            .Which.Message.Should()
            .Contain("CHED-A commodity tree (cheda")
            .And.Contain("POST /control/commodity-trees/refresh");
    }

    [Fact]
    public void ItCountsEachCodeOnce()
    {
        _index.Count.Should().Be(5);
        CommodityTrees.Summarise(CommodityTreeSample.Tree()).Should().Match<CommodityTreeSummary>(s => s.Nodes == 7 && s.Codes == 5);
    }
}
