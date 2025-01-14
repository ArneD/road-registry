namespace RoadRegistry.Producer.Snapshot.ProjectionHost.GradeSeparatedJunction
{
    using System;
    using Be.Vlaanderen.Basisregisters.GrAr.Contracts.RoadRegistry;
    using Schema;

    public class GradeSeparatedJunctionRecord
    {
        public int Id { get; set; }
        public int LowerRoadSegmentId { get; set; }
        public int UpperRoadSegmentId { get; set; }
        public int TypeId { get; set; }
        public string TypeDutchName { get; set; }

        public Origin Origin { get; set; }
        public DateTimeOffset LastChangedTimestamp { get; set; }
        public bool IsRemoved { get; set; }

        // EF needs this
        private GradeSeparatedJunctionRecord() { }

        public GradeSeparatedJunctionRecord(
            int id,
            int lowerRoadSegmentId,
            int upperRoadSegmentId,
            int typeId,
            string typeDutchName,
            Origin origin,
            DateTimeOffset lastChangedTimestamp)
        {
            Id = id;
            LowerRoadSegmentId = lowerRoadSegmentId;
            UpperRoadSegmentId = upperRoadSegmentId;
            TypeId = typeId;
            TypeDutchName = typeDutchName;

            Origin = origin;
            LastChangedTimestamp = lastChangedTimestamp;
            IsRemoved = false;
        }

        public GradeSeparatedJunctionSnapshot ToContract()
        {
            return new GradeSeparatedJunctionSnapshot(
                Id,
                LowerRoadSegmentId,
                UpperRoadSegmentId,
                TypeId,
                TypeDutchName,
                Origin,
                LastChangedTimestamp,
                IsRemoved
            );
        }
    }
}
