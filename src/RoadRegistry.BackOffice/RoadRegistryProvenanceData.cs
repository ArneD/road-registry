namespace RoadRegistry.BackOffice;

using Be.Vlaanderen.Basisregisters.GrAr.Provenance;
using NodaTime;

public class RoadRegistryProvenanceData : ProvenanceData
{
    public RoadRegistryProvenanceData(Modification modification = Modification.Unknown) : base(new Provenance(
        SystemClock.Instance.GetCurrentInstant(),
        Application.RoadRegistry,
        new Be.Vlaanderen.Basisregisters.GrAr.Provenance.Reason(string.Empty),
        new Operator(OperatorName.Unknown),
        modification,
        Organisation.Agiv
    ))
    {
    }
}
