namespace RoadRegistry.Editor.Projections;

using BackOffice;
using BackOffice.Extracts.Dbase.RoadSegments;
using BackOffice.Messages;
using Be.Vlaanderen.Basisregisters.GrAr.Common;
using Be.Vlaanderen.Basisregisters.ProjectionHandling.Connector;
using Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore;
using Be.Vlaanderen.Basisregisters.Shaperon;
using Microsoft.IO;
using RoadRegistry.BackOffice.Core;
using RoadRegistry.BackOffice.Extensions;
using Schema;
using Schema.RoadSegments;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeometryTranslator = Be.Vlaanderen.Basisregisters.Shaperon.Geometries.GeometryTranslator;

public class RoadSegmentRecordProjection : ConnectedProjection<EditorContext>
{
    public RoadSegmentRecordProjection(RecyclableMemoryStreamManager manager,
        Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(encoding);

        When<Envelope<ImportedRoadSegment>>(async (context, envelope, token) =>
        {
            var geometry =
                GeometryTranslator.FromGeometryMultiLineString(BackOffice.GeometryTranslator.Translate(envelope.Message.Geometry));
            var polyLineMShapeContent = new PolyLineMShapeContent(geometry);
            var statusTranslation = RoadSegmentStatus.Parse(envelope.Message.Status).Translation;
            var morphologyTranslation = RoadSegmentMorphology.Parse(envelope.Message.Morphology).Translation;
            var categoryTranslation = RoadSegmentCategory.Parse(envelope.Message.Category).Translation;
            var geometryDrawMethodTranslation = RoadSegmentGeometryDrawMethod.Parse(envelope.Message.GeometryDrawMethod).Translation;
            var accessRestrictionTranslation = RoadSegmentAccessRestriction.Parse(envelope.Message.AccessRestriction).Translation;
            await context.RoadSegments.AddAsync(
                UpdateHash(new RoadSegmentRecord
                {
                    Id = envelope.Message.Id,
                    StartNodeId = envelope.Message.StartNodeId,
                    EndNodeId = envelope.Message.EndNodeId,
                    ShapeRecordContent = polyLineMShapeContent.ToBytes(manager, encoding),
                    ShapeRecordContentLength = polyLineMShapeContent.ContentLength.ToInt32(),
                    BoundingBox = RoadSegmentBoundingBox.From(polyLineMShapeContent.Shape),
                    Geometry = BackOffice.GeometryTranslator.Translate(envelope.Message.Geometry),
                    DbaseRecord = new RoadSegmentDbaseRecord
                    {
                        WS_OIDN = { Value = envelope.Message.Id },
                        WS_UIDN = { Value = $"{envelope.Message.Id}_{envelope.Message.Version}" },
                        WS_GIDN = { Value = $"{envelope.Message.Id}_{envelope.Message.GeometryVersion}" },
                        B_WK_OIDN = { Value = envelope.Message.StartNodeId },
                        E_WK_OIDN = { Value = envelope.Message.EndNodeId },
                        STATUS = { Value = statusTranslation.Identifier },
                        LBLSTATUS = { Value = statusTranslation.Name },
                        MORF = { Value = morphologyTranslation.Identifier },
                        LBLMORF = { Value = morphologyTranslation.Name },
                        WEGCAT = { Value = categoryTranslation.Identifier },
                        LBLWEGCAT = { Value = categoryTranslation.Name },
                        LSTRNMID = { Value = envelope.Message.LeftSide.StreetNameId },
                        LSTRNM = { Value = envelope.Message.LeftSide.StreetName },
                        RSTRNMID = { Value = envelope.Message.RightSide.StreetNameId },
                        RSTRNM = { Value = envelope.Message.RightSide.StreetName },
                        BEHEER = { Value = envelope.Message.MaintenanceAuthority.Code },
                        LBLBEHEER = { Value = envelope.Message.MaintenanceAuthority.Name },
                        METHODE = { Value = geometryDrawMethodTranslation.Identifier },
                        LBLMETHOD = { Value = geometryDrawMethodTranslation.Name },
                        OPNDATUM = { Value = envelope.Message.RecordingDate },
                        BEGINTIJD = { Value = envelope.Message.Origin.Since },
                        BEGINORG = { Value = envelope.Message.Origin.OrganizationId },
                        LBLBGNORG = { Value = envelope.Message.Origin.Organization },
                        TGBEP = { Value = accessRestrictionTranslation.Identifier },
                        LBLTGBEP = { Value = accessRestrictionTranslation.Name }
                    }.ToBytes(manager, encoding)
                }, envelope.Message),
                token);
        });

        When<Envelope<RoadNetworkChangesAccepted>>(async (context, envelope, token) =>
        {
            foreach (var message in envelope.Message.Changes.Flatten())
                switch (message)
                {
                    case RoadSegmentAdded roadSegmentAdded:
                        await AddRoadSegment(manager, encoding, context, roadSegmentAdded, envelope, token);
                        break;

                    case RoadSegmentModified roadSegmentModified:
                        await ModifyRoadSegment(manager, encoding, context, roadSegmentModified, envelope, token);
                        break;

                    case RoadSegmentAttributesModified roadSegmentAttributesModified:
                        await ModifyRoadSegmentAttributes(manager, encoding, context, roadSegmentAttributesModified, envelope, token);
                        break;

                    case RoadSegmentGeometryModified roadSegmentGeometryModified:
                        await ModifyRoadSegmentGeometry(manager, encoding, context, roadSegmentGeometryModified, envelope, token);
                        break;

                    case RoadSegmentRemoved roadSegmentRemoved:
                        await RemoveRoadSegment(manager, encoding, context, roadSegmentRemoved, envelope, token);
                        break;
                }
        });
    }

