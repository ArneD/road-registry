namespace RoadRegistry.Producer.Snapshot.ProjectionHost.Tests.Projections;

using System.Globalization;
using AutoFixture;
using BackOffice;
using BackOffice.Abstractions;
using BackOffice.Messages;
using Be.Vlaanderen.Basisregisters.GrAr.Contracts.RoadRegistry;
using Be.Vlaanderen.Basisregisters.MessageHandling.Kafka.Simple;
using Extensions;
using Moq;
using ProjectionHost.Projections;
using RoadRegistry.Tests.BackOffice;
using RoadRegistry.Tests.BackOffice.Uploads;
using RoadRegistry.Tests.Framework.Projections;
using RoadSegment;
using Syndication.Schema;

public class RoadSegmentRecordProjectionTests : IClassFixture<ProjectionTestServices>
{
    private readonly Fixture _fixture;
    private readonly ProjectionTestServices _services;

    public RoadSegmentRecordProjectionTests(ProjectionTestServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        _fixture = new Fixture();
        _fixture.CustomizeArchiveId();
        _fixture.CustomizeOperatorName();
        _fixture.CustomizeOriginProperties();
        _fixture.CustomizeRoadSegmentId();
        _fixture.CustomizeRoadSegmentMorphology();
        _fixture.CustomizeRoadSegmentStatus();
        _fixture.CustomizeOrganizationId();
        _fixture.CustomizeOrganizationName();
        _fixture.CustomizeRoadSegmentAccessRestriction();
        _fixture.CustomizeRoadSegmentCategory();
        _fixture.CustomizeRoadSegmentGeometryDrawMethod();
        _fixture.CustomizeRoadSegmentGeometryVersion();
        _fixture.CustomizeRoadSegmentPosition();
        _fixture.CustomizeMultiPolygon();
        _fixture.CustomizePolylineM();
        _fixture.CustomizeImportedRoadSegment();
        _fixture.CustomizeRoadNetworkChangesAccepted();
        _fixture.CustomizeCrabStreetnameId();
        _fixture.CustomizeImportedRoadSegmentSideAttributes();

        _fixture.CustomizeRoadSegmentAdded();
        _fixture.CustomizeRoadSegmentModified();
        _fixture.CustomizeRoadSegmentAttributesModified();
        _fixture.CustomizeRoadSegmentGeometryModified();
        _fixture.CustomizeRoadSegmentRemoved();
    }

    private static Mock<IKafkaProducer> BuildKafkaProducer()
    {
        var kafkaProducer = new Mock<IKafkaProducer>();
        kafkaProducer
            .Setup(x => x.Produce(It.IsAny<string>(), It.IsAny<RoadSegmentSnapshot>(), CancellationToken.None))
            .ReturnsAsync(Result<RoadSegmentSnapshot>.Success(It.IsAny<RoadSegmentSnapshot>()));
        return kafkaProducer;
    }

    private static Mock<IStreetNameCache> BuildStreetNameCache()
    {
        var streetNameCache = new Mock<IStreetNameCache>();
        streetNameCache
            .Setup(x => x.GetAsync(It.IsAny<int>(), CancellationToken.None))
            .ReturnsAsync((StreetNameCacheItem)null);
        return streetNameCache;
    }

