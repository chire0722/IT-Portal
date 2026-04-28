// Project comment: CreateStandardSoftwareCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using Mapster;
using MediatR;
using StandardSoftwareEntity = IT_Form.Models.StandardSoftware;

namespace IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware;

/// <summary>
/// Handles the CreateStandardSoftwareCommand command workflow.
/// </summary>
public sealed class CreateStandardSoftwareCommandHandler : IRequestHandler<CreateStandardSoftwareCommand, int>
{
    private readonly ApplicationDbContext _context;

    public CreateStandardSoftwareCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateStandardSoftwareCommand request, CancellationToken cancellationToken)
    {
        var entity = request.Adapt<StandardSoftwareEntity>();

        _context.StandardSoftware.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        if (!entity.HasMultipleVersions)
        {
            var license = new IT_Form.Models.SoftwareLicense
            {
                SoftwareName = entity.SoftwareName,
                StandardSoftwareId = entity.Id,
                IsSubscription = false,
                IsFree = entity.IsFree,
                Description = "Default Edition"
            };
            _context.SoftwareLicenses.Add(license);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return entity.Id;
    }
}
