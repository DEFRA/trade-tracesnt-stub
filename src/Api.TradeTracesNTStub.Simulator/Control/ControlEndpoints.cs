using System.ComponentModel.DataAnnotations;
using Api.TradeTracesNTStub.Simulator.Control.Commodities;
using Api.TradeTracesNTStub.Simulator.Control.Lookups;
using Api.TradeTracesNTStub.Simulator.Control.Mapping;
using Api.TradeTracesNTStub.Simulator.Control.Models;
using Api.TradeTracesNTStub.Simulator.Control.Scenarios;
using Api.TradeTracesNTStub.Simulator.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.TradeTracesNTStub.Simulator.Control;

/// <summary>
/// Plain JSON for setting up test data, so a test author need not build a SOAP envelope. Reading a
/// certificate stays on the SOAP face, so the two cannot drift. What the control API reads back is
/// the model a certificate was built from, so a UI can edit it, and the code lists, so a UI offers
/// only codes the simulator accepts.
/// </summary>
public static class ControlEndpoints
{
    /// <summary>
    /// The prefix every control operation lives under. Chosen so it cannot collide with the five
    /// TRACES service paths, or with the WireMock stub's <c>/mock</c> and <c>/proxy</c>.
    /// </summary>
    public const string Prefix = "/control";

    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapControlApi(this IEndpointRouteBuilder app)
    {
        var control = app.MapGroup(Prefix).WithTags("Simulator control");

        MapCertificates<IChedStore>(control, "cheds", CertificateKind.Ched, "getChedCertificate");
        MapCertificates<IIntraStore>(control, "intras", CertificateKind.Intra, "getEuIntraCertificate");

        MapLookups(control);
        control.MapCommodityTrees();
        control.MapScenarios();

        control
            .MapPost("/reset", Reset)
            .WithSummary("Reset simulator state")
            .WithDescription(
                "Clears every CHED and INTRA, and every customs reservation against them. A test states the certificates it needs rather than resetting to a set."
            )
            .Produces<ResetResponse>();

        return app;
    }

    /// <summary>The same six operations for each kind; only the path and the SOAP operation named differ.</summary>
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
                (CertificateControlModel model, TStore store, SpsCertificateBuilder builder, CommodityCatalogue catalogue) =>
                    Create(kind, collection, model, store, builder, catalogue)
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
            .MapGet(
                $"/{collection}",
                (
                    TStore store,
                    DateTime? updatedFrom,
                    DateTime? updatedTo,
                    [Range(1, int.MaxValue, ErrorMessage = "page counts from 1.")] int page = 1,
                    [Range(1, MaxPageSize)] int pageSize = DefaultPageSize
                ) => List(store, page, pageSize, updatedFrom, updatedTo)
            )
            .WithSummary($"List {kind.Name}s, newest first")
            .WithDescription(
                "Pages through every certificate of this kind by update time, with the model each was "
                    + "built from. Unlike the SOAP find, it includes certificates the SOAP face refuses. "
                    + $"page counts from 1; pageSize defaults to {DefaultPageSize} and is at most "
                    + $"{MaxPageSize}. updatedFrom and updatedTo are optional, inclusive and read as UTC "
                    + "when they carry no offset."
            )
            .Produces<CertificatePage>()
            .ProducesValidationProblem();

