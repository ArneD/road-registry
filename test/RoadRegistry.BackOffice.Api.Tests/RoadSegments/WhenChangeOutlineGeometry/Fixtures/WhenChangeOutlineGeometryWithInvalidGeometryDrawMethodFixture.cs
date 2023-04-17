namespace RoadRegistry.BackOffice.Api.Tests.RoadSegments.WhenChangeOutlineGeometry.Fixtures;

using MediatR;
using RoadRegistry.Editor.Schema;
using RoadRegistry.Tests.BackOffice;
using RoadRegistry.Tests.BackOffice.Scenarios;

public class WhenChangeOutlineGeometryWithInvalidGeometryDrawMethodFixture : WhenChangeOutlineGeometryWithValidRequestFixture
{
    public WhenChangeOutlineGeometryWithInvalidGeometryDrawMethodFixture(IMediator mediator, EditorContext editorContext)
        : base(mediator, editorContext)
    {
        TestData = new RoadNetworkTestData(fixture =>
        {
            fixture.CustomizeRoadSegmentGeometryDrawMethod();
        }).CopyCustomizationsTo(ObjectProvider);
    }
}