    private static async Task AddRoadSegment(RecyclableMemoryStreamManager manager,
        Encoding encoding,
        EditorContext context,
        RoadSegmentAdded segment,
        Envelope<RoadNetworkChangesAccepted> envelope,
        CancellationToken token)
    {
        var geometry = GeometryTranslator.FromGeometryMultiLineString(BackOffice.GeometryTranslator.Translate(segment.Geometry));
        var polyLineMShapeContent = new PolyLineMShapeContent(geometry);
        var statusTranslation = RoadSegmentStatus.Parse(segment.Status).Translation;
        var morphologyTranslation = RoadSegmentMorphology.Parse(segment.Morphology).Translation;
        var categoryTranslation = RoadSegmentCategory.Parse(segment.Category).Translation;
        var geometryDrawMethodTranslation = RoadSegmentGeometryDrawMethod.Parse(segment.GeometryDrawMethod).Translation;
        var accessRestrictionTranslation = RoadSegmentAccessRestriction.Parse(segment.AccessRestriction).Translation;

        await context.RoadSegments.AddAsync(
            UpdateHash(new RoadSegmentRecord
            {
                Id = segment.Id,
                StartNodeId = segment.StartNodeId,
                EndNodeId = segment.EndNodeId,
                ShapeRecordContent = polyLineMShapeContent.ToBytes(manager, encoding),
                ShapeRecordContentLength = polyLineMShapeContent.ContentLength.ToInt32(),
                BoundingBox = RoadSegmentBoundingBox.From(polyLineMShapeContent.Shape),
                Geometry = BackOffice.GeometryTranslator.Translate(segment.Geometry),
                DbaseRecord = UpdateBeginFields(new RoadSegmentDbaseRecord
                {
                    WS_OIDN = { Value = segment.Id },
                    WS_UIDN = { Value = $"{segment.Id}_{segment.Version}" },
                    WS_GIDN = { Value = $"{segment.Id}_{segment.GeometryVersion}" },
                    B_WK_OIDN = { Value = segment.StartNodeId },
                    E_WK_OIDN = { Value = segment.EndNodeId },
                    STATUS = { Value = statusTranslation.Identifier },
                    LBLSTATUS = { Value = statusTranslation.Name },
                    MORF = { Value = morphologyTranslation.Identifier },
                    LBLMORF = { Value = morphologyTranslation.Name },
                    WEGCAT = { Value = categoryTranslation.Identifier },
                    LBLWEGCAT = { Value = categoryTranslation.Name },
                    LSTRNMID = { Value = segment.LeftSide.StreetNameId },
                    LSTRNM = { Value = null }, // This value is fetched from cache when downloading (see RoadSegmentsToZipArchiveWriter)
                    RSTRNMID = { Value = segment.RightSide.StreetNameId },
                    RSTRNM = { Value = null }, // This value is fetched from cache when downloading (see RoadSegmentsToZipArchiveWriter)
                    BEHEER = { Value = segment.MaintenanceAuthority.Code },
                    LBLBEHEER = { Value = segment.MaintenanceAuthority.Name },
                    METHODE = { Value = geometryDrawMethodTranslation.Identifier },
                    LBLMETHOD = { Value = geometryDrawMethodTranslation.Name },
                    OPNDATUM = { Value = LocalDateTimeTranslator.TranslateFromWhen(envelope.Message.When) },
                    TGBEP = { Value = accessRestrictionTranslation.Identifier },
                    LBLTGBEP = { Value = accessRestrictionTranslation.Name }
                }, envelope).ToBytes(manager, encoding)
            }, segment),
            token);
    }

