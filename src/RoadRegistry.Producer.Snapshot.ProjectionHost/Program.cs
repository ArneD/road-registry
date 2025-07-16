namespace RoadRegistry.Producer.Snapshot.ProjectionHost
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BackOffice;
    using Be.Vlaanderen.Basisregisters.EventHandling;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.Runner;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore;
    using Extensions;
    using GradeSeparatedJunction;
    using Hosts;
    using Hosts.Infrastructure.Extensions;
    using Hosts.Metadata;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using NationalRoad;
    using Newtonsoft.Json;
    using RoadNode;
    using RoadSegment;
    using RoadSegmentSurface;
    using Shared;
    using ServiceCollectionExtensions = Extensions.ServiceCollectionExtensions;

    public class Program
    {
        public const int HostingPort = 10016;

        protected Program()
        {
        }

        public static async Task Main(string[] args)
        {
            var roadRegistryHost = new RoadRegistryHostBuilder<Program>(args)
                .ConfigureServices((hostContext, services) => services
                    .AddSingleton(provider => provider.GetRequiredService<IConfiguration>().GetSection(MetadataConfiguration.Section).Get<MetadataConfiguration>())
                    .AddStreetNameCache()
                    .AddSingleton(new EnvelopeFactory(
                        RoadNodeEventProcessor.EventMapping,
                        new EventDeserializer((eventData, eventType) =>
                            JsonConvert.DeserializeObject(eventData, eventType, RoadNodeEventProcessor.SerializerSettings)))
                    )
                    .AddSnapshotProducer<RoadNodeProducerSnapshotContext, RoadNodeRecordProjection, RoadNodeEventProcessor>(
                        "RoadNode",
                        (_, kafkaProducer) => new RoadNodeRecordProjection(kafkaProducer),
                        connectedProjection => new AcceptStreamMessage<RoadNodeProducerSnapshotContext>(connectedProjection, RoadNodeEventProcessor.EventMapping)
                    )
                    .AddSnapshotProducer<RoadSegmentProducerSnapshotContext, RoadSegmentRecordProjection, RoadSegmentEventProcessor>(
                        "RoadSegment",
                        (sp, kafkaProducer) => new RoadSegmentRecordProjection(kafkaProducer, sp.GetRequiredService<IStreetNameCache>()),
                        connectedProjection => new AcceptStreamMessage<RoadSegmentProducerSnapshotContext>(connectedProjection, RoadSegmentEventProcessor.EventMapping)
                    )
                    .AddSnapshotProducer<NationalRoadProducerSnapshotContext, NationalRoadRecordProjection, NationalRoadEventProcessor>(
                        "NationalRoad",
                        (_, kafkaProducer) => new NationalRoadRecordProjection(kafkaProducer),
                        connectedProjection => new AcceptStreamMessage<NationalRoadProducerSnapshotContext>(connectedProjection, NationalRoadEventProcessor.EventMapping)
                    )
                    .AddSnapshotProducer<GradeSeparatedJunctionProducerSnapshotContext, GradeSeparatedJunctionRecordProjection, GradeSeparatedJunctionEventProcessor>(
                        "GradeSeparatedJunction",
                        (_, kafkaProducer) => new GradeSeparatedJunctionRecordProjection(kafkaProducer),
                        connectedProjection => new AcceptStreamMessage<GradeSeparatedJunctionProducerSnapshotContext>(connectedProjection, GradeSeparatedJunctionEventProcessor.EventMapping)
                    )
                    .AddSnapshotProducer<RoadSegmentSurfaceProducerSnapshotContext, RoadSegmentSurfaceRecordProjection, RoadSegmentSurfaceEventProcessor>(
                        "RoadSegmentSurface",
                        (_, kafkaProducer) => new RoadSegmentSurfaceRecordProjection(kafkaProducer),
                        connectedProjection => new AcceptStreamMessage<RoadSegmentSurfaceProducerSnapshotContext>(connectedProjection, RoadSegmentSurfaceEventProcessor.EventMapping)
                    )
                    .AddSingleton(typeof(Program).Assembly
                        .GetTypes()
                        .Where(x => !x.IsAbstract && typeof(IRunnerDbContextMigratorFactory).IsAssignableFrom(x))
                        .Select(type => (IRunnerDbContextMigratorFactory)Activator.CreateInstance(type))
                        .ToArray()))
                .ConfigureHealthChecks(HostingPort, builder => builder
                    .AddHostedServicesStatus()
                )
                .Build();

            await roadRegistryHost
                .LogSqlServerConnectionStrings([
                    WellKnownConnectionNames.Events,
                    WellKnownConnectionNames.ProducerSnapshotProjections,
                    WellKnownConnectionNames.ProducerSnapshotProjectionsAdmin
                ])
                .RunAsync(async (sp, host, configuration) =>
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger<Program>();
                    // var migratorFactories = sp.GetRequiredService<IRunnerDbContextMigratorFactory[]>();
                    //
                    // foreach (var migratorFactory in migratorFactories)
                    // {
                    //     await migratorFactory
                    //         .CreateMigrator(configuration, loggerFactory)
                    //         .MigrateAsync(CancellationToken.None).ConfigureAwait(false);
                    // }
                    var from = new DateTimeOffset(2025, 06, 16, 0, 0, 0, TimeSpan.Zero);
                    var to = new DateTimeOffset(2025, 07, 08, 0, 0, 0, TimeSpan.Zero);

                    logger.LogInformation("Starting to produce removed road registry records");
                    var counter = 0;
                    var nodeContext = sp.GetRequiredService<RoadNodeProducerSnapshotContext>();
                    var nodeProducer = new KafkaProducer(configuration.CreateProducerOptions("RoadNodeTopic"));
                    nodeContext.RoadNodes.Where(x => x.IsRemoved && x.LastChangedTimestamp > from && x.LastChangedTimestamp < to)
                        .ToList()
                        .ForEach(x =>
                        {
                            counter++;
                            nodeProducer.Produce(x.Id, x.ToContract(), CancellationToken.None);
                        });

                    logger.LogInformation("Produced {Counter} removed road nodes", counter);
                    // counter = 0;
                    //
                    // var segmentContext = sp.GetRequiredService<RoadSegmentProducerSnapshotContext>();
                    // var segmentProducer = new KafkaProducer(configuration.CreateProducerOptions("RoadSegmentTopic"));
                    // segmentContext.RoadSegments.Where(x => x.IsRemoved && x.LastChangedTimestamp > from && x.LastChangedTimestamp < to)
                    //     .ToList()
                    //     .ForEach(x =>
                    //     {
                    //         counter++;
                    //         segmentProducer.Produce(x.Id, x.ToContract(), CancellationToken.None);
                    //     });
                    //
                    // logger.LogInformation("Produced {Counter} removed road segments", counter);
                    // counter = 0;
                    //
                    // var nationalRoadContext = sp.GetRequiredService<NationalRoadProducerSnapshotContext>();
                    // var nationalRoadProducer = new KafkaProducer(configuration.CreateProducerOptions("NationalRoadTopic"));
                    // nationalRoadContext.NationalRoads.Where(x => x.IsRemoved && x.LastChangedTimestamp > from && x.LastChangedTimestamp < to)
                    //     .ToList()
                    //     .ForEach(x =>
                    //     {
                    //         counter++;
                    //
                    //         nationalRoadProducer.Produce(x.Id, x.ToContract(), CancellationToken.None);
                    //     });
                    //
                    // logger.LogInformation("Produced {Counter} removed national road", counter);
                    // counter = 0;
                    //
                    // var gradeSeparatedJunctionContext = sp.GetRequiredService<GradeSeparatedJunctionProducerSnapshotContext>();
                    // var gradeSeparatedJunctionProducer = new KafkaProducer(configuration.CreateProducerOptions("GradeSeparatedJunctionTopic"));
                    // gradeSeparatedJunctionContext.GradeSeparatedJunctions.Where(x => x.IsRemoved && x.LastChangedTimestamp > from && x.LastChangedTimestamp < to)
                    //     .ToList()
                    //     .ForEach(x =>
                    //     {
                    //         counter++;
                    //
                    //         gradeSeparatedJunctionProducer.Produce(x.Id, x.ToContract(), CancellationToken.None);
                    //     });
                    //
                    // logger.LogInformation("Produced {Counter} removed grade separated junctions", counter);
                    // counter = 0;
                    //
                    // var roadSegmentSurfaceContext = sp.GetRequiredService<RoadSegmentSurfaceProducerSnapshotContext>();
                    // var roadSegmentSurfaceProducer = new KafkaProducer(configuration.CreateProducerOptions("RoadSegmentSurfaceTopic"));
                    // roadSegmentSurfaceContext.RoadSegmentSurfaces.Where(x => x.IsRemoved && x.LastChangedTimestamp > from && x.LastChangedTimestamp < to)
                    //     .ToList()
                    //     .ForEach(x =>
                    //     {
                    //         counter++;
                    //
                    //         roadSegmentSurfaceProducer.Produce(x.Id, x.ToContract(), CancellationToken.None);
                    //     });
                    //
                    // logger.LogInformation("Produced {Counter} removed road surfaces", counter);
                    // counter = 0;

                });
        }
    }
}
