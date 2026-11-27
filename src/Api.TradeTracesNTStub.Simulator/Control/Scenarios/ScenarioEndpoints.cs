using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.TradeTracesNTStub.Simulator.Control.Scenarios;

/// <summary>
/// Named control models a UI starts a new CHED from. The stub never reads them itself: it stores them
/// so every UI, and every instance on CDP, offers the same set.
/// </summary>
public static class ScenarioEndpoints
{
    public static void MapScenarios(this RouteGroupBuilder control)
    {
        var scenarios = control.MapGroup("/scenarios");

        scenarios
            .MapGet("/", List)
            .WithSummary("List the scenarios, models included, by name")
            .Produces<IReadOnlyList<Scenario>>();

        scenarios
            .MapGet("/{id}", Get)
            .WithSummary("Get a scenario")
            .Produces<Scenario>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        scenarios
            .MapPut("/{id}", Put)
            .WithSummary("Save a scenario")
            .WithDescription("Replaces any scenario with the same ID.")
            .Produces<Scenario>()
            .ProducesValidationProblem();

        scenarios
            .MapDelete("/{id}", Delete)
            .WithSummary("Delete a scenario")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> List(IScenarioStore store) => Results.Ok(await store.ListAsync());

    private static async Task<IResult> Get(string id, IScenarioStore store) =>
        await store.GetAsync(id) is { } scenario ? Results.Ok(scenario) : NotFound(id);

    private static async Task<IResult> Put(
        [RegularExpression(
            "^[a-z0-9]+(-[a-z0-9]+)*$",
            ErrorMessage = "Use lower-case letters and digits separated by hyphens, e.g. ched-a-live-horses."
        )]
            string id,
        ScenarioRequest request,
        IScenarioStore store
    )
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Model.ValueKind != JsonValueKind.Object)
        {
            return Results.Problem(
                "A scenario needs a name and a model: the control model a CHED would be created from.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var scenario = new Scenario(id, request.Name.Trim(), request.Model);
        await store.PutAsync(scenario);
        return Results.Ok(scenario);
    }

    private static async Task<IResult> Delete(string id, IScenarioStore store) =>
        await store.RemoveAsync(id) ? Results.NoContent() : NotFound(id);

    private static IResult NotFound(string id) =>
        Results.Problem($"No scenario '{id}' exists in the simulator.", statusCode: StatusCodes.Status404NotFound);
}