        control
            .MapGet(
                $"/{collection}/{{id}}",
                async (string id, TStore store) =>
                    await store.FindAsync(id) is { } stored
                        ? Results.Ok(new StoredCertificateResponse(stored.Id, stored.Accessible, stored.Source))
                        : NotFound(kind, id)
            )
            .WithSummary($"Get the model a {kind.Name} was built from")
            .WithDescription(
                "Returns the control model as stored, after any PATCH merges, ready to edit and PUT back. "
                    + "It is not the certificate: fields the simulator fills in, such as display names "
                    + "and timestamps, are absent, and a status never set is absent too (it reads as NEW). "
                    + $"Read the certificate itself with {soapOperation}."
            )
            .Produces<StoredCertificateResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        control
            .MapPut(
                $"/{collection}/{{id}}",
                (
                    string id,
                    CertificateControlModel model,
                    TStore store,
                    SpsCertificateBuilder builder,
                    CommodityCatalogue catalogue
                ) => Update(kind, id, model, store, builder, catalogue)
            )
            .WithSummary($"Replace a {kind.Name}")
            .WithDescription("Rebuilds the certificate from the supplied model, keeping its ID.")
            .Produces<StoredCertificateResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        control
            .MapPatch(
                $"/{collection}/{{id}}",
                (
                    string id,
                    CertificateControlModel patch,
                    TStore store,
                    SpsCertificateBuilder builder,
                    CommodityCatalogue catalogue
                ) => Patch(kind, id, patch, store, builder, catalogue)
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

    private static void MapLookups(RouteGroupBuilder control)
    {
        control
            .MapGet("/lookups", (CodeLists codeLists) => Results.Ok(codeLists.ListIds.Order()))
            .WithSummary("List the seeded code lists")
            .WithDescription("The names to pass to /control/lookups/{list}, one per file under Control/Lookups/SeedData.")
            .Produces<IEnumerable<string>>();

        control
            .MapGet(
                "/lookups/{list}",
                (string list, CodeLists codeLists) =>
                    codeLists.TryGetList(list, out var entries)
                        ? Results.Ok(entries.Select(entry => new LookupEntry(entry.Key, entry.Value.Name, entry.Value.Alias)))
                        : Results.Problem($"No code list '{list}' is seeded.", statusCode: StatusCodes.Status404NotFound)
            )
            .WithSummary("Get a seeded code list")
            .WithDescription(
                "The codes the simulator accepts from one list, in file order. A code missing here is a 400 "
                    + "when a certificate uses it. Operators and authorities are lists too, by identifier."
            )
            .Produces<IEnumerable<LookupEntry>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> List(
        ICertificateStore store,
        int page,
        int pageSize,
        DateTime? updatedFrom,
        DateTime? updatedTo
    )
    {
        // One more than the page, so the response can say whether another follows without a count.
        var found = await store.SearchAsync(
            ChedSummary.Bound(updatedFrom),
            ChedSummary.Bound(updatedTo),
            (page - 1) * pageSize,
            pageSize + 1,
            includeInaccessible: true
        );

        return Results.Ok(
            new CertificatePage(
                page,
                pageSize,
                found.Count > pageSize,
                [
                    .. found
                        .Take(pageSize)
                        .Select(stored => new ListedCertificate(
                            stored.Id,
                            stored.Accessible,
                            ChedSummary.UpdatedAt(stored.Certificate),
                            stored.Source
                        )),
                ]
            )
        );
    }

    private static Task<IResult> Create(
        CertificateKind kind,
        string collection,
        CertificateControlModel model,
        ICertificateStore store,
        SpsCertificateBuilder builder,
        CommodityCatalogue catalogue
    ) =>
        Guarded(async () =>
        {
            // The prefix can come out of a note, so reading it can fail the same way any other
            // lookup fails — inside the guard, not before it.
            var id = await store.NextIdAsync(kind.IdPrefix(model));

            await store.PutAsync(await StoredAsync(kind, id, model, builder, catalogue));

            return Results.Created($"{Prefix}/{collection}/{id}", Describe(model, id));
        });

    private static async Task<IResult> Update(
        CertificateKind kind,
        string id,
        CertificateControlModel model,
        ICertificateStore store,
        SpsCertificateBuilder builder,
        CommodityCatalogue catalogue
    )
    {
        if (await store.FindAsync(id) is null)
        {
            return NotFound(kind, id);
        }

        return await Guarded(async () =>
        {
            await store.PutAsync(await StoredAsync(kind, id, model, builder, catalogue));
            return Results.Ok(Describe(model, id));
        });
    }

    private static async Task<IResult> Patch(
        CertificateKind kind,
        string id,
        CertificateControlModel patch,
        ICertificateStore store,
        SpsCertificateBuilder builder,
        CommodityCatalogue catalogue
    )
    {
        if (await store.FindAsync(id) is not { } stored)
        {
            return NotFound(kind, id);
        }

        var merged = stored.Source.Merge(patch);

        return await Guarded(async () =>
        {
            await store.PutAsync(await StoredAsync(kind, id, merged, builder, catalogue));
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

    /// <summary>A CHED's commodity tree is read here, before the synchronous builder runs, because it comes from the store.</summary>
    private static async Task<StoredCertificate> StoredAsync(
        CertificateKind kind,
        string id,
        CertificateControlModel model,
        SpsCertificateBuilder builder,
        CommodityCatalogue catalogue
    )
    {
        var commodities = kind == CertificateKind.Ched
            ? await catalogue.GetAsync(CertificateKind.ChedTypeOf(model))
            : null;

        return new(id, builder.Build(kind, model, id, commodities), model.Accessible ?? true, model);
    }

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

/// <summary>What the control API returns for a stored certificate: the model it was built from. The certificate itself is read back over SOAP.</summary>
public record StoredCertificateResponse(string Id, bool Accessible, CertificateControlModel Certificate);

/// <summary>One page of a listing. <paramref name="HasMore"/> says whether asking for the next page finds anything.</summary>
public record CertificatePage(int Page, int PageSize, bool HasMore, IReadOnlyList<ListedCertificate> Items);

/// <summary>A stored certificate as a listing shows it: its model, and when it last changed, in UTC.</summary>
public record ListedCertificate(string Id, bool Accessible, DateTime Updated, CertificateControlModel Certificate);

/// <summary>One code a list accepts. The alias, where there is one, is accepted in place of the code.</summary>
public record LookupEntry(string Code, string Name, string? Alias);

public record ResetResponse(int CertificateCount);
