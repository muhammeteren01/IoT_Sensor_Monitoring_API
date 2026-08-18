using System.Reflection;
using IoTSensorMonitoring.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace IoTSensorMonitoring.Tests.Controllers.Auth;

public class AuthControllerAuthorizeAttributeTests
{
    [Fact]
    public void Login_HasAllowAnonymousAttribute()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Login));
        method.Should().NotBeNull();
        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Should().ContainSingle();
    }

    [Fact]
    public void Me_HasAuthorizeAttribute_WithEmptyRoles()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Me));
        method.Should().NotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().BeNullOrEmpty();
        method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Should().BeEmpty();
    }
}
