using IT_Form.Application.Common.Models;
using IT_Form.Data;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailContact;

public sealed record GetEmailContactIndexPageQuery() : IRequest<ProcessResult<List<EmailContactIndexListItem>>>;

public sealed record GetEmailContactCreatePageQuery(EmailContactEditorPageViewModel? Model = null)
    : IRequest<ProcessResult<EmailContactEditorPageViewModel>>;

public sealed record GetEmailContactEditPageQuery(int Id, EmailContactEditorPageViewModel? Model = null)
    : IRequest<ProcessResult<EmailContactEditorPageViewModel>>;

public sealed record SearchEmailContactsQuery(string? Term)
    : IRequest<ProcessResult<List<EmailContactSearchItem>>>;

public sealed class GetEmailContactIndexPageQueryHandler
    : IRequestHandler<GetEmailContactIndexPageQuery, ProcessResult<List<EmailContactIndexListItem>>>
{
    private readonly ApplicationDbContext _context;

    public GetEmailContactIndexPageQueryHandler(ApplicationDbContext context) => _context = context;

    public async Task<ProcessResult<List<EmailContactIndexListItem>>> Handle(GetEmailContactIndexPageQuery request, CancellationToken cancellationToken)
    {
        var contacts = await _context.EmailContacts
            .AsNoTracking()
            .OrderBy(contact => contact.Name)
            .ProjectToType<EmailContactIndexListItem>()
            .ToListAsync(cancellationToken);

        return ProcessResult<List<EmailContactIndexListItem>>.Success(contacts);
    }
}

public sealed class GetEmailContactCreatePageQueryHandler
    : IRequestHandler<GetEmailContactCreatePageQuery, ProcessResult<EmailContactEditorPageViewModel>>
{
    public Task<ProcessResult<EmailContactEditorPageViewModel>> Handle(GetEmailContactCreatePageQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ProcessResult<EmailContactEditorPageViewModel>.Success(
            request.Model ?? new EmailContactEditorPageViewModel()));
    }
}

public sealed class GetEmailContactEditPageQueryHandler
    : IRequestHandler<GetEmailContactEditPageQuery, ProcessResult<EmailContactEditorPageViewModel>>
{
    private readonly ApplicationDbContext _context;

    public GetEmailContactEditPageQueryHandler(ApplicationDbContext context) => _context = context;

    public async Task<ProcessResult<EmailContactEditorPageViewModel>> Handle(GetEmailContactEditPageQuery request, CancellationToken cancellationToken)
    {
        var model = request.Model ?? await _context.EmailContacts
            .AsNoTracking()
            .Where(contact => contact.Id == request.Id)
            .ProjectToType<EmailContactEditorPageViewModel>()
            .FirstOrDefaultAsync(cancellationToken);

        if (model == null)
        {
            return ProcessResult<EmailContactEditorPageViewModel>.NotFound();
        }

        return ProcessResult<EmailContactEditorPageViewModel>.Success(model);
    }
}

public sealed class SearchEmailContactsQueryHandler
    : IRequestHandler<SearchEmailContactsQuery, ProcessResult<List<EmailContactSearchItem>>>
{
    private readonly ApplicationDbContext _context;

    public SearchEmailContactsQueryHandler(ApplicationDbContext context) => _context = context;

    public async Task<ProcessResult<List<EmailContactSearchItem>>> Handle(SearchEmailContactsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Term))
        {
            return ProcessResult<List<EmailContactSearchItem>>.Success([]);
        }

        var lowerTerm = request.Term.ToLower();

        var contacts = await _context.EmailContacts
            .AsNoTracking()
            .Where(contact =>
                contact.Name.ToLower().Contains(lowerTerm) ||
                contact.EmailAddress.ToLower().Contains(lowerTerm))
            .OrderBy(contact => contact.Name)
            .Select(contact => new EmailContactSearchItem
            {
                Id = contact.EmailAddress ?? string.Empty,
                Text = (contact.Name ?? string.Empty) + " <" + (contact.EmailAddress ?? string.Empty) + ">",
                Name = contact.Name ?? string.Empty,
                Email = contact.EmailAddress ?? string.Empty,
                Department = contact.Department
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        return ProcessResult<List<EmailContactSearchItem>>.Success(contacts);
    }
}
