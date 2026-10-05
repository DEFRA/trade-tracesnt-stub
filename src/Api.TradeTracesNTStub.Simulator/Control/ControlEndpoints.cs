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
public static class ControlEndpoints
{
    /// <summary>
    /// The prefix every control operation lives under. Chosen so it cannot collide with the five
    /// TRACES service paths, or with the WireMock stub's <c>/mock</c> and <c>/proxy</c>.
    /// </summary>
    public const string Prefix = "/control";

    public static IEndpointRouteBuilder MapControlApi(this IEndpointRouteBuilder app)
    {
        var control = app.MapGroup(Prefix).WithTags("Simulator control");

        MapCertificates<IChedStore>(control, "cheds", CertificateKind.Ched, "getChedCertificate");
        MapCertificates<IIntraStore>(control, "intras", CertificateKind.Intra, "getEuIntraCertificate");

        control
            .MapPost("/reset", Reset)
            .WithSummary("Reset simulator state")
            .WithDescription(
                "Clears every CHED and INTRA, and every customs reservation against them. A test states the certificates it needs rather than resetting to a set."
            )
            .Produces<ResetResponse>();

        return app;
    }

    /// <summary>The same four operations for each kind; only the path and the SOAP operation named differ.</summary>
    private static void MapCertificates<TStore>(
        RouteGroupBuilder control,
        string collection,
        CertificateKind kind,
        string soapOperation
    )
        where TStore : ICertificateStore
    {
        control
            .MapPost(
                $"/{collection}",
                (CertificateControlModel model, TStore store, SpsCertificateBuilder builder) =>
                    Create(kind, collection, model, store, builder)
            )
            .WithSummary($"Create a {kind.Name}")
            .WithDescription(
                "Stores exactly the certificate described, filling in only what TRACES itself fills "
                    + "in — display names, registry lookups and schema scaffolding. Read it back "
                    + $"through the SOAP face with {soapOperation}. The simulator issues the ID, as "
                    + "TRACES does; it is in the response."
            )
            .Produces<StoredCertificateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        control
            .MapPut(
                $"/{collection}/{{id}}",
                (string id, CertificateControlModel model, TStore store, SpsCertificateBuilder builder) =>
                    Update(kind, id, model, store, builder)
            )
            .WithSummary($"Replace a {kind.Name}")
            .WithDescription("Rebuilds the certificate from the supplied model, keeping its ID.")
            .Produces<StoredCertificateResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        control
            .MapPatch(
                $"/{collection}/{{id}}",
                (string id, CertificateControlModel patch, TStore store, SpsCertificateBuilder builder) =>
                    Patch(kind, id, patch, store, builder)
            )
            .WithSummary($"Apply a partial update to a {kind.Name}")
            .WithDescription(
                "Merges the supplied fields into the stored certificate. Notes and clauses merge key by "
                    + "key, so submitting a decision means sending just the clearance block and the new "
                    + "status. Absent fields are left alone; use PUT to clear one."
            )
            .Produces<StoredCertificateResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        control
            .MapDelete(
                $"/{collection}/{{id}}",
                async (string id, TStore store, ICustomsLedger ledger) =>
                {
                    // A CHED issued again under this ID must not inherit the old one's reservations.
                    await ledger.ForgetAsync(id);
                    return await store.RemoveAsync(id) ? Results.NoContent() : NotFound(kind, id);
                }
            )
            .WithSummary($"Delete a {kind.Name}")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static Task<IResult> Create(
        CertificateKind kind,
        string collection,
        CertificateControlModel model,
        ICertificateStore store,
        SpsCertificateBuilder builder
    ) =>
        Guarded(async () =>
        {
            // The prefix can come out of a note, so reading it can fail the same way any other
            // lookup fails — inside the guard, not before it.
            var id = await store.NextIdAsync(kind.IdPrefix(model));

            await store.PutAsync(Stored(kind, id, model, builder));

            return Results.Created($"{Prefix}/{collection}/{id}", Describe(model, id));
        });

    private static async Task<IResult> Update(
        CertificateKind kind,
        string id,
        CertificateControlModel model,
        ICertificateStore store,
        SpsCertificateBuilder builder
    )
    {
        if (await store.FindAsync(id) is null)
        {
            return NotFound(kind, id);
        }

        return await Guarded(async () =>
        {
            await store.PutAsync(Stored(kind, id, model, builder));
            return Results.Ok(Describe(model, id));
        });
    }

    private static async Task<IResult> Patch(
        CertificateKind kind,
        string id,
        CertificateControlModel patch,
        ICertificateStore store,
        SpsCertificateBuilder builder
    )
    {
        if (await store.FindAsync(id) is not { } stored)
        {
            return NotFound(kind, id);
        }

        var merged = stored.Source.Merge(patch);

        return await Guarded(async () =>
        {
            await store.PutAsync(Stored(kind, id, merged, builder));
            return Results.Ok(Describe(merged, id));
        });
    }

    private static async Task<IResult> Reset(IChedStore cheds, IIntraStore intras, ICustomsLedger ledger)
    {
        await cheds.ClearAsync();
        await intras.ClearAsync();
        await ledger.ClearAsync();

        return Results.Ok(new ResetResponse(await cheds.CountAsync() + await intras.CountAsync()));
    }

    private static StoredCertificate Stored(
        CertificateKind kind,
        string id,
        CertificateControlModel model,
        SpsCertificateBuilder builder
    ) => new(id, builder.Build(kind, model, id), model.Accessible ?? true, model);

    private static IResult NotFound(CertificateKind kind, string id) =>
        Results.Problem($"No {kind.Name} '{id}' exists in the simulator.", statusCode: StatusCodes.Status404NotFound);

    /// <summary>
    /// Runs a handler, turning a code the simulator has no display name for into a 400 rather than a
    /// 500. It is the caller's to fix, and the message says which file to add it to.
    /// </summary>
    private static async Task<IResult> Guarded(Func<Task<IResult>> act)
    {
        try
        {
            return await act();
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

    private static StoredCertificateResponse Describe(CertificateControlModel model, string id) =>
        new(id, model.Accessible ?? true, model);
}

/// <summary>What the control API returns for a stored certificate. The certificate itself is read back over SOAP.</summary>
public record StoredCertificateResponse(string Id, bool Accessible, CertificateControlModel Certificate);

public record ResetResponse(int CertificateCount);