    private static async Task ModifyRoadSegment(RecyclableMemoryStreamManager manager,
        Encoding encoding,
        EditorContext context,
        RoadSegmentModified roadSegmentModified,
        Envelope<RoadNetworkChangesAccepted> envelope,
        CancellationToken token)
    {
        var geometry = GeometryTranslator.FromGeometryMultiLineString(BackOffice.GeometryTranslator.Translate(roadSegmentModified.Geometry));
        var polyLineMShapeContent = new PolyLineMShapeContent(geometry);
        var statusTranslation = RoadSegmentStatus.Parse(roadSegmentModified.Status).Translation;
        var morphologyTranslation = RoadSegmentMorphology.Parse(roadSegmentModified.Morphology).Translation;
        var categoryTranslation = RoadSegmentCategory.Parse(roadSegmentModified.Category).Translation;
        var geometryDrawMethodTranslation =
            RoadSegmentGeometryDrawMethod.Parse(roadSegmentModified.GeometryDrawMethod).Translation;
        var accessRestrictionTranslation =
            RoadSegmentAccessRestriction.Parse(roadSegmentModified.AccessRestriction).Translation;

        var roadSegmentRecord = await context.RoadSegments.FindAsync(roadSegmentModified.Id, cancellationToken: token).ConfigureAwait(false);
        if (roadSegmentRecord == null)
        {
            throw new InvalidOperationException($"RoadSegmentRecord with id {roadSegmentModified.Id} is not found");
        }

        roadSegmentRecord.Id = roadSegmentModified.Id;
        roadSegmentRecord.StartNodeId = roadSegmentModified.StartNodeId;
        roadSegmentRecord.EndNodeId = roadSegmentModified.EndNodeId;
        roadSegmentRecord.ShapeRecordContent = polyLineMShapeContent.ToBytes(manager, encoding);
        roadSegmentRecord.ShapeRecordContentLength = polyLineMShapeContent.ContentLength.ToInt32();
        roadSegmentRecord.BoundingBox = RoadSegmentBoundingBox.From(polyLineMShapeContent.Shape);
        roadSegmentRecord.Geometry = BackOffice.GeometryTranslator.Translate(roadSegmentModified.Geometry);

        var dbaseRecord = new RoadSegmentDbaseRecord().FromBytes(roadSegmentRecord.DbaseRecord, manager, encoding);
        dbaseRecord.WS_UIDN.Value = new UIDN(roadSegmentModified.Id, roadSegmentModified.Version);
        dbaseRecord.WS_GIDN.Value = new UIDN(roadSegmentModified.Id, roadSegmentModified.GeometryVersion);
        dbaseRecord.B_WK_OIDN.Value = roadSegmentModified.StartNodeId;
        dbaseRecord.E_WK_OIDN.Value = roadSegmentModified.EndNodeId;
        dbaseRecord.STATUS.Value = statusTranslation.Identifier;
        dbaseRecord.LBLSTATUS.Value = statusTranslation.Name;
        dbaseRecord.MORF.Value = morphologyTranslation.Identifier;
        dbaseRecord.LBLMORF.Value = morphologyTranslation.Name;
        dbaseRecord.WEGCAT.Value = categoryTranslation.Identifier;
        dbaseRecord.LBLWEGCAT.Value = categoryTranslation.Name;
        dbaseRecord.LSTRNMID.Value = roadSegmentModified.LeftSide.StreetNameId;
        dbaseRecord.LSTRNM.Value = null; // This value is fetched from cache when downloading (see RoadSegmentsToZipArchiveWriter)
        dbaseRecord.RSTRNMID.Value = roadSegmentModified.RightSide.StreetNameId;
        dbaseRecord.RSTRNM.Value = null; // This value is fetched from cache when downloading (see RoadSegmentsToZipArchiveWriter)
        dbaseRecord.BEHEER.Value = roadSegmentModified.MaintenanceAuthority.Code;
        dbaseRecord.LBLBEHEER.Value = roadSegmentModified.MaintenanceAuthority.Name.NullIfEmpty() ?? Organization.PredefinedTranslations.Unknown.Name;
        dbaseRecord.METHODE.Value = geometryDrawMethodTranslation.Identifier;
        dbaseRecord.LBLMETHOD.Value = geometryDrawMethodTranslation.Name;
        // dbaseRecord.OPNDATUM.Value remains unchanged upon modification
        dbaseRecord.TGBEP.Value = accessRestrictionTranslation.Identifier;
        dbaseRecord.LBLTGBEP.Value = accessRestrictionTranslation.Name;
        UpdateBeginFields(dbaseRecord, envelope);

        roadSegmentRecord.DbaseRecord = dbaseRecord.ToBytes(manager, encoding);

        UpdateHash(roadSegmentRecord, roadSegmentModified);
    }

