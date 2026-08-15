using Autofac;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Simulation;
using Module = Autofac.Module;

namespace IoTSensorMonitoring.Application.DependencyResolvers;

public class AutofacApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(CompanyService).Assembly)
            .Where(type => type.Name.EndsWith("Service") && !type.IsGenericTypeDefinition)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.RegisterType<MeasurementGenerator>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
