using System.Net;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Health;

public class HealthTests
{
    [Fact]
    public async Task Health_ShouldBeOk()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8085") };

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}