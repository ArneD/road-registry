﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using RoadRegistry.Editor.Schema;

#nullable disable

namespace RoadRegistry.Editor.Schema.Migrations
{
    [DbContext(typeof(EditorContext))]
    partial class EditorContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.16")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Be.Vlaanderen.Basisregisters.ProjectionHandling.Runner.ProjectionStates.ProjectionStateItem", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("DesiredState")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("DesiredStateChangedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Position")
                        .HasColumnType("bigint");

                    b.HasKey("Name");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Name"));

                    b.ToTable("ProjectionStates", "RoadRegistryEditorMeta");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.Extracts.Dbase.Organizations.OrganizationRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("DbaseSchemaVersion")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("V1");

                    b.Property<string>("SortableCode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("Id"), false);

                    b.ToTable("Organization", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.Extracts.Dbase.RoadSegments.RoadSegmentEuropeanRoadAttributeRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("RoadSegmentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RoadSegmentId");

                    b.ToTable("RoadSegmentEuropeanRoadAttribute", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.Extracts.Dbase.RoadSegments.RoadSegmentLaneAttributeRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("RoadSegmentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RoadSegmentId");

                    b.ToTable("RoadSegmentLaneAttribute", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.Extracts.Dbase.RoadSegments.RoadSegmentNationalRoadAttributeRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("RoadSegmentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RoadSegmentId");

                    b.ToTable("RoadSegmentNationalRoadAttribute", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.Extracts.Dbase.RoadSegments.RoadSegmentNumberedRoadAttributeRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("RoadSegmentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RoadSegmentId");

                    b.ToTable("RoadSegmentNumberedRoadAttribute", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.Extracts.Dbase.RoadSegments.RoadSegmentSurfaceAttributeRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("RoadSegmentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RoadSegmentId");

                    b.ToTable("RoadSegmentSurfaceAttribute", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.Extracts.Dbase.RoadSegments.RoadSegmentWidthAttributeRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("RoadSegmentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("RoadSegmentId");

                    b.ToTable("RoadSegmentWidthAttribute", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.Metrics.EventProcessorMetricsRecord", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DbContext")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("EditorContext");

                    b.Property<long>("ElapsedMilliseconds")
                        .HasColumnType("bigint");

                    b.Property<string>("EventProcessorId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("FromPosition")
                        .HasColumnType("bigint");

                    b.Property<long>("ToPosition")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.ToTable("EventProcessors", "RoadRegistryEditorMetrics");
                });

            modelBuilder.Entity("RoadRegistry.BackOffice.RoadNetworkInfo", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<bool>("CompletedImport")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<int>("GradeSeparatedJunctionCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("OrganizationCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("RoadNodeCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("RoadSegmentCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("RoadSegmentEuropeanRoadAttributeCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("RoadSegmentLaneAttributeCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("RoadSegmentNationalRoadAttributeCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("RoadSegmentNumberedRoadAttributeCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("RoadSegmentSurfaceAttributeCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("RoadSegmentWidthAttributeCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("TotalRoadNodeShapeLength")
                        .HasColumnType("int");

                    b.Property<int>("TotalRoadSegmentShapeLength")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("Id"), false);

                    b.ToTable("RoadNetworkInfo", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.Extracts.ExtractDownloadRecord", b =>
                {
                    b.Property<Guid>("DownloadId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ArchiveId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Available")
                        .HasColumnType("bit");

                    b.Property<long>("AvailableOn")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("DownloadedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("ExternalRequestId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsInformative")
                        .HasColumnType("bit");

                    b.Property<string>("RequestId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("RequestedOn")
                        .HasColumnType("bigint");

                    b.HasKey("DownloadId");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("DownloadId"), false);

                    b.ToTable("ExtractDownload", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.Extracts.ExtractRequestRecord", b =>
                {
                    b.Property<Guid>("DownloadId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Geometry>("Contour")
                        .IsRequired()
                        .HasColumnType("Geometry");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("DownloadedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("ExternalRequestId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsInformative")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("RequestedOn")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("DownloadId");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("DownloadId"), false);

                    b.ToTable("ExtractRequest", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.Extracts.ExtractUploadRecord", b =>
                {
                    b.Property<Guid>("UploadId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ArchiveId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ChangeRequestId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<long>("CompletedOn")
                        .HasColumnType("bigint");

                    b.Property<Guid>("DownloadId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ExternalRequestId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("ReceivedOn")
                        .HasColumnType("bigint");

                    b.Property<string>("RequestId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.HasKey("UploadId");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("UploadId"), false);

                    b.HasIndex("ChangeRequestId")
                        .IsUnique();

                    b.ToTable("ExtractUpload", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.GradeSeparatedJunctions.GradeSeparatedJunctionRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("LowerRoadSegmentId")
                        .HasColumnType("int");

                    b.Property<int>("UpperRoadSegmentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.ToTable("GradeSeparatedJunction", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.MunicipalityGeometry", b =>
                {
                    b.Property<string>("NisCode")
                        .HasMaxLength(5)
                        .HasColumnType("nchar(5)")
                        .IsFixedLength();

                    b.Property<Geometry>("Geometry")
                        .IsRequired()
                        .HasColumnType("Geometry");

                    b.HasKey("NisCode");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("NisCode"));

                    b.ToTable("MunicipalityGeometry", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadNetworkChanges.RoadNetworkChange", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<string>("Content")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("When")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("Id"), false);

                    b.ToTable("RoadNetworkChange", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadNetworkChanges.RoadNetworkChangeRequestBasedOnArchive", b =>
                {
                    b.Property<byte[]>("ChangeRequestId")
                        .HasMaxLength(32)
                        .HasColumnType("varbinary(32)");

                    b.Property<string>("ArchiveId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ChangeRequestId");

                    b.HasIndex("ChangeRequestId");

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("ChangeRequestId"), false);

                    b.ToTable("RoadNetworkChangeRequestBasedOnArchive", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadNetworkInfoSegmentCache", b =>
                {
                    b.Property<int>("RoadSegmentId")
                        .HasColumnType("int");

                    b.Property<int>("LanesLength")
                        .HasColumnType("int");

                    b.Property<int>("PartOfEuropeanRoadsLength")
                        .HasColumnType("int");

                    b.Property<int>("PartOfNationalRoadsLength")
                        .HasColumnType("int");

                    b.Property<int>("PartOfNumberedRoadsLength")
                        .HasColumnType("int");

                    b.Property<int>("ShapeLength")
                        .HasColumnType("int");

                    b.Property<int>("SurfacesLength")
                        .HasColumnType("int");

                    b.Property<int>("WidthsLength")
                        .HasColumnType("int");

                    b.HasKey("RoadSegmentId");

                    b.HasIndex("RoadSegmentId");

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("RoadSegmentId"), false);

                    b.ToTable("RoadNetworkInfoSegmentCache", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadNodeBoundingBox2D", b =>
                {
                    b.Property<double>("MaximumX")
                        .HasColumnType("float");

                    b.Property<double>("MaximumY")
                        .HasColumnType("float");

                    b.Property<double>("MinimumX")
                        .HasColumnType("float");

                    b.Property<double>("MinimumY")
                        .HasColumnType("float");

                    b.ToTable("RoadNodeBoundingBox");

                    b.ToSqlQuery("SELECT MIN([BoundingBox_MinimumX]) AS MinimumX, MAX([BoundingBox_MaximumX]) AS MaximumX, MIN([BoundingBox_MinimumY]) AS MinimumY, MAX([BoundingBox_MaximumY]) AS MaximumY FROM [RoadRegistryEditor].[RoadNode]");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadNodes.RoadNodeRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<Geometry>("Geometry")
                        .IsRequired()
                        .HasColumnType("Geometry");

                    b.Property<byte[]>("ShapeRecordContent")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("ShapeRecordContentLength")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.ToTable("RoadNode", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadSegmentBoundingBox3D", b =>
                {
                    b.Property<double>("MaximumM")
                        .HasColumnType("float");

                    b.Property<double>("MaximumX")
                        .HasColumnType("float");

                    b.Property<double>("MaximumY")
                        .HasColumnType("float");

                    b.Property<double>("MinimumM")
                        .HasColumnType("float");

                    b.Property<double>("MinimumX")
                        .HasColumnType("float");

                    b.Property<double>("MinimumY")
                        .HasColumnType("float");

                    b.ToTable("RoadSegmentBoundingBox");

                    b.ToSqlQuery("SELECT MIN([BoundingBox_MinimumX]) AS MinimumX, MAX([BoundingBox_MaximumX]) AS MaximumX, MIN([BoundingBox_MinimumY]) AS MinimumY, MAX([BoundingBox_MaximumY]) AS MaximumY, MIN([BoundingBox_MinimumM]) AS MinimumM, MAX([BoundingBox_MaximumM]) AS MaximumM FROM [RoadRegistryEditor].[RoadSegment]");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadSegments.RoadSegmentRecord", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte[]>("DbaseRecord")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("EndNodeId")
                        .HasColumnType("int");

                    b.Property<Geometry>("Geometry")
                        .IsRequired()
                        .HasColumnType("Geometry");

                    b.Property<bool>("IsRemoved")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<string>("LastEventHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("ShapeRecordContent")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<int>("ShapeRecordContentLength")
                        .HasColumnType("int");

                    b.Property<int>("StartNodeId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.HasIndex("IsRemoved");

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("IsRemoved"), false);

                    b.ToTable("RoadSegment", "RoadRegistryEditor");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadNodes.RoadNodeRecord", b =>
                {
                    b.OwnsOne("RoadRegistry.Editor.Schema.RoadNodes.RoadNodeBoundingBox", "BoundingBox", b1 =>
                        {
                            b1.Property<int>("RoadNodeRecordId")
                                .HasColumnType("int");

                            b1.Property<double>("MaximumX")
                                .HasColumnType("float");

                            b1.Property<double>("MaximumY")
                                .HasColumnType("float");

                            b1.Property<double>("MinimumX")
                                .HasColumnType("float");

                            b1.Property<double>("MinimumY")
                                .HasColumnType("float");

                            b1.HasKey("RoadNodeRecordId");

                            b1.ToTable("RoadNode", "RoadRegistryEditor");

                            b1.WithOwner()
                                .HasForeignKey("RoadNodeRecordId");
                        });

                    b.Navigation("BoundingBox");
                });

            modelBuilder.Entity("RoadRegistry.Editor.Schema.RoadSegments.RoadSegmentRecord", b =>
                {
                    b.OwnsOne("RoadRegistry.Editor.Schema.RoadSegments.RoadSegmentBoundingBox", "BoundingBox", b1 =>
                        {
                            b1.Property<int>("RoadSegmentRecordId")
                                .HasColumnType("int");

                            b1.Property<double>("MaximumM")
                                .HasColumnType("float");

                            b1.Property<double>("MaximumX")
                                .HasColumnType("float");

                            b1.Property<double>("MaximumY")
                                .HasColumnType("float");

                            b1.Property<double>("MinimumM")
                                .HasColumnType("float");

                            b1.Property<double>("MinimumX")
                                .HasColumnType("float");

                            b1.Property<double>("MinimumY")
                                .HasColumnType("float");

                            b1.HasKey("RoadSegmentRecordId");

                            b1.ToTable("RoadSegment", "RoadRegistryEditor");

                            b1.WithOwner()
                                .HasForeignKey("RoadSegmentRecordId");
                        });

                    b.Navigation("BoundingBox");
                });
#pragma warning restore 612, 618
        }
    }
}
