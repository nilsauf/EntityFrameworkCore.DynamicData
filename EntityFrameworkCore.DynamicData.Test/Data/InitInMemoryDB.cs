namespace EntityFrameworkCore.DynamicData.Test.Data
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;

    internal static class InitInMemoryDB
    {
        internal static TestDbContext From<TEntity>(string dbName, params TEntity[] entities)
            where TEntity : class
        {
            var contextOptions = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options;

            var context = new TestDbContext(contextOptions);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.AddRange(entities);
            context.SaveChanges();

            return context;
        }

    }
}
