using BusBooking.Application.Common.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.UnitTests.Common;

public class ValidationBehaviorTests
{
    public sealed record SampleRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Handle_WithFailingValidator_ThrowsValidationException()
    {
        var validator = new Mock<IValidator<SampleRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required.") }));

        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { validator.Object });

        Func<Task> act = () => behavior.Handle(
            new SampleRequest(""),
            () => Task.FromResult("handled"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());

        var result = await behavior.Handle(
            new SampleRequest("Colombo"),
            () => Task.FromResult("handled"),
            CancellationToken.None);

        result.Should().Be("handled");
    }
}
