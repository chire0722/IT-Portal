// Project comment: EditItAssetCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Data;
using IT_Form.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ITAsset.EditItAsset
{
    /// <summary>
    /// Validates input for the EditItAssetCommand request.
    /// </summary>
    public sealed class EditItAssetCommandValidator : AbstractValidator<EditItAssetCommand>
    {
        private readonly ApplicationDbContext _context;

        public EditItAssetCommandValidator(ApplicationDbContext context)
        {
            _context = context;

            RuleFor(x => x.Id)
                .GreaterThan(0);

            RuleFor(x => x.Asset)
                .NotNull();

            RuleFor(x => x)
                .CustomAsync(ValidateAsync);
        }

        /// <summary>
        /// Validates data for the ValidateAsync operation.
        /// </summary>
        private async Task ValidateAsync(
            EditItAssetCommand command,
            ValidationContext<EditItAssetCommand> context,
            CancellationToken cancellationToken)
        {
            if (command.Asset == null)
            {
                return;
            }

            if (await _context.ITAssets
                .AsNoTracking()
                .AnyAsync(asset => asset.AssetTag == command.Asset.AssetTag && asset.Id != command.Id, cancellationToken))
            {
                context.AddFailure("AssetTag", "This Asset Tag is already in use by another asset.");
            }

            if (command.Asset.Status == AssetStatus.Issued && !command.AssignedEmployeeId.HasValue)
            {
                context.AddFailure("CustomEmployeeName", "Please select a valid employee from the directory when status is Issued.");
            }
        }
    }
}
