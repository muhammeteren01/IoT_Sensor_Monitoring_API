using System.Security.Claims;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Tests.Controllers.Auth;

internal static class AuthTestHelper
{
    public static AuthResponse CreateAuthResponse(string email, UserRole role, Guid? companyId) =>
        new(
            "jwt-token",
            DateTime.UtcNow.AddHours(1),
            Guid.NewGuid(),
            companyId,
            email,
            "Ali",
            "Veli",
            role);

    public static void SetUser(ControllerBase controller, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
