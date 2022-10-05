using Be.Vlaanderen.Basisregisters.EventHandling;

namespace RoadRegistry.BackOffice.Messages;

public class AddRoadSegmentToNationalRoad : IMessage
{
    public int TemporaryAttributeId { get; set; }
    public int SegmentId { get; set; }
    public string Number { get; set; }
}
