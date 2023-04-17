namespace RoadRegistry.BackOffice.Api.Tests.RoadSegments.WhenCreateOutline;

using Abstractions;
using Fixtures;
using Xunit.Abstractions;

public class WhenCreateOutlineWithInvalidLaneCount : WhenCreateOutlineWithInvalidRequest<WhenCreateOutlineWithInvalidLaneCountFixture>
{
    public WhenCreateOutlineWithInvalidLaneCount(WhenCreateOutlineWithInvalidLaneCountFixture fixture, ITestOutputHelper outputHelper) : base(fixture, outputHelper)
    {
    }

    protected override string ExpectedErrorCode => "AantalRijstrokenGroterDanNul";
    protected override string ExpectedErrorMessagePrefix => "Aantal rijstroken moet groter dan nul zijn.";
}