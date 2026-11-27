using System.Net;
using Api.TradeTracesNTStub.Simulator.Control.Commodities;
using Microsoft.Extensions.Options;

namespace TradeTracesNTStub.Test.Simulator.Commodities;

public class CommodityTreeSourceTests
{
    private sealed class Recording(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static CommodityTreeSource Source(Recording handler, string? apiKey = null) =>
        new(
            new HttpClient(handler),
            Options.Create(new CommodityTreeOptions { SourceUrl = "http://poc:8080/", ApiKey = apiKey })
        );

    [Fact]
    public async Task ItAsksThePocForTheTreeAsJsonAndReadsTheNodes()
    {
        var handler = new Recording(HttpStatusCode.OK, CommodityTreeSample.Json);

        var nodes = await Source(handler).FetchAsync("cheda", TestContext.Current.CancellationToken);

        handler.Request!.RequestUri!.ToString().Should().Be("http://poc:8080/classificationTrees/cheda/?format=json");
        nodes.Should().BeEquivalentTo(CommodityTreeSample.Nodes);
    }

    [Fact]
    public async Task ItSendsTheDeveloperKeyOnlyWhenThereIsOne()
    {
        var with = new Recording(HttpStatusCode.OK, "[]");
        var without = new Recording(HttpStatusCode.OK, "[]");

        await Source(with, "key").FetchAsync("cheda", TestContext.Current.CancellationToken);
        await Source(without).FetchAsync("cheda", TestContext.Current.CancellationToken);

        with.Request!.Headers.GetValues("x-api-key").Should().Equal("key");
        without.Request!.Headers.Contains("x-api-key").Should().BeFalse();
    }

    [Fact]
    public async Task AFailedFetchThrows()
    {
        var act = () =>
            Source(new Recording(HttpStatusCode.BadGateway, "")).FetchAsync("cheda", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
