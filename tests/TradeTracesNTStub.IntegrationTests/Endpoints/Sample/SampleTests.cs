using System.Net;
using FluentAssertions;

namespace TradeTracesNTStub.IntegrationTests.Endpoints.Sample;

public class SampleTests : IntegrationTestBase
{
    [Fact]
    public async Task Sample_ShouldBeOk()
    {
        var response = await Client.GetAsync("/do-something", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be("Foo");
    }
}