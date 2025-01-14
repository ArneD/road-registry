namespace RoadRegistry.BackOffice.Uploads;

using System.IO.Compression;
using Be.Vlaanderen.Basisregisters.Shaperon;

public interface IZipArchiveDbaseRecordsValidator<TDbaseRecord>
    where TDbaseRecord : DbaseRecord, new()
{
    (ZipArchiveProblems, ZipArchiveValidationContext) Validate(ZipArchiveEntry entry, IDbaseRecordEnumerator<TDbaseRecord> records, ZipArchiveValidationContext context);
}