using FluentValidation;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public sealed class UpdateStopTimesCommandValidator : AbstractValidator<UpdateStopTimesCommand>
    {
        public UpdateStopTimesCommandValidator()
        {
            RuleFor(x => x.TripId).NotEmpty().WithMessage("TripId is required.");

            RuleFor(x => x.Stops)
                .NotEmpty()
                .WithMessage("At least one stop departure time is required.");

            RuleFor(x => x.Stops)
                .Must(stops => stops is null
                    || stops.Select(s => s.StopId).Distinct().Count() == stops.Count)
                .WithMessage("The same stop cannot be updated twice in the same request.");

            RuleForEach(x => x.Stops).ChildRules(stop =>
            {
                stop.RuleFor(s => s.StopId).NotEmpty().WithMessage("StopId is required.");
            });
        }
    }
}
