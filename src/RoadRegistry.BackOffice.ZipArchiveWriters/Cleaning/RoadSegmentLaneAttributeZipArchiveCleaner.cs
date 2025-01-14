namespace RoadRegistry.BackOffice.ZipArchiveWriters.Cleaning;

using System.IO.Compression;
using System.Text;
using Extracts;
using Extracts.Dbase.RoadSegments;

public class RoadSegmentLaneAttributeZipArchiveCleaner : VersionedZipArchiveCleaner
{
    private static readonly ExtractFileName FileName = ExtractFileName.AttRijstroken;

    public RoadSegmentLaneAttributeZipArchiveCleaner(Encoding encoding)
        : base(
            new ExtractsDbaseZipArchiveCleaner(encoding, FileName),
            new UploadsV2DbaseZipArchiveCleaner(encoding, FileName),
            new UploadsV1DbaseZipArchiveCleaner(encoding, FileName)
        )
    {
    }

    private sealed class ExtractsDbaseZipArchiveCleaner : DbaseZipArchiveCleanerBase<RoadSegmentLaneAttributeDbaseRecord>
    {
        public ExtractsDbaseZipArchiveCleaner(Encoding encoding, ExtractFileName fileName)
            : base(RoadSegmentLaneAttributeDbaseRecord.Schema, encoding, fileName)
        {
        }

        protected override bool FixDataInArchive(ZipArchive archive,
            IReadOnlyCollection<RoadSegmentLaneAttributeDbaseRecord> dbfRecords)
        {
            return archive.UpdateRoadSegmentAttributeMissingFromOrToPositions(dbfRecords,
                Encoding,
                record => record.WS_OIDN.Value,
                record => record.VANPOS.Value,
                (record, value) => record.VANPOS.Value = value,
                record => record.TOTPOS.Value,
                (record, value) => record.TOTPOS.Value = value);
        }
    }

    private sealed class UploadsV2DbaseZipArchiveCleaner : DbaseZipArchiveCleanerBase<Uploads.Dbase.BeforeFeatureCompare.V2.Schema.RoadSegmentLaneAttributeDbaseRecord>
    {
        public UploadsV2DbaseZipArchiveCleaner(Encoding encoding, ExtractFileName fileName)
            : base(Uploads.Dbase.BeforeFeatureCompare.V2.Schema.RoadSegmentLaneAttributeDbaseRecord.Schema, encoding, fileName)
        {
        }

        protected override bool FixDataInArchive(ZipArchive archive,
            IReadOnlyCollection<Uploads.Dbase.BeforeFeatureCompare.V2.Schema.RoadSegmentLaneAttributeDbaseRecord> dbfRecords)
        {
            return archive.UpdateRoadSegmentAttributeMissingFromOrToPositions(dbfRecords,
                Encoding,
                record => record.WS_OIDN.Value,
                record => record.VANPOS.Value,
                (record, value) => record.VANPOS.Value = value,
                record => record.TOTPOS.Value,
                (record, value) => record.TOTPOS.Value = value);
        }
    }

    private sealed class UploadsV1DbaseZipArchiveCleaner : DbaseZipArchiveCleanerBase<Uploads.Dbase.BeforeFeatureCompare.V1.Schema.RoadSegmentLaneAttributeDbaseRecord>
    {
        public UploadsV1DbaseZipArchiveCleaner(Encoding encoding, ExtractFileName fileName)
            : base(Uploads.Dbase.BeforeFeatureCompare.V1.Schema.RoadSegmentLaneAttributeDbaseRecord.Schema, encoding, fileName)
        {
        }

        protected override bool FixDataInArchive(ZipArchive archive,
            IReadOnlyCollection<Uploads.Dbase.BeforeFeatureCompare.V1.Schema.RoadSegmentLaneAttributeDbaseRecord> dbfRecords)
        {
            return archive.UpdateRoadSegmentAttributeMissingFromOrToPositions(dbfRecords,
                Encoding,
                record => record.WS_OIDN.Value,
                record => record.VANPOS.Value,
                (record, value) => record.VANPOS.Value = value,
                record => record.TOTPOS.Value,
                (record, value) => record.TOTPOS.Value = value);
        }
    }
}
