namespace RoadRegistry.BackOffice.Handlers.Sqs.Lambda.Tests.RoadSegmentsOutline.Fixtures;

using Abstractions.Fixtures;
using AutoFixture;
using BackOffice.Abstractions.RoadSegmentsOutline;
using Be.Vlaanderen.Basisregisters.Shaperon.Geometries;
using Be.Vlaanderen.Basisregisters.Sqs.Lambda.Handlers;
using Be.Vlaanderen.Basisregisters.Sqs.Lambda.Infrastructure;
using Core;
using Messages;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NodaTime;
using NodaTime.Text;
using RoadRegistry.Tests.BackOffice;
using SqlStreamStore;
using LineString = NetTopologySuite.Geometries.LineString;

public class WhenCreateOutlineWithValidRequestFixture : WhenCreateOutlineFixture
{
    public WhenCreateOutlineWithValidRequestFixture(IConfiguration configuration, ICustomRetryPolicy customRetryPolicy, IStreamStore streamStore, IRoadRegistryContext roadRegistryContext, IRoadNetworkCommandQueue roadNetworkCommandQueue, IIdempotentCommandHandler idempotentCommandHandler, IClock clock)
        : base(configuration, customRetryPolicy, streamStore, roadRegistryContext, roadNetworkCommandQueue, idempotentCommandHandler, clock)
    {
        ObjectProvider.CustomizeRoadSegmentOutlineStatus();
        ObjectProvider.CustomizeRoadSegmentOutlineSurfaceType();
        ObjectProvider.CustomizeRoadSegmentOutlineWidth();
        ObjectProvider.CustomizeRoadSegmentOutlineLaneCount();
        ObjectProvider.CustomizeRoadSegmentOutlineMorphology();

        ObjectProvider.Customize<LineString>(customization =>
            customization.FromFactory(generator => new LineString(
                new CoordinateArraySequence(new Coordinate[] { new CoordinateM(0, 0, 0), new CoordinateM(1, 0, 1) }),
                GeometryConfiguration.GeometryFactory)
            ).OmitAutoProperties()
        );
    }
    
    protected override CreateRoadSegmentOutlineRequest Request => new(
        ObjectProvider.Create<MultiLineString>(),
        ObjectProvider.Create<RoadSegmentStatus>(),
        ObjectProvider.Create<RoadSegmentMorphology>(),
        ObjectProvider.Create<RoadSegmentAccessRestriction>(),
        ObjectProvider.Create<OrganizationId>(),
        ObjectProvider.Create<RoadSegmentSurfaceType>(),
        ObjectProvider.Create<RoadSegmentWidth>(),
        ObjectProvider.Create<RoadSegmentLaneCount>(),
        ObjectProvider.Create<RoadSegmentLaneDirection>()
    );

    protected override async Task SetupAsync()
    {
        await Given(Organizations.ToStreamName(new OrganizationId(Organisation.ToString())), new ImportedOrganization
        {
            Code = Organisation.ToString(),
            Name = Organisation.ToString(),
            When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
        });
    }

    protected override async Task<bool> VerifyTicketAsync()
    {
        var rejectCommand = await Store.GetLastCommandIfTypeIs<RoadNetworkChangesRejected>();
        if (rejectCommand != null)
        {
            var problems = rejectCommand.Changes.SelectMany(change => change.Problems).ToArray();
            if (problems.Any())
            {
                throw new Exception(string.Join(Environment.NewLine, problems.Select(x => x.ToString())));
            }
        }

        var roadSegmentId = new RoadSegmentId(1);

        await VerifyThatTicketHasCompleted(roadSegmentId);

        var command = await Store.GetLastCommand<RoadNetworkChangesAccepted>();
        return command.Changes.Single().RoadSegmentAdded.Id == roadSegmentId;
    }
}
