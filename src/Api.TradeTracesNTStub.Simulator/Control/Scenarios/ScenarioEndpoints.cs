using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.TradeTracesNTStub.Simulator.Control.Scenarios;

/// <summary>
/// Named control models a UI starts a new CHED from. The stub never reads them itself: it stores them
/// so every UI, and every instance on CDP, offers the same set.
/// </summary>
public static partial class ScenarioEndpoints
{
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex IdPattern();

    public static void MapScenarios(this RouteGroupBuilder control)
    {
        var scenarios = control.MapGroup("/scenarios");

        scenarios
            .MapGet("/", async (IScenarioStore store) => Results.Ok(await store.ListAsync()))
            .WithSummary("List the scenarios, models included, by name")
            .Produces<IReadOnlyList<Scenario>>();

        scenarios
            .MapGet(
                "/{id}",
                async (string id, IScenarioStore store) =>
                    await store.GetAsync(id) is { } scenario ? Results.Ok(scenario) : NotFound(id)
            )
            .WithSummary("Get a scenario")
            .Produces<Scenario>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        scenarios
            .MapPut(
                "/{id}",
                async (string id, ScenarioRequest request, IScenarioStore store) =>
                {
                    if (!IdPattern().IsMatch(id))
                    {
                        return Results.Problem(
                            $"'{id}' is not a scenario ID. Use lower-case letters and digits separated by hyphens, e.g. ched-a-live-horses.",
                            statusCode: StatusCodes.Status400BadRequest
                        );
                    }

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
            )
            .WithSummary("Save a scenario")
            .WithDescription("Replaces any scenario with the same ID, a committed one included, until it is deleted.")
            .Produces<Scenario>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        scenarios
            .MapDelete(
                "/{id}",
                async (string id, IScenarioStore store) => await store.RemoveAsync(id) ? Results.NoContent() : NotFound(id)
            )
            .WithSummary("Delete a scenario")
            .WithDescription("A committed scenario comes back when the stub next starts.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static IResult NotFound(string id) =>
        Results.Problem($"No scenario '{id}' exists in the simulator.", statusCode: StatusCodes.Status404NotFound);
}
