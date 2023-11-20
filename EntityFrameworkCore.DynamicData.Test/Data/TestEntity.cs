namespace EntityFrameworkCore.DynamicData.Test.Data
{
	using System;

	public class TestEntity
	{
		public static TestEntity Bob { get; } = new TestEntity { Name = "Bob", Description = "Its Bob" };
		public static TestEntity Peter { get; } = new TestEntity { Name = "Peter", Description = "Its Peter" };
		public static TestEntity Justus { get; } = new TestEntity { Name = "Justus", Description = "Its Justus" };
		public static IList<TestEntity> All { get; } = new List<TestEntity> { Justus, Peter, Bob };

		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}
}
