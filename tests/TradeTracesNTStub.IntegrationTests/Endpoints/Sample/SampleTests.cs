using System.Net;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Sample;

public class SampleTests
{
    [Fact]
    public async Task Sample_ShouldBeOk()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        var response = await client.GetAsync("/do-something", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be("Foo");
    }
}