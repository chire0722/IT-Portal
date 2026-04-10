using IT_Form.Application.Common.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace IT_Form.Services
{
    public sealed class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

        public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User ?? Anonymous;

        public string? UserName => User.Identity?.Name;
    }
}
