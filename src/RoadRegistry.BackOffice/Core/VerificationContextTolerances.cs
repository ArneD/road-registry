namespace RoadRegistry.BackOffice.Core;

using System;

public class VerificationContextTolerances
{
    public static readonly VerificationContextTolerances Default = new (
        DefaultTolerances.DynamicRoadSegmentAttributePositionTolerance,
        DefaultTolerances.MeasurementTolerance,
        DefaultTolerances.GeometryTolerance,
        DefaultTolerances.ClusterTolerance,
        DefaultTolerances.IntersectionBuffer
    );

    public VerificationContextTolerances(
        double dynamicRoadSegmentAttributePositionTolerance,
        double measurementTolerance,
        double geometryTolerance,
        double clusterTolerance,
        double intersectionBuffer
    )
    {
        if (dynamicRoadSegmentAttributePositionTolerance <= 0.0) throw new ArgumentOutOfRangeException(nameof(dynamicRoadSegmentAttributePositionTolerance));
        if (measurementTolerance <= 0.0) throw new ArgumentOutOfRangeException(nameof(measurementTolerance));
        if (geometryTolerance <= 0.0) throw new ArgumentOutOfRangeException(nameof(geometryTolerance));
        if (clusterTolerance <= 0.0) throw new ArgumentOutOfRangeException(nameof(clusterTolerance));
        if (intersectionBuffer <= 0.0) throw new ArgumentOutOfRangeException(nameof(intersectionBuffer));
        DynamicRoadSegmentAttributePositionTolerance = dynamicRoadSegmentAttributePositionTolerance;
        GeometryTolerance = geometryTolerance;
        MeasurementTolerance = measurementTolerance;
        ClusterTolerance = clusterTolerance;
        IntersectionBuffer = intersectionBuffer;
    }

    public double DynamicRoadSegmentAttributePositionTolerance { get; }
    public double GeometryTolerance { get; }
    public double MeasurementTolerance { get; }
    public double ClusterTolerance { get; }
    public double IntersectionBuffer { get; }
}
