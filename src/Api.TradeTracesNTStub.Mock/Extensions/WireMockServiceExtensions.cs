using Api.TradeTracesNTStub.Mock.Hosts;
using Microsoft.Extensions.DependencyInjection;

namespace Api.TradeTracesNTStub.Mock.Extensions;

public static class WireMockServiceExtensions
{
    public static IServiceCollection AddWireMockHostedService(this IServiceCollection services)
    {
        services.AddHostedService<WireMockHostedService>();
        return services;
    }
}