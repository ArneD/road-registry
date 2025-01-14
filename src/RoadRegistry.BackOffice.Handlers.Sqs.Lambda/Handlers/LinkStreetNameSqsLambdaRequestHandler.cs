namespace RoadRegistry.BackOffice.Handlers.Sqs.Lambda.Handlers;

using Abstractions;
using Be.Vlaanderen.Basisregisters.Shaperon;
using Be.Vlaanderen.Basisregisters.Sqs.Lambda.Handlers;
using Be.Vlaanderen.Basisregisters.Sqs.Lambda.Infrastructure;
using Be.Vlaanderen.Basisregisters.Sqs.Responses;
using Core;
using Exceptions;
using Extensions;
using Hosts;
using Infrastructure;
using Microsoft.Extensions.Logging;
using Requests;
using TicketingService.Abstractions;
using ModifyRoadSegment = BackOffice.Uploads.ModifyRoadSegment;

public sealed class LinkStreetNameSqsLambdaRequestHandler : SqsLambdaHandler<LinkStreetNameSqsLambdaRequest>
{
    private readonly DistributedStreamStoreLock _distributedStreamStoreLock;
    private readonly IStreetNameCache _streetNameCache;
    private readonly IChangeRoadNetworkDispatcher _changeRoadNetworkDispatcher;

    private static readonly string[] ProposedOrCurrentStreetNameStatuses = new[]
    {
        Syndication.Schema.StreetNameStatus.Current.ToString(),
        Syndication.Schema.StreetNameStatus.Proposed.ToString(),
        StreetNameConsumer.Schema.StreetNameStatus.Current,
        StreetNameConsumer.Schema.StreetNameStatus.Proposed
    };

    public LinkStreetNameSqsLambdaRequestHandler(
        SqsLambdaHandlerOptions options,
        ICustomRetryPolicy retryPolicy,
        ITicketing ticketing,
        IIdempotentCommandHandler idempotentCommandHandler,
        IRoadRegistryContext roadRegistryContext,
        IStreetNameCache streetNameCache,
        IChangeRoadNetworkDispatcher changeRoadNetworkDispatcher,
        DistributedStreamStoreLockOptions distributedStreamStoreLockOptions,
        ILogger<LinkStreetNameSqsLambdaRequestHandler> logger)
        : base(
            options,
            retryPolicy,
            ticketing,
            idempotentCommandHandler,
            roadRegistryContext,
            logger)
    {
        _streetNameCache = streetNameCache;
        _changeRoadNetworkDispatcher = changeRoadNetworkDispatcher;
        _distributedStreamStoreLock = new DistributedStreamStoreLock(distributedStreamStoreLockOptions, RoadNetworks.Stream, Logger);
    }

    protected override async Task<object> InnerHandle(LinkStreetNameSqsLambdaRequest request, CancellationToken cancellationToken)
    {
        await _distributedStreamStoreLock.RetryRunUntilLockAcquiredAsync(async () =>
        {
            await _changeRoadNetworkDispatcher.DispatchAsync(request, "Straatnaam koppelen", async translatedChanges =>
            {
                var problems = Problems.None;

                var roadNetwork = await RoadRegistryContext.RoadNetworks.Get(cancellationToken);
                var roadSegmentId = new RoadSegmentId(request.Request.WegsegmentId);
                var roadSegment = roadNetwork.FindRoadSegment(roadSegmentId);
                if (roadSegment == null)
                {
                    problems += new RoadSegmentNotFound(roadSegmentId);
                    throw new RoadRegistryProblemsException(problems);
                }

                var recordNumber = RecordNumber.Initial;

                var leftStreetNameId = request.Request.LinkerstraatnaamId.GetIdentifierFromPuri();
                var rightStreetNameId = request.Request.RechterstraatnaamId.GetIdentifierFromPuri();

                if (leftStreetNameId > 0 || rightStreetNameId > 0)
                {
                    if (leftStreetNameId > 0)
                    {
                        if (!CrabStreetnameId.IsEmpty(roadSegment.AttributeHash.LeftStreetNameId))
                        {
                            problems += new RoadSegmentStreetNameLeftNotUnlinked(request.Request.WegsegmentId);
                        }
                        else
                        {
                            problems = await ValidateStreetName(leftStreetNameId, problems, cancellationToken);
                        }
                    }

                    if (rightStreetNameId > 0)
                    {
                        if (!CrabStreetnameId.IsEmpty(roadSegment.AttributeHash.RightStreetNameId))
                        {
                            problems = problems.Add(new RoadSegmentStreetNameRightNotUnlinked(request.Request.WegsegmentId));
                        }
                        else
                        {
                            problems = await ValidateStreetName(rightStreetNameId, problems, cancellationToken);
                        }
                    }

                    translatedChanges = translatedChanges.AppendChange(new ModifyRoadSegment(
                        recordNumber,
                        roadSegment.Id,
                        roadSegment.Start,
                        roadSegment.End,
                        roadSegment.AttributeHash.OrganizationId,
                        roadSegment.AttributeHash.GeometryDrawMethod,
                        roadSegment.AttributeHash.Morphology,
                        roadSegment.AttributeHash.Status,
                        roadSegment.AttributeHash.Category,
                        roadSegment.AttributeHash.AccessRestriction,
                        leftStreetNameId > 0 ? new CrabStreetnameId(leftStreetNameId) : roadSegment.AttributeHash.LeftStreetNameId,
                        rightStreetNameId > 0 ? new CrabStreetnameId(rightStreetNameId) : roadSegment.AttributeHash.RightStreetNameId
                    ).WithGeometry(roadSegment.Geometry));
                }

                if (problems.Any())
                {
                    throw new RoadRegistryProblemsException(problems);
                }

                return translatedChanges;
            }, cancellationToken);
        }, cancellationToken);

        var roadSegmentId = request.Request.WegsegmentId;
        var lastHash = await GetRoadSegmentHash(new RoadSegmentId(roadSegmentId), cancellationToken);
        return new ETagResponse(string.Format(DetailUrlFormat, roadSegmentId), lastHash);
    }

    protected override Task ValidateIfMatchHeaderValue(LinkStreetNameSqsLambdaRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<Problems> ValidateStreetName(int streetNameId, Problems problems, CancellationToken cancellationToken)
    {
        var streetNameStatuses = await _streetNameCache.GetStreetNameStatusesById(new[] { streetNameId }, cancellationToken);
        if (!streetNameStatuses.TryGetValue(streetNameId, out var streetNameStatus)
            || streetNameStatus is null)
        {
            return problems.Add(new StreetNameNotFound());
        }
        
        if (ProposedOrCurrentStreetNameStatuses.All(status => !string.Equals(streetNameStatus, status, StringComparison.InvariantCultureIgnoreCase)))
        {
            return problems.Add(new RoadSegmentStreetNameNotProposedOrCurrent());
        }

        return problems;
    }
}
