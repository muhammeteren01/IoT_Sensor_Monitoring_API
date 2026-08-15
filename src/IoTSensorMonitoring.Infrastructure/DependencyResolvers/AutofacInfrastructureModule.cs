using Autofac;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Infrastructure.Persistence.Repositories;
using Module = Autofac.Module;

namespace IoTSensorMonitoring.Infrastructure.DependencyResolvers;

public class AutofacInfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(Repository<>))
            .As(typeof(IRepository<>))
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(typeof(SensorRepository).Assembly)
            .Where(type => type.Name.EndsWith("Repository") && !type.IsGenericTypeDefinition)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.RegisterType<UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();
    }
}
