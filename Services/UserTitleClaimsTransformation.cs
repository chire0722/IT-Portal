using System.Security.Claims;
using IT_Form.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace IT_Form.Services
{
    /// <summary>
    /// Adds UserTitle claims on every request so authorization policies work.
    /// </summary>
    public class UserTitleClaimsTransformation : IClaimsTransformation
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public UserTitleClaimsTransformation(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity?.IsAuthenticated != true)
                return principal;

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return principal;

            var cacheKey = $"claims-transform:{userId}";
            if (!_cache.TryGetValue(cacheKey, out ClaimsSnapshot? snapshot))
            {
                var titles = await _context.UserTitles
                    .AsNoTracking()
                    .Where(ut => ut.UserId == userId)
                    .Select(ut => ut.Title)
                    .ToListAsync();

                var companies = await _context.UserCompanies
                    .AsNoTracking()
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.CompanyName)
                    .ToListAsync();

                var user = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.MustChangePassword })
                    .FirstOrDefaultAsync();

                snapshot = new ClaimsSnapshot(
                    user?.MustChangePassword == true,
                    titles,
                    companies);

                _cache.Set(
                    cacheKey,
                    snapshot,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                    });
            }

            var identity = principal.Identity as ClaimsIdentity;
            if (identity != null)
            {
                if (snapshot!.MustChangePassword)
                {
                    if (!identity.HasClaim("MustChangePassword", "true"))
                    {
                        identity.AddClaim(new Claim("MustChangePassword", "true"));
                    }
                }
                else
                {
                    foreach (var claim in identity.FindAll("MustChangePassword").ToList())
                    {
                        identity.RemoveClaim(claim);
                    }
                }

                var existingTitles = identity.FindAll("UserTitle")
                    .Select(claim => claim.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var title in snapshot.Titles.Where(title => !existingTitles.Contains(title)))
                {
                    identity.AddClaim(new Claim("UserTitle", title));
                }

                var existingCompanies = identity.FindAll("UserCompany")
                    .Select(claim => claim.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var company in snapshot.Companies.Where(company => !existingCompanies.Contains(company)))
                {
                    identity.AddClaim(new Claim("UserCompany", company));
                }
            }

            return principal;
        }

        private sealed record ClaimsSnapshot(
            bool MustChangePassword,
            IReadOnlyCollection<string> Titles,
            IReadOnlyCollection<string> Companies);
    }
}
