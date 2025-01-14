namespace RoadRegistry.BackOffice.Api.Tests.RoadSegments.WhenCreateOutline.Fixtures;

using Api.RoadSegments;
using Editor.Schema;
using MediatR;

public class WhenCreateOutlineWithInvalidLaneDirectionFixture : WhenCreateOutlineWithValidRequestFixture
{
    public WhenCreateOutlineWithInvalidLaneDirectionFixture(IMediator mediator, EditorContext editorContext) : base(mediator, editorContext)
    {
    }

    protected override PostRoadSegmentOutlineParameters CreateRequest()
    {
        var request = base.CreateRequest();
        request.AantalRijstroken.Richting = "";
        return request;
    }
}