    private static ICollection<object> ConvertToRoadSegmentRecords(RoadNetworkChangesAccepted message, DateTimeOffset created, Action<RoadSegmentRecord> modifier = null)
    {
        return Array.ConvertAll(message.Changes, change =>
        {
            var roadSegmentAdded = change.RoadSegmentAdded;

            var transactionId = new TransactionId(message.TransactionId);
            var method = RoadSegmentGeometryDrawMethod.Parse(roadSegmentAdded.GeometryDrawMethod);
            var accessRestriction = RoadSegmentAccessRestriction.Parse(roadSegmentAdded.AccessRestriction);
            var status = RoadSegmentStatus.Parse(roadSegmentAdded.Status);
            var morphology = RoadSegmentMorphology.Parse(roadSegmentAdded.Morphology);
            var category = RoadSegmentCategory.Parse(roadSegmentAdded.Category);

            var record = new RoadSegmentRecord
            {
                Id = roadSegmentAdded.Id,
                Version = roadSegmentAdded.Version,

                MaintainerId = roadSegmentAdded.MaintenanceAuthority.Code,
                MaintainerName = roadSegmentAdded.MaintenanceAuthority.Name,

                MethodId = method.Translation.Identifier,
                MethodDutchName = method.Translation.Name,

                CategoryId = category.Translation.Identifier,
                CategoryDutchName = category.Translation.Name,

                Geometry = GeometryTranslator.Translate(roadSegmentAdded.Geometry),
                GeometryVersion = roadSegmentAdded.GeometryVersion,

                MorphologyId = morphology.Translation.Identifier,
                MorphologyDutchName = morphology.Translation.Name,

                StatusId = status.Translation.Identifier,
                StatusDutchName = status.Translation.Name,

                AccessRestrictionId = accessRestriction.Translation.Identifier,
                AccessRestrictionDutchName = accessRestriction.Translation.Name,

                RecordingDate = LocalDateTimeTranslator.TranslateFromWhen(message.When),
                TransactionId = transactionId == TransactionId.Unknown ? default(int?) : transactionId.ToInt32(),

                LeftSideMunicipalityId = null,
                LeftSideMunicipalityNisCode = null,
                LeftSideStreetNameId = roadSegmentAdded.LeftSide?.StreetNameId,
                LeftSideStreetName = null,

                RightSideMunicipalityId = null,
                RightSideMunicipalityNisCode = null,
                RightSideStreetNameId = roadSegmentAdded.RightSide?.StreetNameId,
                RightSideStreetName = null,

                RoadSegmentVersion = roadSegmentAdded.Version,

                BeginRoadNodeId = roadSegmentAdded.StartNodeId,
                EndRoadNodeId = roadSegmentAdded.EndNodeId,
                StreetNameCachePosition = 0L,

                Origin = message.ToOrigin(),
                LastChangedTimestamp = created
            };

            modifier?.Invoke(record);
            return (object)record;
        });
    }

    private void KafkaVerify(Mock<IKafkaProducer> kafkaProducer, IEnumerable<object> expectedRecords, Times? times = null)
    {
        foreach (var expectedRecord in expectedRecords.Cast<RoadSegmentRecord>())
        {
            kafkaProducer.Verify(
                x => x.Produce(
                    expectedRecord.Id.ToString(CultureInfo.InvariantCulture),
                    It.Is(expectedRecord.ToContract(), new RoadSegmentSnapshotEqualityComparer()),
                    It.IsAny<CancellationToken>()),
                times ?? Times.Once());
        }
    }

    [Fact]
    public async Task When_adding_road_segment()
    {
        var message = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.CreateMany<RoadSegmentAdded>());

        var created = DateTimeOffset.UtcNow;

        var expectedRecords = ConvertToRoadSegmentRecords(message, created);

        var kafkaProducer = BuildKafkaProducer();
        var streetNameCache = BuildStreetNameCache();

        await new RoadSegmentRecordProjection(kafkaProducer.Object, streetNameCache.Object)
            .Scenario()
            .Given(message)
            .Expect(created.UtcDateTime, expectedRecords);

