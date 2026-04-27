namespace TradeTracesNTStub.IntegrationTests;

[Trait("Category", "IntegrationTest")]
public abstract class IntegrationTestBase
{
    protected readonly HttpClient Client = new() { BaseAddress = new Uri("http://localhost:8085") };
}