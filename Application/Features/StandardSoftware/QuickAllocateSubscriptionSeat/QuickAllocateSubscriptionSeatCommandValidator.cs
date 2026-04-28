// Project comment: QuickAllocateSubscriptionSeatCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.QuickAllocateSubscriptionSeat
{
    /// <summary>
    /// Validates input for the QuickAllocateSubscriptionSeatCommand request.
    /// </summary>
    public sealed class QuickAllocateSubscriptionSeatCommandValidator : AbstractValidator<QuickAllocateSubscriptionSeatCommand>
    {
        public QuickAllocateSubscriptionSeatCommandValidator()
        {
            RuleFor(x => x.SoftwareId).GreaterThan(0);
        }
    }
}