        KafkaVerify(kafkaProducer, expectedRecords);
    }

    [Fact]
    public async Task When_modifying_road_segment_attributes()
    {
        _fixture.Freeze<RoadSegmentId>();

        var acceptedRoadSegmentAdded = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentAdded>());

        var acceptedRoadSegmentAttributesModified = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentAttributesModified>());

        var created = DateTimeOffset.UtcNow;

        var expectedRecords = Array.ConvertAll(acceptedRoadSegmentAttributesModified.Changes, change =>
        {
            var segmentAdded = acceptedRoadSegmentAdded.Changes[0].RoadSegmentAdded;

            var roadSegmentModified = change.RoadSegmentAttributesModified;
            var transactionId = new TransactionId(acceptedRoadSegmentAttributesModified.TransactionId);
            var method = RoadSegmentGeometryDrawMethod.Parse(segmentAdded.GeometryDrawMethod);
            var accessRestriction = RoadSegmentAccessRestriction.Parse(roadSegmentModified.AccessRestriction);
            var status = RoadSegmentStatus.Parse(roadSegmentModified.Status);
            var morphology = RoadSegmentMorphology.Parse(roadSegmentModified.Morphology);
            var category = RoadSegmentCategory.Parse(roadSegmentModified.Category);

            var roadSegmentRecord = new RoadSegmentRecord
            {
                Id = roadSegmentModified.Id,
                Version = roadSegmentModified.Version,

                MaintainerId = roadSegmentModified.MaintenanceAuthority.Code,
                MaintainerName = roadSegmentModified.MaintenanceAuthority.Name,

                MethodId = method.Translation.Identifier,
                MethodDutchName = method.Translation.Name,

                CategoryId = category.Translation.Identifier,
                CategoryDutchName = category.Translation.Name,

                Geometry = GeometryTranslator.Translate(segmentAdded.Geometry),
                GeometryVersion = segmentAdded.GeometryVersion,

                MorphologyId = morphology.Translation.Identifier,
                MorphologyDutchName = morphology.Translation.Name,

                StatusId = status.Translation.Identifier,
                StatusDutchName = status.Translation.Name,

                AccessRestrictionId = accessRestriction.Translation.Identifier,
                AccessRestrictionDutchName = accessRestriction.Translation.Name,

                RecordingDate = LocalDateTimeTranslator.TranslateFromWhen(acceptedRoadSegmentAdded.When),
                TransactionId = transactionId == TransactionId.Unknown ? default(int?) : transactionId.ToInt32(),

                LeftSideMunicipalityId = null,
                LeftSideMunicipalityNisCode = null,
                LeftSideStreetNameId = segmentAdded.LeftSide?.StreetNameId,
                LeftSideStreetName = null,

                RightSideMunicipalityId = null,
                RightSideMunicipalityNisCode = null,
                RightSideStreetNameId = segmentAdded.RightSide?.StreetNameId,
                RightSideStreetName = null,

                RoadSegmentVersion = roadSegmentModified.Version,

                BeginRoadNodeId = segmentAdded.StartNodeId,
                EndRoadNodeId = segmentAdded.EndNodeId,
                StreetNameCachePosition = 0L,

                Origin = acceptedRoadSegmentAttributesModified.ToOrigin(),
                LastChangedTimestamp = created
            };
            return (object)roadSegmentRecord;
        });

        var kafkaProducer = BuildKafkaProducer();
        var streetNameCache = BuildStreetNameCache();

        await new RoadSegmentRecordProjection(kafkaProducer.Object, streetNameCache.Object)
            .Scenario()
            .Given(acceptedRoadSegmentAdded, acceptedRoadSegmentAttributesModified)
            .Expect(created.UtcDateTime, expectedRecords);

        KafkaVerify(kafkaProducer, expectedRecords);
    }

    [Fact]
    public async Task When_modifying_road_segment_geometry()
    {
        _fixture.Freeze<RoadSegmentId>();

        var acceptedRoadSegmentAdded = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentAdded>());

        var acceptedRoadSegmentGeometryModified = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentGeometryModified>());

        var created = DateTimeOffset.UtcNow;

        var expectedRecords = Array.ConvertAll(acceptedRoadSegmentGeometryModified.Changes, change =>
        {
            var segmentAdded = acceptedRoadSegmentAdded.Changes[0].RoadSegmentAdded;

            var roadSegmentModified = change.RoadSegmentGeometryModified;
            var transactionId = new TransactionId(acceptedRoadSegmentGeometryModified.TransactionId);
            var method = RoadSegmentGeometryDrawMethod.Parse(segmentAdded.GeometryDrawMethod);
            var accessRestriction = RoadSegmentAccessRestriction.Parse(segmentAdded.AccessRestriction);
            var status = RoadSegmentStatus.Parse(segmentAdded.Status);
            var morphology = RoadSegmentMorphology.Parse(segmentAdded.Morphology);
            var category = RoadSegmentCategory.Parse(segmentAdded.Category);

            var roadSegmentRecord = new RoadSegmentRecord
            {
                Id = roadSegmentModified.Id,
                Version = roadSegmentModified.Version,

                MaintainerId = segmentAdded.MaintenanceAuthority.Code,
                MaintainerName = segmentAdded.MaintenanceAuthority.Name,

                MethodId = method.Translation.Identifier,
                MethodDutchName = method.Translation.Name,

                CategoryId = category.Translation.Identifier,
                CategoryDutchName = category.Translation.Name,

                Geometry = GeometryTranslator.Translate(roadSegmentModified.Geometry),
                GeometryVersion = roadSegmentModified.GeometryVersion,

                MorphologyId = morphology.Translation.Identifier,
                MorphologyDutchName = morphology.Translation.Name,

                StatusId = status.Translation.Identifier,
                StatusDutchName = status.Translation.Name,

                AccessRestrictionId = accessRestriction.Translation.Identifier,
                AccessRestrictionDutchName = accessRestriction.Translation.Name,

                RecordingDate = LocalDateTimeTranslator.TranslateFromWhen(acceptedRoadSegmentAdded.When),
                TransactionId = transactionId == TransactionId.Unknown ? default(int?) : transactionId.ToInt32(),

                LeftSideMunicipalityId = null,
                LeftSideMunicipalityNisCode = null,
                LeftSideStreetNameId = segmentAdded.LeftSide?.StreetNameId,
                LeftSideStreetName = null,

                RightSideMunicipalityId = null,
                RightSideMunicipalityNisCode = null,
                RightSideStreetNameId = segmentAdded.RightSide?.StreetNameId,
                RightSideStreetName = null,

                RoadSegmentVersion = roadSegmentModified.Version,

                BeginRoadNodeId = segmentAdded.StartNodeId,
                EndRoadNodeId = segmentAdded.EndNodeId,
                StreetNameCachePosition = 0L,

                Origin = acceptedRoadSegmentGeometryModified.ToOrigin(),
                LastChangedTimestamp = created
            };
            return (object)roadSegmentRecord;
        });

        var kafkaProducer = BuildKafkaProducer();
        var streetNameCache = BuildStreetNameCache();

        await new RoadSegmentRecordProjection(kafkaProducer.Object, streetNameCache.Object)
            .Scenario()
            .Given(acceptedRoadSegmentAdded, acceptedRoadSegmentGeometryModified)
            .Expect(created.UtcDateTime, expectedRecords);

        KafkaVerify(kafkaProducer, expectedRecords);
    }

    [Fact]
    public async Task When_modifying_road_segments()
    {
        _fixture.Freeze<RoadSegmentId>();

        var acceptedRoadSegmentAdded = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentAdded>());

        var acceptedRoadSegmentModified = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentModified>());

        var created = DateTimeOffset.UtcNow;

        var expectedRecords = Array.ConvertAll(acceptedRoadSegmentModified.Changes, change =>
        {
            var roadSegmentModified = change.RoadSegmentModified;
            var transactionId = new TransactionId(acceptedRoadSegmentModified.TransactionId);
            var method = RoadSegmentGeometryDrawMethod.Parse(roadSegmentModified.GeometryDrawMethod);
            var accessRestriction = RoadSegmentAccessRestriction.Parse(roadSegmentModified.AccessRestriction);
            var status = RoadSegmentStatus.Parse(roadSegmentModified.Status);
            var morphology = RoadSegmentMorphology.Parse(roadSegmentModified.Morphology);
            var category = RoadSegmentCategory.Parse(roadSegmentModified.Category);

            var roadSegmentRecord = new RoadSegmentRecord
            {
                Id = roadSegmentModified.Id,
                Version = roadSegmentModified.Version,

                MaintainerId = roadSegmentModified.MaintenanceAuthority.Code,
                MaintainerName = roadSegmentModified.MaintenanceAuthority.Name,

                MethodId = method.Translation.Identifier,
                MethodDutchName = method.Translation.Name,

                CategoryId = category.Translation.Identifier,
                CategoryDutchName = category.Translation.Name,

                Geometry = GeometryTranslator.Translate(roadSegmentModified.Geometry),
                GeometryVersion = roadSegmentModified.GeometryVersion,

                MorphologyId = morphology.Translation.Identifier,
                MorphologyDutchName = morphology.Translation.Name,

                StatusId = status.Translation.Identifier,
                StatusDutchName = status.Translation.Name,

                AccessRestrictionId = accessRestriction.Translation.Identifier,
                AccessRestrictionDutchName = accessRestriction.Translation.Name,

                RecordingDate = LocalDateTimeTranslator.TranslateFromWhen(acceptedRoadSegmentAdded.When),
                TransactionId = transactionId == TransactionId.Unknown ? default(int?) : transactionId.ToInt32(),

                LeftSideMunicipalityId = null,
                LeftSideMunicipalityNisCode = null,
                LeftSideStreetNameId = roadSegmentModified.LeftSide?.StreetNameId,
                LeftSideStreetName = null,

                RightSideMunicipalityId = null,
                RightSideMunicipalityNisCode = null,
                RightSideStreetNameId = roadSegmentModified.RightSide?.StreetNameId,
                RightSideStreetName = null,

                RoadSegmentVersion = roadSegmentModified.Version,

                BeginRoadNodeId = roadSegmentModified.StartNodeId,
                EndRoadNodeId = roadSegmentModified.EndNodeId,
                StreetNameCachePosition = 0L,

                Origin = acceptedRoadSegmentModified.ToOrigin(),
                LastChangedTimestamp = created
            };
            return (object)roadSegmentRecord;
        });

        var kafkaProducer = BuildKafkaProducer();
        var streetNameCache = BuildStreetNameCache();

        await new RoadSegmentRecordProjection(kafkaProducer.Object, streetNameCache.Object)
            .Scenario()
            .Given(acceptedRoadSegmentAdded, acceptedRoadSegmentModified)
            .Expect(created.UtcDateTime, expectedRecords);

        KafkaVerify(kafkaProducer, expectedRecords);
    }

    [Fact]
    public async Task When_removing_road_segments()
    {
        _fixture.Freeze<RoadSegmentId>();

        var acceptedRoadSegmentAdded = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentAdded>());

        var acceptedRoadSegmentRemoved = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentRemoved>());

        var created = DateTimeOffset.UtcNow;

        var expectedRecords = Array.ConvertAll(acceptedRoadSegmentAdded.Changes, change =>
        {
            var roadSegmentAdded = change.RoadSegmentAdded;
            var transactionId = new TransactionId(acceptedRoadSegmentAdded.TransactionId);
            var method = RoadSegmentGeometryDrawMethod.Parse(roadSegmentAdded.GeometryDrawMethod);
            var accessRestriction = RoadSegmentAccessRestriction.Parse(roadSegmentAdded.AccessRestriction);
            var status = RoadSegmentStatus.Parse(roadSegmentAdded.Status);
            var morphology = RoadSegmentMorphology.Parse(roadSegmentAdded.Morphology);
            var category = RoadSegmentCategory.Parse(roadSegmentAdded.Category);

            return (object)new RoadSegmentRecord
            {
                Id = roadSegmentAdded.Id,
                Version = roadSegmentAdded.Version,

                MaintainerId = roadSegmentAdded.MaintenanceAuthority.Code,
                MaintainerName = roadSegmentAdded.MaintenanceAuthority.Name,

                MethodId = method.Translation.Identifier,
                MethodDutchName = method.Translation.Name,

                CategoryId = category.Translation.Identifier,
                CategoryDutchName = category.Translation.Name,

                Geometry = GeometryTranslator.Translate(roadSegmentAdded.Geometry),
                GeometryVersion = roadSegmentAdded.GeometryVersion,

                MorphologyId = morphology.Translation.Identifier,
                MorphologyDutchName = morphology.Translation.Name,

                StatusId = status.Translation.Identifier,
                StatusDutchName = status.Translation.Name,

                AccessRestrictionId = accessRestriction.Translation.Identifier,
                AccessRestrictionDutchName = accessRestriction.Translation.Name,

                RecordingDate = LocalDateTimeTranslator.TranslateFromWhen(acceptedRoadSegmentAdded.When),
                TransactionId = transactionId == TransactionId.Unknown ? default(int?) : transactionId.ToInt32(),

                LeftSideMunicipalityId = null,
                LeftSideMunicipalityNisCode = null,
                LeftSideStreetNameId = roadSegmentAdded.LeftSide.StreetNameId,
                LeftSideStreetName = null,

                RightSideMunicipalityId = null,
                RightSideMunicipalityNisCode = null,
                RightSideStreetNameId = roadSegmentAdded.RightSide.StreetNameId,
                RightSideStreetName = null,

                RoadSegmentVersion = roadSegmentAdded.Version,

                BeginRoadNodeId = roadSegmentAdded.StartNodeId,
                EndRoadNodeId = roadSegmentAdded.EndNodeId,
                StreetNameCachePosition = 0L,

                Origin = acceptedRoadSegmentAdded.ToOrigin(),
                LastChangedTimestamp = created.AddDays(-1),
                IsRemoved = true
            };
        });

        expectedRecords = Array.ConvertAll(acceptedRoadSegmentRemoved.Changes, change =>
        {
            var roadSegmentRemoved = change.RoadSegmentRemoved;

            var roadSegmentRecord = expectedRecords.Cast<RoadSegmentRecord>().Single(x => x.Id == roadSegmentRemoved.Id);
            roadSegmentRecord.Origin = acceptedRoadSegmentRemoved.ToOrigin();
            roadSegmentRecord.IsRemoved = true;
            roadSegmentRecord.LastChangedTimestamp = created;
            return (object)roadSegmentRecord;
        });

        var kafkaProducer = BuildKafkaProducer();
        var streetNameCache = BuildStreetNameCache();

        await new RoadSegmentRecordProjection(kafkaProducer.Object, streetNameCache.Object)
            .Scenario()
            .Given(acceptedRoadSegmentAdded, acceptedRoadSegmentRemoved)
            .Expect(created.UtcDateTime, expectedRecords);

        KafkaVerify(kafkaProducer, expectedRecords);
    }

    [Fact]
    public async Task When_road_segments_were_imported()
    {
        var created = DateTimeOffset.UtcNow;

        var data = _fixture
            .CreateMany<ImportedRoadSegment>(new Random().Next(1, 100))
            .Select(@event =>
            {
                @event.When = LocalDateTimeTranslator.TranslateToWhen(_fixture.Create<DateTime>());
                @event.Origin.TransactionId = _fixture.Create<TransactionId>();

                var method = RoadSegmentGeometryDrawMethod.Parse(@event.GeometryDrawMethod);
                var accessRestriction = RoadSegmentAccessRestriction.Parse(@event.AccessRestriction);
                var status = RoadSegmentStatus.Parse(@event.Status);
                var morphology = RoadSegmentMorphology.Parse(@event.Morphology);
                var category = RoadSegmentCategory.Parse(@event.Category);

                var expectedRecord = new RoadSegmentRecord
                {
                    Id = @event.Id,

                    MaintainerId = @event.MaintenanceAuthority.Code,
                    MaintainerName = @event.MaintenanceAuthority.Name,

                    MethodId = method.Translation.Identifier,
                    MethodDutchName = method.Translation.Name,

                    CategoryId = category.Translation.Identifier,
                    CategoryDutchName = category.Translation.Name,

                    Geometry = GeometryTranslator.Translate(@event.Geometry),
                    GeometryVersion = @event.GeometryVersion,

                    MorphologyId = morphology.Translation.Identifier,
                    MorphologyDutchName = morphology.Translation.Name,

                    StatusId = status.Translation.Identifier,
                    StatusDutchName = status.Translation.Name,

                    AccessRestrictionId = accessRestriction.Translation.Identifier,
                    AccessRestrictionDutchName = accessRestriction.Translation.Name,

                    RecordingDate = @event.RecordingDate,
                    TransactionId = @event.Origin.TransactionId,

                    LeftSideMunicipalityId = null,
                    LeftSideMunicipalityNisCode = @event.LeftSide.MunicipalityNISCode,
                    LeftSideStreetNameId = @event.LeftSide.StreetNameId,
                    LeftSideStreetName = @event.LeftSide.StreetName,

                    RightSideMunicipalityId = null,
                    RightSideMunicipalityNisCode = @event.RightSide.MunicipalityNISCode,
                    RightSideStreetNameId = @event.RightSide.StreetNameId,
                    RightSideStreetName = @event.RightSide.StreetName,

                    RoadSegmentVersion = @event.Version,

                    BeginRoadNodeId = @event.StartNodeId,
                    EndRoadNodeId = @event.EndNodeId,
                    StreetNameCachePosition = 0L,

                    Origin = @event.Origin.ToOrigin(),
                    LastChangedTimestamp = created,
                    IsRemoved = false
                };

                return new
                {
                    ImportedRoadSegment = @event,
                    ExpectedRecord = expectedRecord
                };
            }).ToList();

        var kafkaProducer = BuildKafkaProducer();
        var streetNameCache = BuildStreetNameCache();

        await new RoadSegmentRecordProjection(kafkaProducer.Object, streetNameCache.Object)
            .Scenario()
            .Given(data.Select(d => d.ImportedRoadSegment))
            .Expect(created.UtcDateTime, data.Select(d => d.ExpectedRecord));

        var expectedRecords = data.AsReadOnly().Select(x => x.ExpectedRecord).ToArray();
        KafkaVerify(kafkaProducer, expectedRecords);
    }

    [Fact]
    public async Task When_adding_road_segments_which_were_previously_removed()
    {
        _fixture.Freeze<RoadSegmentId>();

        var acceptedRoadSegmentAdded = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentAdded>());

        var acceptedRoadSegmentRemoved = _fixture
            .Create<RoadNetworkChangesAccepted>()
            .WithAcceptedChanges(_fixture.Create<RoadSegmentRemoved>());

        var created = DateTimeOffset.UtcNow;

        var expectedRecords = ConvertToRoadSegmentRecords(acceptedRoadSegmentAdded, created);

        var kafkaProducer = BuildKafkaProducer();
        var streetNameCache = BuildStreetNameCache();

        await new RoadSegmentRecordProjection(kafkaProducer.Object, streetNameCache.Object)
            .Scenario()
            .Given(acceptedRoadSegmentAdded, acceptedRoadSegmentRemoved, acceptedRoadSegmentAdded)
            .Expect(created.UtcDateTime, expectedRecords);

        KafkaVerify(kafkaProducer, expectedRecords, Times.Exactly(2));
    }
}
