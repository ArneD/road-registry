namespace RoadRegistry.Projector.Infrastructure;

using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Be.Vlaanderen.Basisregisters.Api;
using Be.Vlaanderen.Basisregisters.DataDog.Tracing.Autofac;
using Configuration;
using Editor.Schema;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Modules;
using Options;
using Product.Schema;
using Syndication.Schema;
using Wfs.Schema;
using Wms.Schema;

/// <summary>Represents the startup process for the application.</summary>
public class Startup
{
    private const string DatabaseTag = "db";
    private readonly IConfiguration _configuration;
    private IContainer _applicationContainer;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(
        IServiceProvider serviceProvider,
        IApplicationBuilder app,
        IWebHostEnvironment env,
        IHostApplicationLifetime appLifetime,
        ILoggerFactory loggerFactory,
        IApiVersionDescriptionProvider apiVersionProvider,
        ApiDataDogToggle datadogToggle,
        ApiDebugDataDogToggle debugDataDogToggle,
        HealthCheckService healthCheckService)
    {
        StartupHelpers.CheckDatabases(healthCheckService, DatabaseTag, loggerFactory).GetAwaiter().GetResult();

        app
            .UseDataDog<Startup>(new DataDogOptions
            {
                Common =
                {
                    ServiceProvider = serviceProvider,
                    LoggerFactory = loggerFactory
                },
                Toggles =
                {
                    Enable = new Be.Vlaanderen.Basisregisters.DataDog.Tracing.Microsoft.ApiDataDogToggle(datadogToggle.FeatureEnabled),
                    Debug = new Be.Vlaanderen.Basisregisters.DataDog.Tracing.Microsoft.ApiDebugDataDogToggle(debugDataDogToggle.FeatureEnabled)
                },
                Tracing =
                {
                    ServiceName = _configuration["DataDog:ServiceName"]
                }
            })
            .UseDefaultForApi(new StartupUseOptions
            {
                Common =
                {
                    ApplicationContainer = _applicationContainer,
                    ServiceProvider = serviceProvider,
                    HostingEnvironment = env,
                    ApplicationLifetime = appLifetime,
                    LoggerFactory = loggerFactory
                },
                Api =
                {
                    VersionProvider = apiVersionProvider,
                    Info = groupName =>
                        $"Basisregisters.Vlaanderen - Road Registry API {groupName}",
                    CSharpClientOptions =
                    {
                        ClassName = "RoadRegistryProjector",
                        Namespace = "Be.Vlaanderen.Basisregisters"
                    },
                    TypeScriptClientOptions =
                    {
                        ClassName = "RoadRegistryProjector"
                    }
                },
                Server =
                {
                    PoweredByName = "Vlaamse overheid - Basisregisters Vlaanderen",
                    ServerName = "Digitaal Vlaanderen"
                },
                MiddlewareHooks =
                {
                    AfterMiddleware = x => x.UseMiddleware<AddNoCacheHeadersMiddleware>()
                }
            });
    }

    /// <summary>Configures services for the application.</summary>
    /// <param name="services">The collection of services to configure the application with.</param>
    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        var baseUrl = _configuration.GetValue<string>("BaseUrl");
        var baseUrlForExceptions = baseUrl.EndsWith("/")
            ? baseUrl.Substring(0, baseUrl.Length - 1)
            : baseUrl;

        services
            .ConfigureDefaultForApi<Startup>(new StartupConfigureOptions
            {
                Cors =
                {
                    Origins = _configuration
                        .GetSection("Cors")
                        .GetChildren()
                        .Select(c => c.Value)
                        .ToArray()
                },
                Server =
                {
                    BaseUrl = baseUrlForExceptions
                },
                Swagger =
                {
                    ApiInfo = (provider, description) => new OpenApiInfo
                    {
                        Version = description.ApiVersion.ToString(),
                        Title = "Basisregisters Vlaanderen Road Registry API",
                        Description = GetApiLeadingText(description),
                        Contact = new OpenApiContact
                        {
                            Name = "Digitaal Vlaanderen",
                            Email = "digitaal.vlaanderen@vlaanderen.be",
                            Url = new Uri("https://legacy.basisregisters.vlaanderen")
                        }
                    },
                    XmlCommentPaths = new[] { typeof(Startup).GetTypeInfo().Assembly.GetName().Name ?? "RoadRegistry.Projector" }
                },
                MiddlewareHooks =
                {
                    AfterHealthChecks = health =>
                    {
                        var connectionStrings = _configuration
                            .GetSection("ConnectionStrings")
                            .GetChildren();
                        var projectionOptions = _configuration.GetOptions<ProjectionOptions>("Projections");

                        foreach (var connectionString in connectionStrings)
                        {
                            health.AddSqlServer(
                                connectionString.Value,
                                name: $"sqlserver-{connectionString.Key.ToLowerInvariant()}",
                                tags: new[] { DatabaseTag, "sql", "sqlserver" });
                        }

                        if (projectionOptions.Product.Enabled)
                        {
                            health.AddDbContextCheck<ProductContext>();
                        }

                        if (projectionOptions.Editor.Enabled)
                        {
                            health.AddDbContextCheck<EditorContext>();
                        }

                        if (projectionOptions.Syndication.Enabled)
                        {
                            health.AddDbContextCheck<SyndicationContext>();
                        }

                        if (projectionOptions.Wms.Enabled)
                        {
                            health.AddDbContextCheck<WmsContext>();
                        }

                        if (projectionOptions.Wfs.Enabled)
                        {
                            health.AddDbContextCheck<WfsContext>();
                        }

                        if (projectionOptions.ProducerSnapshot.Enabled)
                        {
                            health.AddDbContextCheck<Producer.Snapshot.ProjectionHost.RoadNode.RoadNodeProducerSnapshotContext>();
                            health.AddDbContextCheck<Producer.Snapshot.ProjectionHost.RoadSegment.RoadSegmentProducerSnapshotContext>();
                            health.AddDbContextCheck<Producer.Snapshot.ProjectionHost.RoadSegmentSurface.RoadSegmentSurfaceProducerSnapshotContext>();
                            health.AddDbContextCheck<Producer.Snapshot.ProjectionHost.GradeSeparatedJunction.GradeSeparatedJunctionProducerSnapshotContext>();
                            health.AddDbContextCheck<Producer.Snapshot.ProjectionHost.NationalRoad.NationalRoadProducerSnapshotContext>();

                        }

                    }
                }
            })
            .AddValidatorsFromAssemblyContaining<Startup>();

        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterModule(new LoggingModule(_configuration, services));
        containerBuilder.RegisterModule(new ApiModule(_configuration, services));
        _applicationContainer = containerBuilder.Build();

        return new AutofacServiceProvider(_applicationContainer);
    }

    private static string GetApiLeadingText(ApiVersionDescription description)
    {
        return $"Momenteel leest u de documentatie voor versie {description.ApiVersion} van de Basisregisters Vlaanderen Road Registry API{string.Format(description.IsDeprecated ? ", **deze API versie is niet meer ondersteund * *." : ".")}";
    }
}
