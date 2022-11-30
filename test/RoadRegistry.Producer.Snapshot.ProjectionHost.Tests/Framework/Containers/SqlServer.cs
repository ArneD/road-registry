namespace RoadRegistry.Producer.Snapshot.ProjectionHost.Tests.Framework.Containers;

using BackOffice.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IO;
using RoadRegistry.Tests.Framework.Containers;
using Schema;

public class SqlServer : ISqlServerDatabase
{
    private readonly ISqlServerDatabase _inner;

    public SqlServer()
    {
        const int hostPort = 21541;
        if (Environment.GetEnvironmentVariable("CI") == null)
            _inner = new SqlServerEmbeddedContainer(hostPort);
        else
            _inner = new SqlServerComposedContainer(hostPort.ToString());

        MemoryStreamManager = new RecyclableMemoryStreamManager();
        StreetNameCache = new FakeStreetNameCache();
    }

    public RecyclableMemoryStreamManager MemoryStreamManager { get; }
    public IStreetNameCache StreetNameCache { get; }

    public Task<SqlConnectionStringBuilder> CreateDatabaseAsync()
    {
        return _inner.CreateDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return _inner.DisposeAsync();
    }

    public Task InitializeAsync()
    {
        return _inner.InitializeAsync();
    }

    public async Task<ProducerSnapshotContext> CreateProducerSnapshotContextAsync(SqlConnectionStringBuilder builder)
    {
        var options = new DbContextOptionsBuilder<ProducerSnapshotContext>()
            .UseSqlServer(builder.ConnectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder.UseNetTopologySuite())
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ProducerSnapshotContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    public async Task<ProducerSnapshotContext> CreateEmptyProducerSnapshotContextAsync(SqlConnectionStringBuilder builder)
    {
        var context = await CreateProducerSnapshotContextAsync(builder);
        
        context.RoadNodes.RemoveRange(context.RoadNodes);

        context.ProjectionStates.RemoveRange(context.ProjectionStates);
        await context.SaveChangesAsync();

        return context;
    }
}