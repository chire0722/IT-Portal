// Project comment: CompanyScopeHelper.cs is part of the IT Form application source code.

using System.Security.Claims;
using System.Linq.Expressions;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Security;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Helpers;

/// <summary>
/// Defines the CompanyScopeHelper class used by the IT Form application.
/// </summary>
public static class CompanyScopeHelper
{
    private const string NoCompanyAccessSentinel = "__NO_COMPANY_ACCESS__";

    /// <summary>
    /// Gets the data required by the GetScopedCompanies operation.
    /// </summary>
    public static List<string> GetScopedCompanies(ClaimsPrincipal? user)
    {
        // Super Admin is always global, even if an older identity still carries a company claim.
        if (PortalTitles.IsSuperAdmin(user))
        {
            return [];
        }

        var companies = UserHelper.GetClaimValues(user, "UserCompany")
            .Where(company => !string.IsNullOrWhiteSpace(company))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (companies.Count > 0)
        {
            return companies;
        }

        // Unscoped IT remains a global IT identity; scoped IT is constrained by its company claims.
        return RequiresExplicitCompanyScope(user)
            ? [NoCompanyAccessSentinel]
            : [];
    }

    /// <summary>
    /// Runs the HasScopedCompanies operation.
    /// </summary>
    public static bool HasScopedCompanies(ClaimsPrincipal? user)
        => GetScopedCompanies(user).Count > 0;

    /// <summary>
    /// Runs the CanAccessCompany operation.
    /// </summary>
    public static bool CanAccessCompany(ClaimsPrincipal? user, string? company)
        => CanAccessCompany(GetScopedCompanies(user), company);

    /// <summary>
    /// Runs the CanAccessCompany operation.
    /// </summary>
    public static bool CanAccessCompany(IEnumerable<string> scopedCompanies, string? company)
    {
        var normalizedScope = NormalizeCompanies(scopedCompanies);
        if (normalizedScope.Count == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(company))
        {
            return false;
        }

        return normalizedScope.Any(scopedCompany => CompanyHelper.IsMatch(scopedCompany, company));
    }

    /// <summary>
    /// Runs the CanAccessAnyCompany operation.
    /// </summary>
    public static bool CanAccessAnyCompany(ClaimsPrincipal? user, params string?[] companies)
        => CanAccessAnyCompany(GetScopedCompanies(user), companies);

    /// <summary>
    /// Runs the CanAccessAnyCompany operation.
    /// </summary>
    public static bool CanAccessAnyCompany(IEnumerable<string> scopedCompanies, params string?[] companies)
    {
        var normalizedScope = NormalizeCompanies(scopedCompanies);
        if (normalizedScope.Count == 0)
        {
            return true;
        }

        return companies.Any(company => CanAccessCompany(normalizedScope, company));
    }

    /// <summary>
    /// Runs the HasAnyAllowedCompany operation.
    /// </summary>
    public static bool HasAnyAllowedCompany(IEnumerable<string> scopedCompanies, IEnumerable<string> targetCompanies)
    {
        var normalizedScope = NormalizeCompanies(scopedCompanies);
        if (normalizedScope.Count == 0)
        {
            return true;
        }

        var targets = NormalizeCompanies(targetCompanies);
        return targets.Any(targetCompany => CanAccessCompany(normalizedScope, targetCompany));
    }

    /// <summary>
    /// Runs the AreAllCompaniesAllowed operation.
    /// </summary>
    public static bool AreAllCompaniesAllowed(IEnumerable<string> scopedCompanies, IEnumerable<string> targetCompanies)
    {
        var normalizedScope = NormalizeCompanies(scopedCompanies);
        if (normalizedScope.Count == 0)
        {
            return true;
        }

        var targets = NormalizeCompanies(targetCompanies);
        return targets.Count > 0 &&
               targets.All(targetCompany => CanAccessCompany(normalizedScope, targetCompany));
    }

    /// <summary>
    /// Runs the LoadAccessibleCompanyDisplayNamesAsync operation.
    /// </summary>
    public static async Task<List<string>> LoadAccessibleCompanyDisplayNamesAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        var scopedCompanies = GetScopedCompanies(user);
        var companies = await context.Companies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .OrderBy(company => company.Name)
            .Select(company => string.IsNullOrWhiteSpace(company.Lot)
                ? company.Name
                : company.Name + " (" + company.Lot + ")")
            .ToListAsync(cancellationToken);

        if (scopedCompanies.Count == 0)
        {
            return companies;
        }

