using Api.TradeTracesNTStub.Extensions;
using WireMock.Server;

namespace Api.TradeTracesNTStub.Hosts;

public class WireMockHostedService : IHostedService
{
    private WireMockServer? _server;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server = WireMockServer.StartWithAdminInterface(1080);

        _server.CreateStubMappings();
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Stop();
        return Task.CompletedTask;
    }
}