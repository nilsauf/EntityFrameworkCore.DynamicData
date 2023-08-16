namespace EntityFrameworkCore.DynamicData
{
	using global::DynamicData;
	using Microsoft.EntityFrameworkCore;

	/// <summary>
	/// Extensions for <see cref="DbContext"/> classes to work with DynamicData
	/// </summary>
	public static class DbContextExtensions
	{
		/// <summary>
		/// Creates a proxy cache for a specified query from <paramref name="dbContext"/>
		/// </summary>
		/// <typeparam name="TDbContext">The correct type of <paramref name="dbContext"/></typeparam>
		/// <typeparam name="TEntity">The type of entities to track</typeparam>
		/// <typeparam name="TKey">The type of the key</typeparam>
		/// <param name="dbContext">The <see cref="DbContext"/> to operate on</param>
		/// <param name="queryFactory">The <see cref="IQueryable{TEntity}"/> to operate on</param>
		/// <param name="keyFactory">A factory to generate keys</param>
		/// <returns>A proxy cache for <paramref name="dbContext"/></returns>
		/// <exception cref="ArgumentNullException">Thrown if any of the parameters are null</exception>
		public static IConnectableCache<TEntity, TKey> GetCache<TDbContext, TEntity, TKey>(
			this TDbContext dbContext,
			Func<TDbContext, IQueryable<TEntity>> queryFactory,
			Func<TEntity, TKey> keyFactory)
			where TDbContext : DbContext
			where TEntity : class
			where TKey : notnull
		{
			if (dbContext is null)
				throw new ArgumentNullException(nameof(dbContext));

			if (queryFactory is null)
				throw new ArgumentNullException(nameof(queryFactory));

			if (keyFactory is null)
				throw new ArgumentNullException(nameof(keyFactory));

			return new DbContextCache<TDbContext, TEntity, TKey>(
				dbContext,
				queryFactory(dbContext),
				keyFactory);
		}
	}
}
