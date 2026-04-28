// Project comment: ICurrentUserAccessor.cs is part of the IT Form application source code.

using System.Security.Claims;

namespace IT_Form.Application.Common.Abstractions
{
    /// <summary>
    /// Defines the contract implemented by ICurrentUserAccessor services.
    /// </summary>
    public interface ICurrentUserAccessor
    {
        ClaimsPrincipal User { get; }
        string? UserName { get; }
    }
}
