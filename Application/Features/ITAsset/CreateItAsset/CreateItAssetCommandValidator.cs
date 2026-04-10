using FluentValidation;
using IT_Form.Data;
using IT_Form.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ITAsset.CreateItAsset
{
    public sealed class CreateItAssetCommandValidator : AbstractValidator<CreateItAssetCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateItAssetCommandValidator(ApplicationDbContext context)
        {
            _context = context;

            RuleFor(x => x.Asset)
                .NotNull();

            RuleFor(x => x)
                .CustomAsync(ValidateAsync);
        }

        private async Task ValidateAsync(
            CreateItAssetCommand command,
            ValidationContext<CreateItAssetCommand> context,
            CancellationToken cancellationToken)
        {
            if (command.Asset == null)
            {
                return;
            }

            if (await _context.ITAssets
                .AsNoTracking()
                .AnyAsync(asset => asset.AssetTag == command.Asset.AssetTag, cancellationToken))
            {
                context.AddFailure("AssetTag", "This Asset Tag is already in use.");
            }

            if (command.Asset.Status == AssetStatus.Issued && !command.AssignedEmployeeId.HasValue)
            {
                context.AddFailure("CustomEmployeeName", "Please select a valid employee from the directory when status is Issued.");
            }
        }
    }
}
