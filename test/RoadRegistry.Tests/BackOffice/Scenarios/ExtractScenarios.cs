namespace RoadRegistry.BackOffice.Scenarios
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;
    using Xunit;
    using AutoFixture;
    using Be.Vlaanderen.Basisregisters.BlobStore;
    using Extracts;
    using KellermanSoftware.CompareNetObjects;
    using Messages;
    using NodaTime.Text;
    using RoadRegistry.Framework.Projections;
    using RoadRegistry.Framework.Testing;

    public class ExtractScenarios: RoadRegistryFixture
    {
        private static ComparisonConfig CreateComparisonConfig()
        {
            var comparisonConfig = new ComparisonConfig
            {
                MaxDifferences = int.MaxValue,
                MaxStructDepth = 5,
                IgnoreCollectionOrder = true
            };

            comparisonConfig.CustomPropertyComparer<RoadNetworkExtractGotRequested>(
                x => x.Contour.Polygon,
                new GeometryPolygonComparer(RootComparerFactory.GetRootComparer()));
            comparisonConfig.CustomPropertyComparer<RoadNetworkExtractGotRequested>(
                x => x.Contour.MultiPolygon,
                new GeometryPolygonComparer(RootComparerFactory.GetRootComparer()));

            return comparisonConfig;
        }

        public ExtractScenarios() : base(CreateComparisonConfig())
        {
            Fixture.CustomizeExternalExtractRequestId();
            Fixture.CustomizeRoadNetworkExtractGeometry();
            Fixture.CustomizeExtractDescription();
            Fixture.CustomizeArchiveId();
        }

        [Fact]
        public Task when_requesting_an_extract_for_the_first_time()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var downloadId = Fixture.Create<DownloadId>();
            var contour = Fixture.Create<RoadNetworkExtractGeometry>();

            return Run(scenario => scenario
                .GivenNone()
                .When(TheExternalSystem.PutsInARoadNetworkExtractRequest(externalExtractRequestId, downloadId, extractDescription, contour))
                .Then(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    Description = extractDescription,
                    Contour = FlattenContour(contour),
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
            );
        }

        private static RoadNetworkExtractGeometry FlattenContour(RoadNetworkExtractGeometry contour)
        {
            return GeometryTranslator.TranslateToRoadNetworkExtractGeometry(GeometryTranslator.Translate(contour));
        }

        [Fact]
        public Task when_requesting_an_extract_again_with_a_different_download_id()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var oldDownloadId = Fixture.Create<DownloadId>();
            var newDownloadId = Fixture.Create<DownloadId>();
            var oldContour = Fixture.Create<RoadNetworkExtractGeometry>();
            var newContour = Fixture.Create<RoadNetworkExtractGeometry>();

            return Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = oldDownloadId,
                    Contour = oldContour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(TheExternalSystem.PutsInARoadNetworkExtractRequest(externalExtractRequestId, newDownloadId, extractDescription, newContour))
                .Then(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = newDownloadId,
                    Contour = FlattenContour(newContour),
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
            );
        }

        [Fact]
        public Task when_requesting_an_extract_again_with_the_same_download_id()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var downloadId = Fixture.Create<DownloadId>();
            var oldContour = Fixture.Create<RoadNetworkExtractGeometry>();
            var newContour = Fixture.Create<RoadNetworkExtractGeometry>();

            return Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    Contour = oldContour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(TheExternalSystem.PutsInARoadNetworkExtractRequest(externalExtractRequestId, downloadId, extractDescription, newContour))
                .ThenNone()
            );
        }

        [Fact]
        public Task when_announcing_a_requested_road_network_extract_download_became_available()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var downloadId = Fixture.Create<DownloadId>();
            var archiveId = Fixture.Create<ArchiveId>();
            var contour = Fixture.Create<RoadNetworkExtractGeometry>();

            return Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    Contour = contour,
                    Description = extractDescription,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(OurSystem.AnnouncesRoadNetworkExtractDownloadBecameAvailable(extractRequestId, downloadId, archiveId))
                .Then(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractDownloadBecameAvailable
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    ArchiveId = archiveId,
                    Description = extractDescription,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
            );
        }

        [Fact]
        public Task when_announcing_an_announced_road_network_extract_download_became_available()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var downloadId = Fixture.Create<DownloadId>();
            var archiveId = Fixture.Create<ArchiveId>();
            var contour = Fixture.Create<RoadNetworkExtractGeometry>();

            return Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    Contour = contour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                }, new RoadNetworkExtractDownloadBecameAvailable
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    ArchiveId = archiveId,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(OurSystem.AnnouncesRoadNetworkExtractDownloadBecameAvailable(extractRequestId, downloadId, archiveId))
                .ThenNone()
            );
        }

        [Fact]
        public async Task when_uploading_an_archive_of_changes_for_an_outdated_download()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var outdatedDownloadId = Fixture.Create<DownloadId>();
            var downloadId = Fixture.Create<DownloadId>();
            var uploadId = Fixture.Create<UploadId>();
            var archiveId = Fixture.Create<ArchiveId>();
            var contour = Fixture.Create<RoadNetworkExtractGeometry>();

            await CreateEmptyArchive(archiveId);

            await Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = outdatedDownloadId,
                    Description = extractDescription,
                    Contour = contour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                }, new RoadNetworkExtractDownloadBecameAvailable
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    ArchiveId = archiveId,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                },
                new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    Contour = contour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(TheExternalSystem.UploadsRoadNetworkExtractChangesArchive(extractRequestId, outdatedDownloadId, uploadId, archiveId))
                .Throws(new CanNotUploadRoadNetworkExtractChangesArchiveForSupersededDownloadException(externalExtractRequestId, extractRequestId, outdatedDownloadId, downloadId, uploadId))
            );
        }

        private async Task CreateEmptyArchive(ArchiveId archiveId)
        {
            using (var stream = new MemoryStream())
            {
                using (new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    // what's the problem?
                }

                stream.Position = 0;
                await Client.CreateBlobAsync(
                    new BlobName(archiveId),
                    Metadata.None,
                    ContentType.Parse("application/zip"),
                    stream);
            }
        }

        private async Task CreateErrorArchive(ArchiveId archiveId)
        {
            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    archive.CreateEntry("error");
                }

                stream.Position = 0;
                await Client.CreateBlobAsync(
                    new BlobName(archiveId),
                    Metadata.None,
                    ContentType.Parse("application/zip"),
                    stream);
            }
        }

        [Fact]
        public async Task when_uploading_an_archive_of_changes_for_an_unknown_download()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var unknownDownloadId = Fixture.Create<DownloadId>();
            var downloadId = Fixture.Create<DownloadId>();
            var uploadId = Fixture.Create<UploadId>();
            var archiveId = Fixture.Create<ArchiveId>();
            var contour = Fixture.Create<RoadNetworkExtractGeometry>();

            await CreateEmptyArchive(archiveId);

            await Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    Description = extractDescription,
                    Contour = contour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                }, new RoadNetworkExtractDownloadBecameAvailable
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    ArchiveId = archiveId,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(TheExternalSystem.UploadsRoadNetworkExtractChangesArchive(extractRequestId, unknownDownloadId, uploadId, archiveId))
                .Throws(new CanNotUploadRoadNetworkExtractChangesArchiveForUnknownDownloadException(externalExtractRequestId, extractRequestId, unknownDownloadId, uploadId))
            );
        }

        [Fact]
        public async Task when_uploading_an_archive_of_changes_which_are_accepted_after_validation()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var downloadId = Fixture.Create<DownloadId>();
            var uploadId = Fixture.Create<UploadId>();
            var archiveId = Fixture.Create<ArchiveId>();
            var contour = Fixture.Create<RoadNetworkExtractGeometry>();

            await CreateEmptyArchive(archiveId);

            await Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    Contour = contour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                }, new RoadNetworkExtractDownloadBecameAvailable
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    ArchiveId = archiveId,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(TheExternalSystem.UploadsRoadNetworkExtractChangesArchive(extractRequestId, downloadId, uploadId, archiveId))
                .Then(RoadNetworkExtracts.ToStreamName(extractRequestId),
                    new RoadNetworkExtractChangesArchiveUploaded
                    {
                        RequestId = extractRequestId,
                        ExternalRequestId = externalExtractRequestId,
                        DownloadId = downloadId,
                        UploadId = uploadId,
                        ArchiveId = archiveId,
                        When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                    },
                    new RoadNetworkExtractChangesArchiveAccepted
                    {
                        RequestId = extractRequestId,
                        ExternalRequestId = externalExtractRequestId,
                        DownloadId = downloadId,
                        UploadId = uploadId,
                        ArchiveId = archiveId,
                        Problems = Array.Empty<FileProblem>(),
                        When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                    })
            );
        }

        [Fact]
        public async Task when_uploading_an_archive_of_changes_which_are_not_accepted_after_validation()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var downloadId = Fixture.Create<DownloadId>();
            var uploadId = Fixture.Create<UploadId>();
            var archiveId = Fixture.Create<ArchiveId>();
            var contour = Fixture.Create<RoadNetworkExtractGeometry>();

            await CreateErrorArchive(archiveId);

            await Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    Description = extractDescription,
                    Contour = contour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                }, new RoadNetworkExtractDownloadBecameAvailable
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    Description = extractDescription,
                    DownloadId = downloadId,
                    ArchiveId = archiveId,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(TheExternalSystem.UploadsRoadNetworkExtractChangesArchive(extractRequestId, downloadId, uploadId, archiveId))
                .Then(RoadNetworkExtracts.ToStreamName(extractRequestId),
                    new RoadNetworkExtractChangesArchiveUploaded
                    {
                        RequestId = extractRequestId,
                        ExternalRequestId = externalExtractRequestId,
                        DownloadId = downloadId,
                        UploadId = uploadId,
                        ArchiveId = archiveId,
                        When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                    },
                    new RoadNetworkExtractChangesArchiveRejected
                    {
                        RequestId = extractRequestId,
                        ExternalRequestId = externalExtractRequestId,
                        DownloadId = downloadId,
                        UploadId = uploadId,
                        ArchiveId = archiveId,
                        Problems = new []
                        {
                            new FileProblem
                            {
                                File = "error",
                                Severity = ProblemSeverity.Error,
                                Reason = "reason",
                                Parameters = Array.Empty<ProblemParameter>()
                            }
                        },
                        When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                    })
            );
        }

        [Fact]
        public async Task when_uploading_an_archive_of_changes_a_second_time()
        {
            var externalExtractRequestId = Fixture.Create<ExternalExtractRequestId>();
            var extractRequestId = ExtractRequestId.FromExternalRequestId(externalExtractRequestId);
            var extractDescription = Fixture.Create<ExtractDescription>();
            var downloadId = Fixture.Create<DownloadId>();
            var uploadId = Fixture.Create<UploadId>();
            var archiveId = Fixture.Create<ArchiveId>();
            var contour = Fixture.Create<RoadNetworkExtractGeometry>();

            await CreateErrorArchive(archiveId);

            await Run(scenario => scenario
                .Given(RoadNetworkExtracts.ToStreamName(extractRequestId), new RoadNetworkExtractGotRequestedV2
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    Description = extractDescription,
                    Contour = contour,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                }, new RoadNetworkExtractDownloadBecameAvailable
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    Description = extractDescription,
                    ArchiveId = archiveId,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                },
                new RoadNetworkExtractChangesArchiveUploaded
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    UploadId = uploadId,
                    ArchiveId = archiveId,
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                },
                new RoadNetworkExtractChangesArchiveAccepted
                {
                    RequestId = extractRequestId,
                    ExternalRequestId = externalExtractRequestId,
                    DownloadId = downloadId,
                    UploadId = uploadId,
                    ArchiveId = archiveId,
                    Problems = Array.Empty<FileProblem>(),
                    When = InstantPattern.ExtendedIso.Format(Clock.GetCurrentInstant())
                })
                .When(TheExternalSystem.UploadsRoadNetworkExtractChangesArchive(extractRequestId, downloadId, uploadId, archiveId))
                .Throws(new CanNotUploadRoadNetworkExtractChangesArchiveForSameDownloadMoreThanOnceException(externalExtractRequestId, extractRequestId, downloadId, uploadId))
            );
        }
    }
}
