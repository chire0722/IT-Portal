using System.Security.Claims;

namespace IT_Form.Application.Common.Abstractions
{
    public interface ICurrentUserAccessor
    {
        ClaimsPrincipal User { get; }
        string? UserName { get; }
    }
}
