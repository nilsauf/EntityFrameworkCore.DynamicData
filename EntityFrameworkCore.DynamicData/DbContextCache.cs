namespace EntityFrameworkCore.DynamicData
{
	using global::DynamicData;
	using Microsoft.EntityFrameworkCore;
	using Microsoft.EntityFrameworkCore.ChangeTracking;
	using ReactiveMarbles.ObservableEvents;
	using System;
	using System.Linq;
	using System.Reactive.Linq;

	/// <summary>
	/// A <see cref="IConnectableCache{TEntity, TKey}"/> for an <see cref="DbContext"/> class
	/// </summary>
	/// <typeparam name="TDbContext">The correct type of the <see cref="DbContext"/> object</typeparam>
	/// <typeparam name="TEntity">The type of entities to track</typeparam>
	/// <typeparam name="TKey">The type of the key</typeparam>
	public class DbContextCache<TDbContext, TEntity, TKey> : IConnectableCache<TEntity, TKey>
		where TDbContext : DbContext
		where TEntity : class
		where TKey : notnull
	{
		private readonly IQueryable<TEntity> entities;
		private readonly Func<TEntity, TKey> keyFactory;
		private readonly RxChangeTrackerEvents changeTrackerEvents;

		/// <summary>
		/// Creates a <see cref="IConnectableCache{TEntity, TKey}"/> for an <see cref="DbContext"/> class
		/// </summary>
		/// <param name="dbContext">The <see cref="DbContext"/> to operate on</param>
		/// <param name="entities">The <see cref="IQueryable{TEntity}"/> to operate on</param>
		/// <param name="keyFactory">A factory to generate keys</param>
		/// <exception cref="ArgumentNullException">Thrown if any of the parameters are null</exception>
		public DbContextCache(TDbContext dbContext, IQueryable<TEntity> entities, Func<TEntity, TKey> keyFactory)
		{
			this.changeTrackerEvents = dbContext?.ChangeTracker.Events() ?? throw new ArgumentNullException(nameof(dbContext));
			this.entities = entities ?? throw new ArgumentNullException(nameof(entities));
			this.keyFactory = keyFactory ?? throw new ArgumentNullException(nameof(keyFactory));
		}

		/// <inheritdoc/>
		public IObservable<int> CountChanged => Observable.FromAsync(this.entities.CountAsync)
			.RepeatWhen(signal => signal.Concat(
				MergedObservables()
					.Where(args => args.Entry.State == EntityState.Added || args.Entry.State == EntityState.Deleted)));

		/// <inheritdoc/>
		public IObservable<IChangeSet<TEntity, TKey>> Connect(Func<TEntity, bool>? predicate = null, bool suppressEmptyChangeSets = true)
		{
			return Observable.FromAsync(ct => entities.ToListAsync(ct))
				.Select(entities => entities
					.Where(Filter)
					.Select(CreateAddChange)
					.ToList())
				.Select(CreateChangeSet)
				.Concat(this.Preview(predicate));

			bool Filter(TEntity entity)
			{
				return predicate is null || predicate(entity);
			}

			Change<TEntity, TKey> CreateAddChange(TEntity entity)
			{
				return new Change<TEntity, TKey>(ChangeReason.Add, keyFactory(entity), entity);
			}

			ChangeSet<TEntity, TKey> CreateChangeSet(IEnumerable<Change<TEntity, TKey>> changes)
			{
				return new ChangeSet<TEntity, TKey>(changes);
			}
		}

		/// <inheritdoc/>
		public IObservable<IChangeSet<TEntity, TKey>> Preview(Func<TEntity, bool>? predicate = null)
		{
			return Observable.Create<IChangeSet<TEntity, TKey>>(obs =>
			{
				return MergedObservables()
					.Select(CreateChange)
					.Where(change => change != default)
					.Select(CreateChangeSet)
					.SubscribeSafe(obs);

				Change<TEntity, TKey> CreateChange(EntityEntryEventArgs args)
				{
					var entity = (TEntity)(args.Entry.Entity);
					Change<TEntity, TKey> result = default;

					if (predicate is not null && predicate(entity) == false)
					{
						return result;
					}

					var state = args.Entry.State;
					var key = keyFactory(entity);

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

				ChangeSet<TEntity, TKey> CreateChangeSet(Change<TEntity, TKey> change)
				{
					return new ChangeSet<TEntity, TKey> { change };
				}

			});
		}

		/// <inheritdoc/>
		public IObservable<Change<TEntity, TKey>> Watch(TKey key)
		{
			return Observable.FromAsync(ct => this.entities.FirstAsync(entity => this.keyFactory(entity).Equals(key), ct))
				.Select(entity => new Change<TEntity, TKey>(ChangeReason.Add, keyFactory(entity), entity))
				.Concat(this.Preview(entity => this.keyFactory(entity).Equals(key))
					.Select(changeSet => changeSet.First()));
		}

		private IObservable<EntityEntryEventArgs> MergedObservables()
			=> Observable.Merge<EntityEntryEventArgs>(
					this.changeTrackerEvents.StateChanged,
					this.changeTrackerEvents.Tracked)
				.Where(args => args.Entry.Entity is TEntity);
	}
}
