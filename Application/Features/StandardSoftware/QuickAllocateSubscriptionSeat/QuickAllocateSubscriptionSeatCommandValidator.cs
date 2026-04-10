using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.QuickAllocateSubscriptionSeat
{
    public sealed class QuickAllocateSubscriptionSeatCommandValidator : AbstractValidator<QuickAllocateSubscriptionSeatCommand>
    {
        public QuickAllocateSubscriptionSeatCommandValidator()
        {
            RuleFor(x => x.SoftwareId).GreaterThan(0);
        }
    }
}
