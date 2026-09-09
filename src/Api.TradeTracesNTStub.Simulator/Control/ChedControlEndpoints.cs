using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Control.Templates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// The simple face of the simulator: plain JSON for setting up test data, on a prefix of its own.
/// </summary>
/// <remarks>
/// Deliberately not TRACES-shaped. A test author should not have to build a SOAP envelope to create a
/// fixture, and nothing here mirrors the XML schema — the mapping onto it happens in
/// <see cref="ChedCertificateFactory"/>. There is no read-back endpoint on purpose: the SOAP face is
/// the only way to read state back, which keeps the two from drifting.
/// </remarks>
public static class ChedControlEndpoints
{
    /// <summary>
    /// The prefix every control operation lives under. Chosen so it cannot collide with the five
    /// TRACES service paths, or with the WireMock stub's <c>/mock</c> and <c>/proxy</c>.
    /// </summary>
    public const string Prefix = "/control";

    public static IEndpointRouteBuilder MapChedControlApi(this IEndpointRouteBuilder app)
    {
        var control = app.MapGroup(Prefix).WithTags("Simulator control");

        control
            .MapPost("/cheds", Create)
            .WithSummary("Create a CHED")
            .WithDescription(
                "Layers the supplied fields onto a baseline template and stores the result. "
                    + "Everything except 'type' is optional. The stored representation is returned; "
                    + "read it back through the SOAP face with getChedCertificate."
            );

        control
            .MapPut("/cheds/{id}", Update)
            .WithSummary("Replace a CHED")
            .WithDescription("Rebuilds the CHED from the supplied model, keeping its ID.");

        control.MapDelete("/cheds/{id}", Delete).WithSummary("Delete a CHED");

        control
            .MapPost("/reset", Reset)
            .WithSummary("Reset simulator state")
            .WithDescription(
                "Clears every CHED. Pass ?fixtureSet=<name> to load a named set instead of ending empty."
            );

        control.MapGet("/fixture-sets", ListFixtureSets).WithSummary("List the available fixture sets");

        return app;
    }

    private static IResult Create(
        ChedControlModel model,
        ChedStore store,
        ChedCertificateFactory factory,
        ChedIds ids
    )
    {
        var id = string.IsNullOrWhiteSpace(model.Id) ? ids.Next(model.Type) : model.Id;

        return Store(model, id, store, factory) is { } problem ? problem : Results.Created($"{Prefix}/cheds/{id}", Describe(model, id));
    }

    private static IResult Update(
        string id,
        ChedControlModel model,
        ChedStore store,
        ChedCertificateFactory factory
    )
    {
        if (!store.TryGet(id, out _))
        {
            return Results.Problem($"No CHED '{id}' exists in the simulator.", statusCode: StatusCodes.Status404NotFound);
        }

        return Store(model, id, store, factory) is { } problem ? problem : Results.Ok(Describe(model, id));
    }

    private static IResult Delete(string id, ChedStore store) =>
        store.Remove(id)
            ? Results.NoContent()
            : Results.Problem($"No CHED '{id}' exists in the simulator.", statusCode: StatusCodes.Status404NotFound);

    private static IResult Reset(
        string? fixtureSet,
        ChedStore store,
        FixtureSets fixtures,
        ChedCertificateFactory factory,
        ChedIds ids
    )
    {
        store.Clear();

        if (string.IsNullOrWhiteSpace(fixtureSet))
        {
            return Results.Ok(new ResetResponse(null, 0));
        }

        if (!fixtures.Exists(fixtureSet))
        {
            return Results.Problem(
                $"No fixture set named '{fixtureSet}'. Available: {string.Join(", ", fixtures.Names)}.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        IReadOnlyList<ChedControlModel> models;

        try
        {
            models = fixtures.Load(fixtureSet);
        }
        catch (InvalidOperationException exception)
        {
            // A malformed fixture is the author's to fix, and the message names the file.
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        foreach (var model in models)
        {
            var id = string.IsNullOrWhiteSpace(model.Id) ? ids.Next(model.Type) : model.Id;

            if (Store(model, id, store, factory) is { } problem)
            {
                return problem;
            }
        }

        return Results.Ok(new ResetResponse(fixtureSet, store.Count));
    }

    private static IResult ListFixtureSets(FixtureSets fixtures) =>
        Results.Ok(new FixtureSetsResponse(fixtures.Names, [.. ChedTemplates.Names]));

    /// <summary>
    /// Builds and stores one CHED, or returns the problem to send back. An unknown code is a 400
    /// rather than a 500 because it is the caller's to fix, and the message says how.
    /// </summary>
    private static IResult? Store(ChedControlModel model, string id, ChedStore store, ChedCertificateFactory factory)
    {
        try
        {
            store.Put(new StoredChed(id, factory.Create(model, id), model.Accessible));
            return null;
        }
        catch (UnknownCodeException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static StoredChedResponse Describe(ChedControlModel model, string id) =>
        new(id, model.Type, model.Accessible, model with { Id = id });
}

/// <summary>What the control API returns for a stored CHED. The certificate itself is read back over SOAP.</summary>
public record StoredChedResponse(string Id, ChedType Type, bool Accessible, ChedControlModel Ched);

public record ResetResponse(string? FixtureSet, int ChedCount);

public record FixtureSetsResponse(IReadOnlyList<string> FixtureSets, IReadOnlyList<string> Templates);
