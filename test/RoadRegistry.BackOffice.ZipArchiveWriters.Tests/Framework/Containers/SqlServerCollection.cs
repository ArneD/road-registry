namespace RoadRegistry.BackOffice.ZipArchiveWriters.Tests.Framework.Containers;

using Xunit;

[CollectionDefinition(nameof(SqlServerCollection))]
public class SqlServerCollection : ICollectionFixture<SqlServer>
{
}