namespace RoadRegistry.Hosts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using BackOffice;
using BackOffice.Extensions;
using BackOffice.Framework;
using Be.Vlaanderen.Basisregisters.Aws.Lambda;
using Be.Vlaanderen.Basisregisters.DataDog.Tracing.Autofac;
using Be.Vlaanderen.Basisregisters.EventHandling;
using Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore.Autofac;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Newtonsoft.Json;
using Environments = Be.Vlaanderen.Basisregisters.Aws.Lambda.Environments;

public abstract class RoadRegistryLambdaFunction : FunctionBase
{
    protected static readonly ApplicationMetadata ApplicationMetadata = new(RoadRegistryApplication.Lambda);
    protected readonly string ApplicationName;
    protected readonly JsonSerializerSettings EventSerializerSettings;

    protected RoadRegistryLambdaFunction(string applicationName, IReadOnlyCollection<Assembly> messageAssemblies) : base(messageAssemblies)
    {
        ApplicationName = applicationName;
        EventSerializerSettings = EventsJsonSerializerSettingsProvider.CreateSerializerSettings();
    }

    protected virtual IConfiguration BuildConfiguration(IHostEnvironment hostEnvironment)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .UseDefaultConfiguration(hostEnvironment);

        if (Debugger.IsAttached)
        {
            var dir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            configurationBuilder
                .SetBasePath(dir);
        }

        return configurationBuilder.Build();
    }

    protected virtual void ConfigureContainer(ContainerBuilder builder, IConfiguration configuration)
    {
        JsonConvert.DefaultSettings = () => EventSerializerSettings;

        builder
            .RegisterMediator()
            .RegisterRetryPolicy()
            .RegisterModule(new DataDogModule(configuration))
            .RegisterModule<EnvelopeModule>();
    }

    protected override IServiceProvider ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IHostEnvironment>(sp => new HostingEnvironment
        {
            ApplicationName = ApplicationName,
            EnvironmentName = Environments.Production
        });

        var tempProvider = services.BuildServiceProvider();
        var hostEnvironment = tempProvider.GetRequiredService<IHostEnvironment>();

        var configuration = BuildConfiguration(hostEnvironment);

        var eventSourcedEntityMap = new EventSourcedEntityMap();

        services
            .AddTicketing()
            .AddSingleton(ApplicationMetadata)
            .AddSingleton<Func<EventSourcedEntityMap>>(_ => () => eventSourcedEntityMap)
            .AddEditorContext()
            .AddStreamStore()
            .AddLogging(configure => { configure.AddRoadRegistryLambdaLogger(); })
            .AddSqsLambdaHandlerOptions()
            .AddFeatureToggles<ApplicationFeatureToggle>(configuration)
            ;

        var builder = new ContainerBuilder();
        builder.RegisterConfiguration(configuration);
        builder.Populate(services);

        ConfigureContainer(builder, configuration);

        return new AutofacServiceProvider(builder.Build());
    }
}