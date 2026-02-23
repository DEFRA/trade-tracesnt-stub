using System.Diagnostics.CodeAnalysis;

namespace Api.TradeTracesNTStub.Endpoints.Api;

[ExcludeFromCodeCoverage]
public static class EndpointRouteBuilderExtensions
{
    public static void UseSampleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("do-something", GetSomething);
    }

    private static IResult GetSomething()
    {
        return Results.Ok("Foo");
    }
}