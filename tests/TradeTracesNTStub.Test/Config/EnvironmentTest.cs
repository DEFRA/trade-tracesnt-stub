using Microsoft.AspNetCore.Builder;

namespace TradeTracesNTStub.Test.Config;

public class EnvironmentTest
{
    [Fact]
    public void IsNotDevModeByDefault()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var isDev = Api.TradeTracesNTStub.Config.Environment.IsDevMode(builder);
        Assert.False(isDev);
    }
}