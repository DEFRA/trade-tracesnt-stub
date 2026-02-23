using Api.TradeTracesNTStub.Hosts;

namespace Api.TradeTracesNTStub.Extensions;

public static class WireMockServiceExtensions
{
    public static IServiceCollection AddWireMockHostedService(this IServiceCollection services)
    {
        services.AddHostedService<WireMockHostedService>();
        return services;
    }
}