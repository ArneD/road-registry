namespace RoadRegistry.BackOffice.FeatureCompare;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Translators;
using Uploads;

public interface IZipArchiveFeatureCompareTranslator : IZipArchiveTranslatorAsync
{
}

public class ZipArchiveFeatureCompareTranslator : IZipArchiveFeatureCompareTranslator
{
    private readonly ILogger _logger;
    private readonly IReadOnlyCollection<IZipArchiveEntryFeatureCompareTranslator> _translators;

    public ZipArchiveFeatureCompareTranslator(Encoding encoding, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        _logger = logger.ThrowIfNull();

        _translators = new IZipArchiveEntryFeatureCompareTranslator[]
        {
            new TransactionZoneFeatureCompareTranslator(encoding),
            new RoadNodeFeatureCompareTranslator(encoding),
            new RoadSegmentFeatureCompareTranslator(encoding),
            new RoadSegmentLaneFeatureCompareTranslator(encoding),
            new RoadSegmentWidthFeatureCompareTranslator(encoding),
            new RoadSegmentSurfaceFeatureCompareTranslator(encoding),
            new EuropeanRoadFeatureCompareTranslator(encoding),
            new NationalRoadFeatureCompareTranslator(encoding),
            new NumberedRoadFeatureCompareTranslator(encoding),
            new GradeSeparatedJunctionFeatureCompareTranslator(encoding)
        };
    }

    public async Task<TranslatedChanges> Translate(ZipArchive archive, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archive);

        var changes = TranslatedChanges.Empty;

        var roadSegments = new List<RoadSegmentRecord>();

        foreach (var translator in _translators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();
            _logger.LogInformation("{Type} started...", translator.GetType().Name);
            var context = new ZipArchiveEntryFeatureCompareTranslateContext(archive.Entries, roadSegments);
            changes = await translator.TranslateAsync(context, changes, cancellationToken);
            _logger.LogInformation("{Type} completed in {Elapsed}", translator.GetType().Name, sw.Elapsed);
        }

        return changes;
    }
}