    private static async Task ModifyRoadSegmentAttributes(RecyclableMemoryStreamManager manager,
        Encoding encoding,
        EditorContext context,
        RoadSegmentAttributesModified roadSegmentAttributesModified,
        Envelope<RoadNetworkChangesAccepted> envelope,
        CancellationToken token)
    {
        var roadSegmentRecord = await context.RoadSegments.FindAsync(roadSegmentAttributesModified.Id, cancellationToken: token).ConfigureAwait(false);
        if (roadSegmentRecord == null)
        {
            throw new InvalidOperationException($"RoadSegmentRecord with id {roadSegmentAttributesModified.Id} is not found");
        }

        var dbaseRecord = new RoadSegmentDbaseRecord().FromBytes(roadSegmentRecord.DbaseRecord, manager, encoding);

        if (roadSegmentAttributesModified.Status is not null)
        {
            var statusTranslation = RoadSegmentStatus.Parse(roadSegmentAttributesModified.Status).Translation;

            dbaseRecord.STATUS.Value = statusTranslation.Identifier;
            dbaseRecord.LBLSTATUS.Value = statusTranslation.Name;
        }

        if (roadSegmentAttributesModified.Morphology is not null)
        {
            var morphologyTranslation = RoadSegmentMorphology.Parse(roadSegmentAttributesModified.Morphology).Translation;

            dbaseRecord.MORF.Value = morphologyTranslation.Identifier;
            dbaseRecord.LBLMORF.Value = morphologyTranslation.Name;
        }

        if (roadSegmentAttributesModified.Category is not null)
        {
            var categoryTranslation = RoadSegmentCategory.Parse(roadSegmentAttributesModified.Category).Translation;

            dbaseRecord.WEGCAT.Value = categoryTranslation.Identifier;
            dbaseRecord.LBLWEGCAT.Value = categoryTranslation.Name;
        }

        if (roadSegmentAttributesModified.AccessRestriction is not null)
        {
            var accessRestrictionTranslation = RoadSegmentAccessRestriction.Parse(roadSegmentAttributesModified.AccessRestriction).Translation;

            dbaseRecord.TGBEP.Value = accessRestrictionTranslation.Identifier;
            dbaseRecord.LBLTGBEP.Value = accessRestrictionTranslation.Name;
        }

        if (roadSegmentAttributesModified.MaintenanceAuthority is not null)
        {
            dbaseRecord.BEHEER.Value = roadSegmentAttributesModified.MaintenanceAuthority.Code;
            dbaseRecord.LBLBEHEER.Value = roadSegmentAttributesModified.MaintenanceAuthority.Name.NullIfEmpty() ?? Organization.PredefinedTranslations.Unknown.Name;
        }

        dbaseRecord.WS_UIDN.Value = new UIDN(roadSegmentAttributesModified.Id, roadSegmentAttributesModified.Version);
        // dbaseRecord.OPNDATUM.Value remains unchanged upon modification
        UpdateBeginFields(dbaseRecord, envelope);

        roadSegmentRecord.DbaseRecord = dbaseRecord.ToBytes(manager, encoding);

        UpdateHash(roadSegmentRecord, roadSegmentAttributesModified);
    }

