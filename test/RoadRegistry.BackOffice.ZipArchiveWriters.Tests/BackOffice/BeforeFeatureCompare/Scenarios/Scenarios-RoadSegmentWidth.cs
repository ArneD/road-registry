namespace RoadRegistry.BackOffice.ZipArchiveWriters.Tests.BackOffice.BeforeFeatureCompare.Scenarios;

using Core.ProblemCodes;
using Exceptions;
using FeatureCompare;
using Microsoft.Extensions.Logging;
using RoadRegistry.Tests.BackOffice;
using Uploads;
using Xunit.Abstractions;

public class RoadSegmentWidthScenarios : FeatureCompareTranslatorScenariosBase
{
    public RoadSegmentWidthScenarios(ITestOutputHelper testOutputHelper, ILogger<ZipArchiveFeatureCompareTranslator> logger)
        : base(testOutputHelper, logger)
    {
    }

    [Fact]
    public async Task NotAdjacentFromToPositionShouldGiveProblem()
    {
        var zipArchive = new ExtractsZipArchiveBuilder()
            .WithChange((builder, context) =>
            {
                var widthDbaseRecord1 = builder.CreateRoadSegmentWidthDbaseRecord();
                widthDbaseRecord1.WS_OIDN.Value = builder.TestData.RoadSegment1DbaseRecord.WS_OIDN.Value;
                widthDbaseRecord1.VANPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Max - 1;
                widthDbaseRecord1.TOTPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Max;
                var widthDbaseRecord2 = builder.CreateRoadSegmentWidthDbaseRecord();
                widthDbaseRecord2.WS_OIDN.Value = builder.TestData.RoadSegment1DbaseRecord.WS_OIDN.Value;
                widthDbaseRecord2.VANPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Min;
                widthDbaseRecord2.TOTPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Min + 1;

                builder.DataSet.WidthDbaseRecords = new[] { widthDbaseRecord1, widthDbaseRecord2, builder.TestData.RoadSegment2WidthDbaseRecord }.ToList();
            })
            .Build();

        var ex = await Assert.ThrowsAsync<ZipArchiveValidationException>(() => TranslateReturnsExpectedResult(zipArchive, TranslatedChanges.Empty));
        var problem = Assert.Single(ex.Problems);
        Assert.Equal(ProblemCode.RoadSegment.Width.NotAdjacent, problem.Reason);
    }

    [Fact]
    public async Task ZeroFromPositionShouldGiveProblem()
    {
        var zipArchive = new ExtractsZipArchiveBuilder()
            .WithChange((builder, context) =>
            {
                builder.TestData.RoadSegment1WidthDbaseRecord.VANPOS.Value = 1;
            })
            .Build();

        var ex = await Assert.ThrowsAsync<ZipArchiveValidationException>(() => TranslateReturnsExpectedResult(zipArchive, TranslatedChanges.Empty));
        var problem = Assert.Single(ex.Problems);
        Assert.Equal(ProblemCode.RoadSegment.Width.FromPositionNotEqualToZero, problem.Reason);
    }
    
