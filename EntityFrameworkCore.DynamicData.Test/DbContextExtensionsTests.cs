namespace EntityFrameworkCore.DynamicData.Test
{
	using EntityFrameworkCore.DynamicData.Test.Data;
	using global::DynamicData;
	using Microsoft.EntityFrameworkCore;
	using Xunit;

	public class DbContextExtensionsTests
	{
		private readonly Func<TestEntity, Guid> keyFactory = entity => entity.Id;
		private readonly Func<TestDbContext, DbSet<TestEntity>> entitySelector = db => db.TestEntities;

		[Fact]
		public void SimpleAdd()
		{
			var listToAdd = new List<TestEntity> { TestEntity.Peter, TestEntity.Bob };
			var dbContext = InitInMemoryDB.From("TestDB", TestEntity.Justus);

			var dbCache = dbContext.GetCache(entitySelector, keyFactory);
			var cache = dbCache.Connect().AsObservableCache();

			dbContext.AddRange(listToAdd);
			dbContext.SaveChanges();

			Assert.Equal(dbContext.TestEntities.Count(), cache.Count);
		}

		[Fact]
		public void SimpleRemove()
		{
			var listToRemove = new List<TestEntity> { TestEntity.Peter, TestEntity.Bob };
			var dbContext = InitInMemoryDB.From("TestDB", TestEntity.Justus, TestEntity.Peter, TestEntity.Bob);

			var dbCache = dbContext.GetCache(entitySelector, keyFactory);
			var cache = dbCache.Connect().AsObservableCache();

			dbContext.RemoveRange(listToRemove);
			dbContext.SaveChanges();

			Assert.Single(cache.Items);
			Assert.Same(TestEntity.Justus, cache.Items.FirstOrDefault());
		}

		[Fact]
		public void SimpleModify()
		{
			string oldName = "Test";
			string newName = "ChangedTest";
			var entityToModify = new TestEntity { Name = oldName, Description = "Its a test!" };
			var dbContext = InitInMemoryDB.From("TestDB", TestEntity.Justus, TestEntity.Peter, TestEntity.Bob, entityToModify);

			var dbCache = dbContext.GetCache(entitySelector, keyFactory);
			var cache = dbCache.Connect().AsObservableCache();

			entityToModify.Name = newName;

			dbContext.SaveChanges();

			Assert.Equal(newName, cache.Items.First(item => item.Id == entityToModify.Id).Name);
		}
	}
}
