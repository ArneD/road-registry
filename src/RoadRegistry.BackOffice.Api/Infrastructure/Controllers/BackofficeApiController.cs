namespace RoadRegistry.BackOffice.Api.Infrastructure.Controllers;

using System;
using System.Collections.Generic;
using Be.Vlaanderen.Basisregisters.Api;
using Be.Vlaanderen.Basisregisters.AspNetCore.Mvc.Middleware;
using Be.Vlaanderen.Basisregisters.GrAr.Provenance;
using Be.Vlaanderen.Basisregisters.Sqs.Requests;
using Hosts.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;

public abstract class BackofficeApiController : ApiController
{
    private readonly TicketingOptions _ticketingOptions;

    protected BackofficeApiController()
    {
    }

    protected BackofficeApiController(TicketingOptions ticketingOptions)
    {
        _ticketingOptions = ticketingOptions;
    }

    protected IActionResult Accepted(LocationResult locationResult)
    {
        return Accepted(locationResult
            .Location
            .ToString()
            .Replace(_ticketingOptions.InternalBaseUrl, _ticketingOptions.PublicBaseUrl));
    }

    private ProvenanceData CreateFakeProvenanceData()
    {
        return new RoadRegistryProvenanceData(Modification.Insert);
    }

    protected TRequest Enrich<TRequest>(TRequest request)
        where TRequest : SqsRequest
    {
        request.Metadata = GetMetadata();
        request.ProvenanceData = CreateFakeProvenanceData();
        return request;
    }

    private IDictionary<string, object> GetMetadata()
    {
        var userId = User.FindFirst("urn:be:vlaanderen:roadregistry:acmid")?.Value;
        var correlationId = User.FindFirst(AddCorrelationIdMiddleware.UrnBasisregistersVlaanderenCorrelationId)?.Value;

        return new Dictionary<string, object>
        {
            { "UserId", userId },
            { "CorrelationId", correlationId ?? Guid.NewGuid().ToString() }
        };
    }
}
