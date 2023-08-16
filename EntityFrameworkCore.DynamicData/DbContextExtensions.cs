namespace EntityFrameworkCore.DynamicData
{
	using global::DynamicData;
	using Microsoft.EntityFrameworkCore;
	using Microsoft.EntityFrameworkCore.ChangeTracking;
	using ReactiveMarbles.ObservableEvents;
	using System.Reactive.Linq;

	public static class DbContextExtensions
	{
		public static IObservable<IChangeSet<TEntity, TKey>> Preview<TDbContext, TEntity, TKey>(
			this TDbContext db,
			Func<TEntity, TKey> keyFactory)
			where TDbContext : DbContext
			where TEntity : class
			where TKey : notnull
		{
			if (db is null)
				throw new ArgumentNullException(nameof(db));
			if (keyFactory is null)
				throw new ArgumentNullException(nameof(keyFactory));

			return Observable.Create<IChangeSet<TEntity, TKey>>(obs =>
			{
				var changeTrackerEvents = db.ChangeTracker.Events();
				return Observable.Merge<EntityEntryEventArgs>(
						changeTrackerEvents.StateChanged,
						changeTrackerEvents.Tracked)
					.Where(args => args.Entry.Entity is TEntity)
					.Select(CreateChange)
					.Where(change => change != default)
					.Select(change => new ChangeSet<TEntity, TKey> { change })
					.SubscribeSafe(obs);

				Change<TEntity, TKey> CreateChange(EntityEntryEventArgs args)
				{
					var state = args.Entry.State;
					var entity = (TEntity)(args.Entry.Entity);
					var key = keyFactory(entity);

					Change<TEntity, TKey> result = default;

					switch (state)
					{
						case EntityState.Detached:
							break;
						case EntityState.Unchanged:
							result = new(ChangeReason.Refresh, key, entity);
							break;
						case EntityState.Deleted:
							result = new(ChangeReason.Remove, key, entity);
							break;
						case EntityState.Modified:
							result = new(ChangeReason.Update, key, entity);
							break;
						case EntityState.Added:
							result = new(ChangeReason.Add, key, entity);
							break;
					}

					return result;
				}
			});
		}

		public static IObservable<IChangeSet<TEntity, TKey>> Connect<TDbContext, TEntity, TKey>(
			this TDbContext db,
			Func<TDbContext, IQueryable<TEntity>> entitySelector,
			Func<TEntity, TKey> keyFactory)
			where TDbContext : DbContext
			where TEntity : class
			where TKey : notnull
		{
			if (db is null)
				throw new ArgumentNullException(nameof(db));
			if (entitySelector is null)
				throw new ArgumentNullException(nameof(entitySelector));
			if (keyFactory is null)
				throw new ArgumentNullException(nameof(keyFactory));

			return Observable.FromAsync(ct => entitySelector(db).ToListAsync(ct))
				.Select(entities => entities
					.Select(entity => new Change<TEntity, TKey>(ChangeReason.Add, keyFactory(entity), entity))
					.ToList())
				.Select(changes => new ChangeSet<TEntity, TKey>(changes))
				.Concat(db.Preview(keyFactory));
		}
	}
}
