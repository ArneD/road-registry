namespace RoadRegistry.BackOffice.Messages;
using System;
using Be.Vlaanderen.Basisregisters.EventHandling;

public class AnnounceRoadNetworkExtractDownloadBecameAvailable : IMessage
{
    public string RequestId { get; set; }
    public Guid DownloadId { get; set; }

    public string ArchiveId { get; set; }
    //TODO: - Extend all road network events with a RoadNetworkRevision
    //      - Store this revision for each change in the editor projections
    //      - Read this revision as part of assembling the archive to download
    //      - Add this revision as a property here and copy over to the event
    //      - Use the property to remember what revision of the road network we handed and to do conflict resolution
}