        return companies
            .Where(company => CanAccessCompany(scopedCompanies, company))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Runs the LoadAccessibleCompanyIdsAsync operation.
    /// </summary>
    public static async Task<HashSet<int>> LoadAccessibleCompanyIdsAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken,
        bool activeOnly = false)
    {
        var scopedCompanies = GetScopedCompanies(user);
        var query = context.Companies.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(company => company.IsActive);
        }

        var companies = await query
            .Select(company => new
            {
                company.Id,
                DisplayName = string.IsNullOrWhiteSpace(company.Lot)
                    ? company.Name
                    : company.Name + " (" + company.Lot + ")"
            })
            .ToListAsync(cancellationToken);

        if (scopedCompanies.Count == 0)
        {
            return companies.Select(company => company.Id).ToHashSet();
        }

        return companies
            .Where(company => CanAccessCompany(scopedCompanies, company.DisplayName))
            .Select(company => company.Id)
            .ToHashSet();
    }

    /// <summary>
    /// Runs the AreRequestedCompanyIdsAssignableAsync operation.
    /// </summary>
    public static async Task<bool> AreRequestedCompanyIdsAssignableAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        IEnumerable<int> requestedCompanyIds,
        CancellationToken cancellationToken)
    {
        var requestedIds = requestedCompanyIds
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            return false;
        }

        var accessibleActiveCompanyIds = await LoadAccessibleCompanyIdsAsync(
            context,
            user,
            cancellationToken,
            activeOnly: true);

        return requestedIds.All(accessibleActiveCompanyIds.Contains);
    }

    /// <summary>
    /// Applies changes for the ApplyOnboardingCompanyScope operation.
    /// </summary>
    public static IQueryable<OnboardingRequest> ApplyOnboardingCompanyScope(
        IQueryable<OnboardingRequest> query,
        IEnumerable<string> scopedCompanies)
        => ApplyCompanyScope(query, scopedCompanies, request => request.Branch);

    public static IQueryable<T> ApplyCompanyScope<T>(
        IQueryable<T> query,
        IEnumerable<string> scopedCompanies,
        params Expression<Func<T, string?>>[] companySelectors)
    {
        var matchers = BuildQueryMatchers(scopedCompanies);
        if (matchers.Count == 0 || companySelectors.Length == 0)
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        Expression? combinedBody = null;

        foreach (var selector in companySelectors)
        {
            var selectorBody = ReplaceParameter(selector.Body, selector.Parameters[0], parameter);
            var selectorMatchBody = BuildCompanyMatchBody(selectorBody, matchers);
            combinedBody = combinedBody == null
                ? selectorMatchBody
                : Expression.OrElse(combinedBody, selectorMatchBody);
        }

        return query.Where(Expression.Lambda<Func<T, bool>>(combinedBody!, parameter));
    }

    /// <summary>
    /// Runs the NormalizeCompanies operation.
    /// </summary>
    private static List<string> NormalizeCompanies(IEnumerable<string> companies)
        => companies
            .Where(company => !string.IsNullOrWhiteSpace(company))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Runs the RequiresExplicitCompanyScope operation.
    /// </summary>
    private static bool RequiresExplicitCompanyScope(ClaimsPrincipal? user)
        => user?.Identity?.IsAuthenticated == true &&
           !PortalTitles.HasGlobalCompanyAccess(user) &&
           user.HasClaim(claim =>
               claim.Type == "UserTitle" &&
               !string.IsNullOrWhiteSpace(claim.Value));

    /// <summary>
    /// Builds the value returned by the BuildQueryMatchers operation.
    /// </summary>
    private static List<CompanyScopeQueryMatcher> BuildQueryMatchers(IEnumerable<string> scopedCompanies)
        => NormalizeCompanies(scopedCompanies)
            .Select(company =>
            {
                var normalized = CompanyHelper.Normalize(company);
                var hasLot = company.Contains(" (", StringComparison.Ordinal);
                return new CompanyScopeQueryMatcher(company, normalized, AllowPrefixMatch: !hasLot);
            })
            .ToList();

    /// <summary>
    /// Builds the value returned by the BuildCompanyMatchBody operation.
    /// </summary>
    private static Expression BuildCompanyMatchBody(
        Expression companyExpression,
        IReadOnlyList<CompanyScopeQueryMatcher> matchers)
    {
        var notNull = Expression.NotEqual(companyExpression, Expression.Constant(null, typeof(string)));
        Expression? combinedMatch = null;

        foreach (var matcher in matchers)
        {
            Expression matcherExpression = Expression.Equal(
                companyExpression,
                Expression.Constant(matcher.ExactCompany, typeof(string)));

            if (!string.Equals(matcher.NormalizedCompany, matcher.ExactCompany, StringComparison.Ordinal))
            {
                matcherExpression = Expression.OrElse(
                    matcherExpression,
                    Expression.Equal(
                        companyExpression,
                        Expression.Constant(matcher.NormalizedCompany, typeof(string))));
            }

            if (matcher.AllowPrefixMatch)
            {
                matcherExpression = Expression.OrElse(
                    matcherExpression,
                    Expression.Call(
                        companyExpression,
                        nameof(string.StartsWith),
                        Type.EmptyTypes,
                        Expression.Constant(matcher.NormalizedCompany + " (", typeof(string))));
            }

            combinedMatch = combinedMatch == null
                ? matcherExpression
                : Expression.OrElse(combinedMatch, matcherExpression);
        }

        return Expression.AndAlso(notNull, combinedMatch ?? Expression.Constant(false));
    }

    /// <summary>
    /// Runs the ReplaceParameter operation.
    /// </summary>
    private static Expression ReplaceParameter(Expression expression, ParameterExpression source, Expression target)
        => new ParameterReplaceVisitor(source, target).Visit(expression)!;

    /// <summary>
    /// Defines the CompanyScopeQueryMatcher record used by the IT Form application.
    /// </summary>
    private sealed record CompanyScopeQueryMatcher(
        string ExactCompany,
        string NormalizedCompany,
        bool AllowPrefixMatch);

    /// <summary>
    /// Defines the ParameterReplaceVisitor class used by the IT Form application.
    /// </summary>
    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly Expression _target;

        public ParameterReplaceVisitor(ParameterExpression source, Expression target)
        {
            _source = source;
            _target = target;
        }

        /// <summary>
        /// Runs the VisitParameter operation.
        /// </summary>
        protected override Expression VisitParameter(ParameterExpression node)
            => node == _source ? _target : base.VisitParameter(node);
    }
}
