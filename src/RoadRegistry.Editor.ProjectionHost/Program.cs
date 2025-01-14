namespace RoadRegistry.Editor.ProjectionHost;

using Autofac;
using BackOffice;
using BackOffice.Configuration;
using BackOffice.Uploads;
using Be.Vlaanderen.Basisregisters.BlobStore;
using Be.Vlaanderen.Basisregisters.EventHandling;
using Be.Vlaanderen.Basisregisters.ProjectionHandling.Connector;
using Be.Vlaanderen.Basisregisters.ProjectionHandling.Runner;
using Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore;
using EventProcessors;
using Extensions;
using Hosts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Newtonsoft.Json;
using Projections;
using Schema;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    protected Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var roadRegistryHost = CreateHostBuilder(args).Build();

        await roadRegistryHost
            .LogSqlServerConnectionStrings(new[]
            {
                WellknownConnectionNames.Events,
                WellknownConnectionNames.EditorProjections,
                WellknownConnectionNames.EditorProjectionsAdmin
            })
            .Log((sp, logger) =>
            {
                var blobClientOptions = sp.GetRequiredService<BlobClientOptions>();
                logger.LogBlobClientCredentials(blobClientOptions);
            })
            .RunAsync(async (sp, host, configuration) =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var migratorFactory = sp.GetRequiredService<IRunnerDbContextMigratorFactory>();

                await migratorFactory.CreateMigrator(configuration, loggerFactory)
                    .MigrateAsync(CancellationToken.None).ConfigureAwait(false);
            });
    }

    public static RoadRegistryHostBuilder<Program> CreateHostBuilder(string[] args) => new RoadRegistryHostBuilder<Program>(args)
        .ConfigureServices((hostContext, services) => services
            .AddSingleton(new EnvelopeFactory(
                EditorContextEventProcessor.EventMapping,
                new EventDeserializer((eventData, eventType) =>
                    JsonConvert.DeserializeObject(eventData, eventType, EditorContextEventProcessor.SerializerSettings)))
            )
            .AddSingleton(() =>
                new EditorContext(
                    new DbContextOptionsBuilder<EditorContext>()
                        .UseSqlServer(
                            hostContext.Configuration.GetConnectionString(WellknownConnectionNames.EditorProjections),
                            options => options
                                .EnableRetryOnFailure()
                                .UseNetTopologySuite()
                        ).Options)
            )
            .AddSingleton<IRunnerDbContextMigratorFactory>(new EditorContextMigrationFactory())
            .AddEditorContextEventProcessor<RoadNetworkEventProcessor>(sp => new ConnectedProjection<EditorContext>[]
            {
                new RoadNetworkInfoProjection(),
                new GradeSeparatedJunctionRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>()),
                new RoadNodeRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>()),
                new RoadSegmentEuropeanRoadAttributeRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>()),
                new RoadSegmentLaneAttributeRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>()),
                new RoadSegmentNationalRoadAttributeRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>()),
                new RoadSegmentNumberedRoadAttributeRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>()),
                new RoadSegmentRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>()),
                new RoadSegmentSurfaceAttributeRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>()),
                new RoadSegmentWidthAttributeRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>())
            })
            .AddEditorContextEventProcessor<OrganizationEventProcessor>(sp => new ConnectedProjection<EditorContext>[]
            {
                new OrganizationRecordProjection(sp.GetRequiredService<RecyclableMemoryStreamManager>(), sp.GetRequiredService<FileEncoding>())
            })
            .AddEditorContextEventProcessor<MunicipalityEventProcessor>(sp => new ConnectedProjection<EditorContext>[]
            {
                new MunicipalityGeometryProjection()
            })
            .AddEditorContextEventProcessor<ChangeFeedEventProcessor>(sp => new ConnectedProjection<EditorContext>[]
            {
                new RoadNetworkChangeFeedProjection(sp.GetRequiredService<IBlobClient>())
            })
            .AddEditorContextEventProcessor<ExtractDownloadEventProcessor>(sp => new ConnectedProjection<EditorContext>[]
            {
                new ExtractDownloadRecordProjection()
            })
            .AddEditorContextEventProcessor<ExtractRequestEventProcessor>(sp => new ConnectedProjection<EditorContext>[]
            {
                new ExtractRequestRecordProjection()
            })
            .AddEditorContextEventProcessor<ExtractUploadEventProcessor>(sp => new ConnectedProjection<EditorContext>[]
            {
                new ExtractUploadRecordProjection()
            })
        )
        .ConfigureContainer((context, builder) =>
        {
            builder
                .Register(c => c.Resolve<RoadNetworkUploadsBlobClient>())
                .As<IBlobClient>().SingleInstance();
        });
}