    private static async Task ModifyRoadSegmentGeometry(RecyclableMemoryStreamManager manager,
        Encoding encoding,
        EditorContext context,
        RoadSegmentGeometryModified roadSegmentGeometryModified,
        Envelope<RoadNetworkChangesAccepted> envelope,
        CancellationToken token)
    {
        var roadSegmentRecord = await context.RoadSegments.FindAsync(roadSegmentGeometryModified.Id, cancellationToken: token).ConfigureAwait(false);
        if (roadSegmentRecord == null)
        {
            throw new InvalidOperationException($"RoadSegmentRecord with id {roadSegmentGeometryModified.Id} is not found");
        }

        var dbaseRecord = new RoadSegmentDbaseRecord().FromBytes(roadSegmentRecord.DbaseRecord, manager, encoding);

        var geometry = GeometryTranslator.FromGeometryMultiLineString(BackOffice.GeometryTranslator.Translate(roadSegmentGeometryModified.Geometry));
        var polyLineMShapeContent = new PolyLineMShapeContent(geometry);

        roadSegmentRecord.ShapeRecordContent = polyLineMShapeContent.ToBytes(manager, encoding);
        roadSegmentRecord.ShapeRecordContentLength = polyLineMShapeContent.ContentLength.ToInt32();
        roadSegmentRecord.BoundingBox = RoadSegmentBoundingBox.From(polyLineMShapeContent.Shape);
        roadSegmentRecord.Geometry = BackOffice.GeometryTranslator.Translate(roadSegmentGeometryModified.Geometry);

        dbaseRecord.WS_GIDN.Value = new UIDN(roadSegmentGeometryModified.Id, roadSegmentGeometryModified.GeometryVersion);

        dbaseRecord.WS_UIDN.Value = new UIDN(roadSegmentGeometryModified.Id, roadSegmentGeometryModified.Version);
        // dbaseRecord.OPNDATUM.Value remains unchanged upon modification
        UpdateBeginFields(dbaseRecord, envelope);

        roadSegmentRecord.DbaseRecord = dbaseRecord.ToBytes(manager, encoding);

        UpdateHash(roadSegmentRecord, roadSegmentGeometryModified);
    }

    private static async Task RemoveRoadSegment(RecyclableMemoryStreamManager manager,
        Encoding encoding,
        EditorContext context,
        RoadSegmentRemoved roadSegmentRemoved,
        Envelope<RoadNetworkChangesAccepted> envelope,
        CancellationToken token)
    {
        var roadSegmentRecord = await context.RoadSegments.FindAsync(roadSegmentRemoved.Id, cancellationToken: token).ConfigureAwait(false);
        
        if (roadSegmentRecord is not null && !roadSegmentRecord.IsRemoved)
        {
            var dbaseRecord = new RoadSegmentDbaseRecord().FromBytes(roadSegmentRecord.DbaseRecord, manager, encoding);

            UpdateBeginFields(dbaseRecord, envelope);
            roadSegmentRecord.DbaseRecord = dbaseRecord.ToBytes(manager, encoding);
            roadSegmentRecord.IsRemoved = true;

            UpdateHash(roadSegmentRecord, roadSegmentRemoved);
        }
    }

    private static RoadSegmentRecord UpdateHash<T>(RoadSegmentRecord entity, T message) where T : IHaveHash
    {
        entity.LastEventHash = message.GetHash();
        return entity;
    }

    private static RoadSegmentDbaseRecord UpdateBeginFields(RoadSegmentDbaseRecord record, Envelope<RoadNetworkChangesAccepted> envelope)
    {
        record.BEGINTIJD.Value = LocalDateTimeTranslator.TranslateFromWhen(envelope.Message.When);
        record.BEGINORG.Value = envelope.Message.OrganizationId;
        record.LBLBGNORG.Value = envelope.Message.Organization;
        return record;
    }
}
