using System.Reflection;
using IT_Form.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IT_Form.Tests;

public sealed class EmployeeControllerAuthorizationTests
{
    [Fact]
    public void EmployeeDirectory_RequiresHrOrItPolicy()
    {
        var authorization = typeof(EmployeeController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorization);
        Assert.Equal("RequireHROrIT", authorization!.Policy);
    }
}
