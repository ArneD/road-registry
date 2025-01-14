namespace RoadRegistry.BackOffice.ExtractHost;

using System;
using System.Threading.Tasks;
using Abstractions;
using Be.Vlaanderen.Basisregisters.BlobStore.Sql;
using Configuration;
using Editor.Schema;
using Extensions;
using Extracts;
using FeatureCompare;
using Framework;
using Handlers.Extracts;
using Hosts;
using Hosts.Infrastructure.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using SqlStreamStore;
using Uploads;
using ZipArchiveWriters.ExtractHost;

public class Program
{
    private static readonly ApplicationMetadata ApplicationMetadata = new(RoadRegistryApplication.BackOffice);

    protected Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var roadRegistryHost = new RoadRegistryHostBuilder<Program>(args)
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .AddHostedService<EventProcessor>()
                    .RegisterOptions<ZipArchiveWriterOptions>()
                    .AddRoadRegistrySnapshot()
                    .AddSingleton<IEventProcessorPositionStore>(sp =>
                        new SqlEventProcessorPositionStore(
                            new SqlConnectionStringBuilder(
                                sp.GetService<IConfiguration>().GetConnectionString(WellknownConnectionNames.ExtractHost)
                            ),
                            WellknownSchemas.ExtractHostSchema))
                    .AddStreetNameCache()
                    .AddSingleton<IZipArchiveWriter<EditorContext>>(sp =>
                        new RoadNetworkExtractToZipArchiveWriter(
                            sp.GetService<ZipArchiveWriterOptions>(),
                            sp.GetService<IStreetNameCache>(),
                            sp.GetService<RecyclableMemoryStreamManager>(),
                            sp.GetRequiredService<FileEncoding>()))
                    .AddSingleton<Func<EditorContext>>(sp =>
                        () =>
                            new EditorContext(
                                new DbContextOptionsBuilder<EditorContext>()
                                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                                    .UseLoggerFactory(sp.GetService<ILoggerFactory>())
                                    .UseSqlServer(
                                        hostContext.Configuration.GetConnectionString(WellknownConnectionNames.EditorProjections),
                                        options => options
                                            .UseNetTopologySuite()
                                    ).Options)
                    )
                    .AddSingleton<IRoadNetworkExtractArchiveAssembler>(sp =>
                        new RoadNetworkExtractArchiveAssembler(
                            sp.GetService<RecyclableMemoryStreamManager>(),
                            sp.GetService<Func<EditorContext>>(),
                            sp.GetService<IZipArchiveWriter<EditorContext>>()))
                    .AddSingleton(sp => new EventHandlerModule[]
                    {
                        new RoadNetworkExtractEventModule(
                            sp.GetService<RoadNetworkExtractDownloadsBlobClient>(),
                            sp.GetService<RoadNetworkExtractUploadsBlobClient>(),
                            sp.GetService<IRoadNetworkExtractArchiveAssembler>(),
                            new ZipArchiveTranslator(sp.GetRequiredService<FileEncoding>()),
                            new ZipArchiveFeatureCompareTranslator(sp.GetRequiredService<FileEncoding>(), sp.GetRequiredService<ILogger<ZipArchiveFeatureCompareTranslator>>()),
                            sp.GetService<IStreamStore>(),
                            ApplicationMetadata)
                    })
                    .AddSingleton(sp => AcceptStreamMessage.WhenEqualToMessageType(sp.GetRequiredService<EventHandlerModule[]>(), EventProcessor.EventMapping))
                    .AddSingleton(sp => Dispatch.Using(Resolve.WhenEqualToMessage(sp.GetRequiredService<EventHandlerModule[]>())));
            })
            .Build();

        await roadRegistryHost
            .LogSqlServerConnectionStrings(new []
            {
                WellknownConnectionNames.Events,
                WellknownConnectionNames.ExtractHost,
                WellknownConnectionNames.ExtractHostAdmin,
                WellknownConnectionNames.Snapshots,
                WellknownConnectionNames.SnapshotsAdmin,
                WellknownConnectionNames.EditorProjections,
                WellknownConnectionNames.SyndicationProjections
            })
            .Log((sp, logger) => {
                var blobClientOptions = sp.GetService<BlobClientOptions>();
                logger.LogBlobClientCredentials(blobClientOptions);
            })
            .RunAsync(async (sp, host, configuration) =>
            {
                await new SqlBlobSchema(new SqlConnectionStringBuilder(configuration.GetConnectionString(WellknownConnectionNames.SnapshotsAdmin))).CreateSchemaIfNotExists(WellknownSchemas.SnapshotSchema).ConfigureAwait(false);
                await new SqlEventProcessorPositionStoreSchema(new SqlConnectionStringBuilder(configuration.GetConnectionString(WellknownConnectionNames.ExtractHostAdmin))).CreateSchemaIfNotExists(WellknownSchemas.ExtractHostSchema).ConfigureAwait(false);
            });
    }
}
