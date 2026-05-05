// Project comment: CreateStandardSoftwareCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Services;
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
    private readonly IQuotaControl _quotaControl;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public CreateStandardSoftwareCommandHandler(ApplicationDbContext context, IQuotaControl? quotaControl = null, ICurrentUserAccessor? currentUserAccessor = null)
    {
        _context = context;
        _quotaControl = quotaControl ?? new QuotaControl(context);
        _currentUserAccessor = currentUserAccessor ?? new CurrentUserAccessor(new Microsoft.AspNetCore.Http.HttpContextAccessor());
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateStandardSoftwareCommand request, CancellationToken cancellationToken)
    {
        await _quotaControl.EnsureAvailableAsync(QuotaControl.Software, user: _currentUserAccessor.User, cancellationToken: cancellationToken);
        var entity = request.Adapt<StandardSoftwareEntity>();

        _context.StandardSoftware.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        if (!entity.HasMultipleVersions)
        {
            await _quotaControl.EnsureAvailableAsync(QuotaControl.SoftwareVersions, user: _currentUserAccessor.User, cancellationToken: cancellationToken);
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
