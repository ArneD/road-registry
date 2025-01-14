namespace RoadRegistry.BackOffice.Uploads;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;
using Be.Vlaanderen.Basisregisters.Shaperon;

public class ZipArchiveVersionedDbaseEntryTranslator : IZipArchiveEntryTranslator
{
    private readonly Encoding _encoding;
    private readonly DbaseFileHeaderReadBehavior _readBehavior;
    private readonly IReadOnlyDictionary<DbaseSchema, IZipArchiveEntryTranslator> _versionedTranslators;

    public ZipArchiveVersionedDbaseEntryTranslator(
        Encoding encoding,
        DbaseFileHeaderReadBehavior readBehavior,
        IReadOnlyDictionary<DbaseSchema, IZipArchiveEntryTranslator> versionedTranslators)
    {
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        _readBehavior = readBehavior ?? throw new ArgumentNullException(nameof(readBehavior));
        _versionedTranslators = versionedTranslators ?? throw new ArgumentNullException(nameof(versionedTranslators));
    }

    public TranslatedChanges Translate(ZipArchiveEntry entry, TranslatedChanges changes)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(changes);

        using (var stream = entry.Open())
        using (var reader = new BinaryReader(stream, _encoding))
        {
            var header = DbaseFileHeader.Read(reader, _readBehavior);
            if (_versionedTranslators.TryGetValue(header.Schema, out var translator)) return translator.Translate(entry, changes);

            throw new TranslatorNotFoundException();
        }
    }
}

[Serializable]
public sealed class TranslatorNotFoundException : ApplicationException
{
    public TranslatorNotFoundException()
    {
    }

    private TranslatorNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
