using System.Reflection;
using IoTSensorMonitoring.Api.Controllers;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace IoTSensorMonitoring.Tests.Controllers;

public class ControllerAuthorizeAttributeTests
{
    [Fact]
    public void CompaniesController_ClassRequiresAuthentication()
    {
        typeof(CompaniesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true).Should().NotBeNull();
    }

    [Theory]
    [InlineData(nameof(CompaniesController.GetAll), AppRoles.All)]
    [InlineData(nameof(CompaniesController.GetById), AppRoles.All)]
    [InlineData(nameof(CompaniesController.GetFacilities), AppRoles.All)]
    [InlineData(nameof(CompaniesController.GetUsers), AppRoles.CompanyAdmins)]
    [InlineData(nameof(CompaniesController.Create), AppRoles.SuperAdminOnly)]
    [InlineData(nameof(CompaniesController.Update), AppRoles.SuperAdminOnly)]
    [InlineData(nameof(CompaniesController.Delete), AppRoles.SuperAdminOnly)]
    public void CompaniesController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(CompaniesController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(FacilitiesController.GetById), AppRoles.All)]
    [InlineData(nameof(FacilitiesController.GetZones), AppRoles.All)]
    [InlineData(nameof(FacilitiesController.Create), AppRoles.Writers)]
    [InlineData(nameof(FacilitiesController.Update), AppRoles.Writers)]
    [InlineData(nameof(FacilitiesController.Delete), AppRoles.Writers)]
    public void FacilitiesController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(FacilitiesController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(ZonesController.GetById), AppRoles.All)]
    [InlineData(nameof(ZonesController.GetSensors), AppRoles.All)]
    [InlineData(nameof(ZonesController.Create), AppRoles.Writers)]
    [InlineData(nameof(ZonesController.Update), AppRoles.Writers)]
    [InlineData(nameof(ZonesController.Delete), AppRoles.Writers)]
    public void ZonesController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(ZonesController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(DeviceModelsController.GetAll), AppRoles.All)]
    [InlineData(nameof(DeviceModelsController.GetById), AppRoles.All)]
    [InlineData(nameof(DeviceModelsController.Create), AppRoles.SuperAdminOnly)]
    [InlineData(nameof(DeviceModelsController.Update), AppRoles.SuperAdminOnly)]
    [InlineData(nameof(DeviceModelsController.Delete), AppRoles.SuperAdminOnly)]
    public void DeviceModelsController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(DeviceModelsController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(SensorsController.GetAll), AppRoles.All)]
    [InlineData(nameof(SensorsController.GetById), AppRoles.All)]
    [InlineData(nameof(SensorsController.Create), AppRoles.Writers)]
    [InlineData(nameof(SensorsController.Update), AppRoles.Writers)]
    [InlineData(nameof(SensorsController.SetStatus), AppRoles.Writers)]
    [InlineData(nameof(SensorsController.Delete), AppRoles.Writers)]
    public void SensorsController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(SensorsController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(SensorMeasurementsController.GetAll), AppRoles.All)]
    [InlineData(nameof(SensorMeasurementsController.GetById), AppRoles.All)]
    [InlineData(nameof(SensorMeasurementsController.Create), AppRoles.All)]
    public void SensorMeasurementsController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(SensorMeasurementsController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(AlertRulesController.GetById), AppRoles.All)]
    [InlineData(nameof(AlertRulesController.Create), AppRoles.Writers)]
    [InlineData(nameof(AlertRulesController.Update), AppRoles.Writers)]
    [InlineData(nameof(AlertRulesController.Delete), AppRoles.Writers)]
    public void AlertRulesController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(AlertRulesController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(AlertHistoryController.List), AppRoles.All)]
    [InlineData(nameof(AlertHistoryController.Resolve), AppRoles.All)]
    public void AlertHistoryController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(AlertHistoryController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(MaintenanceLogsController.GetBySensorId), AppRoles.All)]
    [InlineData(nameof(MaintenanceLogsController.Create), AppRoles.All)]
    public void MaintenanceLogsController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(MaintenanceLogsController), methodName, expectedRoles);
    }

    [Theory]
    [InlineData(nameof(UsersController.GetAll), AppRoles.CompanyAdmins)]
    public void UsersController_ActionRoles(string methodName, string expectedRoles)
    {
        AssertMethodRoles(typeof(UsersController), methodName, expectedRoles);
    }

    [Fact]
    public void Writers_DoesNotIncludeOperator()
    {
        var allowed = AppRoles.Writers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.SuperAdmin);
        allowed.Should().Contain(AppRoles.CompanyAdmin);
        allowed.Should().NotContain(UserRole.Operator.ToString());
    }

    [Fact]
    public void ProtectedControllers_HaveNoAllowAnonymous()
    {
        Type[] controllers =
        [
            typeof(CompaniesController),
            typeof(FacilitiesController),
            typeof(ZonesController),
            typeof(DeviceModelsController),
            typeof(SensorsController),
            typeof(SensorMeasurementsController),
            typeof(AlertRulesController),
            typeof(AlertHistoryController),
            typeof(MaintenanceLogsController),
            typeof(UsersController)
        ];

        foreach (var controller in controllers)
        {
            controller.GetCustomAttribute<AuthorizeAttribute>(inherit: true).Should().NotBeNull(controller.Name);
            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
                    .Should().BeEmpty($"{controller.Name}.{method.Name}");
            }
        }
    }

    private static void AssertMethodRoles(Type controller, string methodName, string expectedRoles)
    {
        var method = controller.GetMethod(methodName);
        method.Should().NotBeNull($"{controller.Name}.{methodName}");

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(expectedRoles);
    }
}
