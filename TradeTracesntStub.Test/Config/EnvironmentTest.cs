using Microsoft.AspNetCore.Builder;

namespace TradeTracesntStub.Test.Config;

public class EnvironmentTest
{
    [Fact]
    public void IsNotDevModeByDefault()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var isDev = TradeTracesntStub.Config.Environment.IsDevMode(builder);
        Assert.False(isDev);
    }
}