// Project comment: DeleteCompanyCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Company.Writes.DeleteCompany;

/// <summary>
/// Handles the DeleteCompanyCommand command workflow.
/// </summary>
public sealed class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public DeleteCompanyCommandHandler(
        ApplicationDbContext context,
        IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<Unit> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Companies
            .FirstOrDefaultAsync(company => company.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.Id);

        if (!string.IsNullOrEmpty(entity.LogoPath))
        {
            var fullPath = Path.Combine(_hostEnvironment.WebRootPath, entity.LogoPath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        _context.Companies.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