    [Fact]
    public async Task EqualFromToPositionShouldGiveProblem()
    {
        var zipArchive = new ExtractsZipArchiveBuilder()
            .WithChange((builder, context) =>
            {
                var widthDbaseRecord1 = builder.CreateRoadSegmentWidthDbaseRecord();
                widthDbaseRecord1.WS_OIDN.Value = builder.TestData.RoadSegment1DbaseRecord.WS_OIDN.Value;
                widthDbaseRecord1.VANPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Min;
                widthDbaseRecord1.TOTPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Min + 1;

                var widthDbaseRecord2 = builder.CreateRoadSegmentWidthDbaseRecord();
                widthDbaseRecord2.WS_OIDN.Value = builder.TestData.RoadSegment1DbaseRecord.WS_OIDN.Value;
                widthDbaseRecord2.VANPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Max - 1;
                widthDbaseRecord2.TOTPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Max - 1;

                var widthDbaseRecord3 = builder.CreateRoadSegmentWidthDbaseRecord();
                widthDbaseRecord3.WS_OIDN.Value = builder.TestData.RoadSegment1DbaseRecord.WS_OIDN.Value;
                widthDbaseRecord3.VANPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Max - 1;
                widthDbaseRecord3.TOTPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Max;

                builder.DataSet.WidthDbaseRecords = new[] { widthDbaseRecord1, widthDbaseRecord2, widthDbaseRecord3, builder.TestData.RoadSegment2WidthDbaseRecord }.ToList();
            })
            .Build();

        var ex = await Assert.ThrowsAsync<ZipArchiveValidationException>(() => TranslateReturnsExpectedResult(zipArchive, TranslatedChanges.Empty));
        Assert.Contains(ex.Problems, problem => problem.Reason == ProblemCode.RoadSegment.Width.HasLengthOfZero);
    }

    [Fact]
    public async Task ToPositionDifferentThanSegmentLengthShouldGiveProblem()
    {
        var zipArchive = new ExtractsZipArchiveBuilder()
            .WithChange((builder, context) =>
            {
                builder.TestData.RoadSegment1WidthDbaseRecord.TOTPOS.Value = builder.TestData.RoadSegment1ShapeRecord.Shape.MeasureRange.Max - 1;
            })
            .Build();

        var ex = await Assert.ThrowsAsync<ZipArchiveValidationException>(() => TranslateReturnsExpectedResult(zipArchive, TranslatedChanges.Empty));
        var problem = Assert.Single(ex.Problems);
        Assert.Equal(ProblemCode.RoadSegment.Width.ToPositionNotEqualToLength, problem.Reason);
    }

    [Fact]
    public async Task RoadSegmentsWithoutWidthAttributesShouldGiveProblem()
    {
        var zipArchive = new ExtractsZipArchiveBuilder()
            .WithChange((builder, context) =>
            {
                builder.DataSet.WidthDbaseRecords.RemoveAt(1);
            })
            .Build();

        var ex = await Assert.ThrowsAsync<ZipArchiveValidationException>(() => TranslateReturnsExpectedResult(zipArchive, TranslatedChanges.Empty));
        var problem = Assert.Single(ex.Problems);
        Assert.Equal(nameof(DbaseFileProblems.RoadSegmentsWithoutWidthAttributes), problem.Reason);
    }

    [Fact]
    public async Task UsingUnknownRoadSegmentShouldGiveProblem()
    {
        var zipArchive = new ExtractsZipArchiveBuilder()
            .WithChange((builder, context) =>
            {
                var laneDbaseRecord3 = builder.CreateRoadSegmentWidthDbaseRecord();
                laneDbaseRecord3.WS_OIDN.Value = context.Fixture.CreateWhichIsDifferentThan(new RoadSegmentId(builder.TestData.RoadSegment1DbaseRecord.WS_OIDN.Value), new RoadSegmentId(builder.TestData.RoadSegment2DbaseRecord.WS_OIDN.Value));
                laneDbaseRecord3.VANPOS.Value = builder.TestData.RoadSegment2ShapeRecord.Shape.MeasureRange.Min;
                laneDbaseRecord3.TOTPOS.Value = builder.TestData.RoadSegment2ShapeRecord.Shape.MeasureRange.Max;

                builder.DataSet.WidthDbaseRecords = new[] { builder.TestData.RoadSegment1WidthDbaseRecord, builder.TestData.RoadSegment2WidthDbaseRecord, laneDbaseRecord3 }.ToList();
            })
            .Build();

        var ex = await Assert.ThrowsAsync<ZipArchiveValidationException>(() => TranslateReturnsExpectedResult(zipArchive, TranslatedChanges.Empty));
        var problem = Assert.Single(ex.Problems);
        Assert.Equal(ProblemCode.RoadSegment.Missing, problem.Reason);
    }
}
