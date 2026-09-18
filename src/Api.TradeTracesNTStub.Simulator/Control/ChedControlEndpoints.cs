using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// Plain JSON for setting up test data, so a test author need not build a SOAP envelope. There is no
/// read-back endpoint on purpose — the SOAP face is the only way to read state, so the two cannot drift.
/// </summary>
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
                "Stores exactly the certificate described, filling in only what TRACES itself fills "
                    + "in — display names, registry lookups and schema scaffolding. Read it back "
                    + "through the SOAP face with getChedCertificate."
            );

        control
            .MapPut("/cheds/{id}", Update)
            .WithSummary("Replace a CHED")
            .WithDescription("Rebuilds the CHED from the supplied model, keeping its ID.");

        control
            .MapPatch("/cheds/{id}", Patch)
            .WithSummary("Apply a partial update to a CHED")
            .WithDescription(
                "Merges the supplied fields into the stored CHED. Notes and clauses merge key by key, "
                    + "so submitting a decision means sending just the clearance block and the new "
                    + "status. Absent fields are left alone; use PUT to clear one."
            );

        control.MapDelete("/cheds/{id}", Delete).WithSummary("Delete a CHED");

        control
            .MapPost("/reset", Reset)
            .WithSummary("Reset simulator state")
            .WithDescription("Clears every CHED. A test states the CHEDs it needs rather than resetting to a set.");

        return app;
    }

    private static IResult Create(
        ChedControlModel model,
        ChedStore store,
        ChedCertificateBuilder builder
    ) =>
        Guarded(() =>
        {
            // The type comes out of the CHED_TYPE note, so reading it can fail the same way any
            // other lookup fails — inside the guard, not before it.
            var id = string.IsNullOrWhiteSpace(model.Id)
                ? store.NextId(ChedCertificateBuilder.ChedTypeOf(model))
                : model.Id;

            store.Put(new StoredChed(id, builder.Build(model, id), model.Accessible ?? true, model));

            return Results.Created($"{Prefix}/cheds/{id}", Describe(model, id));
        });

    private static IResult Update(
        string id,
        ChedControlModel model,
        ChedStore store,
        ChedCertificateBuilder builder
    )
    {
        if (!store.TryGet(id, out _))
        {
            return Results.Problem($"No CHED '{id}' exists in the simulator.", statusCode: StatusCodes.Status404NotFound);
        }

        return Guarded(() =>
        {
            store.Put(new StoredChed(id, builder.Build(model, id), model.Accessible ?? true, model));
            return Results.Ok(Describe(model, id));
        });
    }

    private static IResult Patch(
        string id,
        ChedControlModel patch,
        ChedStore store,
        ChedCertificateBuilder builder
    )
    {
        if (!store.TryGet(id, out var stored))
        {
            return Results.Problem($"No CHED '{id}' exists in the simulator.", statusCode: StatusCodes.Status404NotFound);
        }

        var merged = stored.Source.Merge(patch);

        return Guarded(() =>
        {
            store.Put(new StoredChed(id, builder.Build(merged, id), merged.Accessible ?? true, merged));
            return Results.Ok(Describe(merged, id));
        });
    }

    private static IResult Delete(string id, ChedStore store) =>
        store.Remove(id)
            ? Results.NoContent()
            : Results.Problem($"No CHED '{id}' exists in the simulator.", statusCode: StatusCodes.Status404NotFound);

    private static IResult Reset(ChedStore store)
    {
        store.Clear();

        return Results.Ok(new ResetResponse(store.Count));
    }

    /// <summary>
    /// Runs a handler, turning a code the simulator has no display name for into a 400 rather than a
    /// 500. It is the caller's to fix, and the message says which file to add it to.
    /// </summary>
    private static IResult Guarded(Func<IResult> act)
    {
        try
        {
            return act();
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
        new(id, ChedCertificateBuilder.ChedTypeOf(model), model.Accessible ?? true, model with { Id = id });
}

/// <summary>What the control API returns for a stored CHED. The certificate itself is read back over SOAP.</summary>
public record StoredChedResponse(string Id, string ChedType, bool Accessible, ChedControlModel Ched);

public record ResetResponse(int ChedCount);

