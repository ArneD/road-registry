namespace RoadRegistry.BackOffice.Extracts.Dbase.Organizations.V1;

using Be.Vlaanderen.Basisregisters.Shaperon;

public class OrganizationDbaseRecord : DbaseRecord
{
    public static readonly OrganizationDbaseSchema Schema = new();
    public const string DbaseSchemaVersion = WellKnownDbaseSchemaVersions.V1;

    public OrganizationDbaseRecord()
    {
        ORG = new DbaseString(Schema.ORG);
        LBLORG = new DbaseString(Schema.LBLORG);

        Values = new DbaseFieldValue[]
        {
            ORG,
            LBLORG
        };
    }

    public DbaseString LBLORG { get; }
    public DbaseString ORG { get; }
}
