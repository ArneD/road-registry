namespace RoadRegistry.BackOffice.ZipArchiveWriters.Tests.ForEditor;

using System.IO.Compression;
using System.Text;
using Be.Vlaanderen.Basisregisters.Shaperon;
using Editor.Schema;
using Editor.Schema.RoadSegments;
using Framework.Containers;
using Xunit;
using ZipArchiveWriters.ForEditor;

[Collection(nameof(SqlServerCollection))]
public class RoadSegmentLaneAttributesToZipArchiveWriterTests
{
    private readonly SqlServer _fixture;

    public RoadSegmentLaneAttributesToZipArchiveWriterTests(SqlServer fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    [Fact]
    public Task ArchiveCanNotBeNull()
    {
        var sut = new RoadSegmentLaneAttributesToZipArchiveWriter(_fixture.MemoryStreamManager, Encoding.UTF8);
        return Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.WriteAsync(null, new EditorContext(), default));
    }

    [Fact]
    public Task ContextCanNotBeNull()
    {
        var sut = new RoadSegmentLaneAttributesToZipArchiveWriter(_fixture.MemoryStreamManager, Encoding.UTF8);
        return Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.WriteAsync(new ZipArchive(Stream.Null, ZipArchiveMode.Create, true), null, default));
    }

    [Fact]
    public async Task WithEmptyDatabaseWritesArchiveWithExpectedEntries()
    {
        var sut = new RoadSegmentLaneAttributesToZipArchiveWriter(_fixture.MemoryStreamManager, Encoding.UTF8);

        var db = await _fixture.CreateDatabaseAsync();
        var context = await _fixture.CreateEditorContextAsync(db);
        await context.SaveChangesAsync();

        await new ZipArchiveScenario<EditorContext>(_fixture.MemoryStreamManager, sut)
            .WithContext(context)
            .Assert(readArchive =>
            {
                Assert.Single(readArchive.Entries);
                foreach (var entry in readArchive.Entries)
                    switch (entry.Name)
                    {
                        case "AttRijstroken.dbf":
                            using (var entryStream = entry.Open())
                            using (var reader = new BinaryReader(entryStream, Encoding.UTF8))
                            {
                                Assert.Equal(
                                    new DbaseFileHeader(
                                        DateTime.Now,
                                        DbaseCodePage.Western_European_ANSI,
                                        new DbaseRecordCount(0),
                                        RoadSegmentLaneAttributeDbaseRecord.Schema),
                                    DbaseFileHeader.Read(reader));
                            }

                            break;

                        default:
                            throw new Exception($"File '{entry.Name}' was not expected in this archive.");
                    }
            });
    }
}
