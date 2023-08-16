namespace EntityFrameworkCore.DynamicData.Test.Data
{
	using Microsoft.EntityFrameworkCore;

	public class TestDbContext : DbContext
	{
		public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
		{
		}

		public DbSet<TestEntity> TestEntities { get; set; }
	}
}
