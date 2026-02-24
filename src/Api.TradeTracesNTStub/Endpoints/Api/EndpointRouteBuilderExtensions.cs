using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Api.TradeTracesNTStub.Endpoints.Api;

[ExcludeFromCodeCoverage]
public static class EndpointRouteBuilderExtensions
{
    public static void UseSampleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("do-something", GetSomething);
    }

    [HttpGet]
    private static IResult GetSomething()
    {
        return Results.Ok("Foo");
    }
}