using System.Diagnostics.CodeAnalysis;
using Api.TradeTracesNTStub.Example.Models;
using FluentValidation;

namespace Api.TradeTracesNTStub.Example.Validators;

[ExcludeFromCodeCoverage]
public class ExampleValidator : AbstractValidator<ExampleModel>
{
    /**
     * Example model validator.
     */
    public ExampleValidator()
    {
        RuleFor(model => model.Name)
            .Matches(@"^[\w\s]+$")
            .Length(3, 20)
            .WithMessage(
                "Name was not valid. Must be between 3 and 20 characters and contain only letters, numbers and whitespace.");

        RuleFor(model => model.Counter).GreaterThanOrEqualTo(0);
        RuleFor(model => model.Value).NotEmpty();
    }
}