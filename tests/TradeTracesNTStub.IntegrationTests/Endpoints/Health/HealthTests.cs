using System.Net;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Health;

public class HealthTests : IntegrationTestBase
{
    [Fact]
    public async Task Health_ShouldBeOk()
    {
        var response = await Client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}