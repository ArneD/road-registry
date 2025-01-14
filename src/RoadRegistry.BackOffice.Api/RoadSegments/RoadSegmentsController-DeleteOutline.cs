namespace RoadRegistry.BackOffice.Api.RoadSegments;

using System.Threading;
using System.Threading.Tasks;
using Abstractions.Extensions;
using Abstractions.RoadSegmentsOutline;
using Be.Vlaanderen.Basisregisters.AcmIdm;
using Be.Vlaanderen.Basisregisters.Api.Exceptions;
using Be.Vlaanderen.Basisregisters.Sqs.Exceptions;
using FeatureToggles;
using Handlers.Sqs.RoadSegments;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

public partial class RoadSegmentsController
{
    private const string DeleteOutlineRoute = "{id}/acties/verwijderen/schets";

    /// <summary>
    ///     Verwijder een ingeschetst wegsegment.
    /// </summary>
    /// <param name="featureToggle"></param>
    /// <param name="idValidator"></param>
    /// <param name="id">Identificator van het ingeschetst wegsegment.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="202">Als het ingeschetst wegsegment gevonden is.</response>
    /// <response code="400">Als uw verzoek foutieve data bevat.</response>
    /// <response code="404">Als het wegsegment niet gevonden kan worden.</response>
    /// <response code="500">Als er een interne fout is opgetreden.</response>
    [HttpPost(DeleteOutlineRoute, Name = nameof(DeleteOutline))]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.AllBearerSchemes, Policy = PolicyNames.GeschetsteWeg.Beheerder)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseHeader(StatusCodes.Status202Accepted, "ETag", "string", "De ETag van de response.")]
    [SwaggerResponseHeader(StatusCodes.Status202Accepted, "x-correlation-id", "string", "Correlatie identificator van de response.")]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestResponseExamples))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(RoadSegmentNotFoundResponseExamples))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples))]
    [SwaggerOperation(OperationId = nameof(DeleteOutline), Description = "Verwijder een wegsegment met geometriemethode <ingeschetst>.")]
    public async Task<IActionResult> DeleteOutline(
        [FromServices] UseRoadSegmentOutlineDeleteFeatureToggle featureToggle,
        [FromServices] RoadSegmentOutlinedIdValidator idValidator,
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        if (!featureToggle.FeatureEnabled)
        {
            return NotFound();
        }

        try
        {
            await idValidator.ValidateRoadSegmentIdAndThrowAsync(id, cancellationToken);

            var result = await _mediator.Send(Enrich(
                new DeleteRoadSegmentOutlineSqsRequest
                {
                    Request = new DeleteRoadSegmentOutlineRequest(new RoadSegmentId(id))
                }
            ), cancellationToken);

            return Accepted(result);
        }
        catch (IdempotencyException)
        {
            return Accepted();
        }
    }
}
