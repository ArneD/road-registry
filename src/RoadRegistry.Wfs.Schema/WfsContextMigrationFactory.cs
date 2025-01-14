namespace RoadRegistry.Wfs.Schema;

using BackOffice;
using Be.Vlaanderen.Basisregisters.ProjectionHandling.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public class WfsContextMigrationFactory : RunnerDbContextMigrationFactory<WfsContext>
{
    public WfsContextMigrationFactory() :
        base(WellknownConnectionNames.WfsProjectionsAdmin, HistoryConfiguration)
    {
    }

    private static MigrationHistoryConfiguration HistoryConfiguration =>
        new()
        {
            Schema = WellknownSchemas.WfsSchema,
            Table = MigrationTables.Wfs
        };

    protected override void ConfigureSqlServerOptions(SqlServerDbContextOptionsBuilder sqlServerOptions)
    {
        sqlServerOptions.UseNetTopologySuite();
        base.ConfigureSqlServerOptions(sqlServerOptions);
    }

    protected override WfsContext CreateContext(DbContextOptions<WfsContext> migrationContextOptions)
    {
        return new WfsContext(migrationContextOptions);
    }
}
