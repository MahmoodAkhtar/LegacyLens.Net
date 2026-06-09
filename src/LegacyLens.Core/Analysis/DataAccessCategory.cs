namespace LegacyLens.Core.Analysis;

public enum DataAccessCategory
{
    ConnectionString,
    DatabaseProvider,
    EntityFramework6,
    EntityFrameworkCore,
    EdmxObjectContext,
    AdoNet,
    Dapper,
    NHibernate,
    LinqToSql,
    RawSql,
    StoredProcedure,
    RepositoryPattern,
    UnitOfWorkPattern,
    MigrationArtifact,
    UnknownRequiresReview
}