using IoTSensorMonitoring.Application.Grafana;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Grafana;

public class GrafanaPostgresRoleTests
{
    [Fact]
    public void RoleName_IsStableAndSafeForPostgres()
    {
        var companyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var roleName = GrafanaPostgresRole.RoleName(companyId);

        roleName.Should().Be("g_c_aaaaaaaabbbbccccddddeeeeeeeeeeee");
        GrafanaPostgresRole.IsManagedRoleName(roleName).Should().BeTrue();
    }

    [Fact]
    public void Password_IsDeterministicForSameSecret()
    {
        var companyId = Guid.NewGuid();

        var first = GrafanaPostgresRole.Password(companyId, "secret-secret-secret-secret-32ch");
        var second = GrafanaPostgresRole.Password(companyId, "secret-secret-secret-secret-32ch");

        first.Should().Be(second);
        first.Should().HaveLength(64);
        first.Should().MatchRegex("^[0-9a-f]+$");
    }
}
