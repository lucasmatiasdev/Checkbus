using Checkbus.ApiService.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Checkbus.Tests.Common;

public class ValidationBehaviorTests
{
    // Deliberately NOT LoginCommand — proves the behavior is generic.
    public sealed class DummyRequest : IRequest<string>
    {
        public required string Value { get; set; }
    }

    private sealed class FakeValidator(bool isValid) : IValidator<DummyRequest>
    {
        private ValidationResult Result()
            => isValid
                ? new ValidationResult()
                : new ValidationResult([new ValidationFailure("Value", "Value is invalid.")]);

        public ValidationResult Validate(DummyRequest instance) => Result();

        public Task<ValidationResult> ValidateAsync(DummyRequest instance, CancellationToken cancellation = default)
            => Task.FromResult(Result());

        public ValidationResult Validate(IValidationContext context) => Result();

        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default)
            => Task.FromResult(Result());

        public IValidatorDescriptor CreateDescriptor() => throw new NotImplementedException();
        public bool CanValidateInstancesOfType(Type type) => type == typeof(DummyRequest);
    }

    [Fact]
    public async Task Handle_NoValidators_InvokesNext()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>([]);
        var request = new DummyRequest { Value = "anything" };
        var nextCalled = false;
        Task<string> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }

        var result = await behavior.Handle(request, Next, TestContext.Current.CancellationToken);

        Assert.True(nextCalled);
        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_FailingValidator_ThrowsAndSkipsNext()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>([new FakeValidator(isValid: false)]);
        var request = new DummyRequest { Value = "bad" };
        var nextCalled = false;
        Task<string> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(request, Next, TestContext.Current.CancellationToken));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_PassingValidator_InvokesNext()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>([new FakeValidator(isValid: true)]);
        var request = new DummyRequest { Value = "good" };
        var nextCalled = false;
        Task<string> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }

        var result = await behavior.Handle(request, Next, TestContext.Current.CancellationToken);

        Assert.True(nextCalled);
        Assert.Equal("handled", result);
    }
}
