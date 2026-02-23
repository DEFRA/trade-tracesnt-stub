using System.Diagnostics.CodeAnalysis;

namespace Api.TradeTracesNTStub.Endpoints.Api;

[ExcludeFromCodeCoverage]
public static class EndpointRouteBuilderExtensions
{
    // All custom endpoints are exposed with /api/ prefix. The WireMock configuration will proxy all requests with this prefix through to these endpoints. 
    private const string ApiPrefix = "api";
    
    public static void UseSampleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet($"{ApiPrefix}/do-something", GetSomething);
    }

    private static IResult GetSomething()
    {
        return Results.Ok("Foo");
    }
}