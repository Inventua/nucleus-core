using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// Entity-framework dbcontext implementation.
	/// </summary>
	/// <remarks>
	/// This class includes automatic configuration of the data provider, automatic handling of <see cref="ModelBase"/> audit properties, 
	/// and a special SaveChanges overload which allows the caller to limit the scope of saved changes.  Modules should inherit this class
	/// instead of inheriting <seealso cref="Microsoft.EntityFrameworkCore.DbContext"/>.
	/// </remarks>
	public abstract class DbContext: Microsoft.EntityFrameworkCore.DbContext 
	{
		// Private/Protected properties

		/// <summary>
		/// Logger factory used to provide logger instance for entity-framework logs.
		/// </summary>
		protected ILoggerFactory LoggerFactory { get; }

		/// <summary>
		/// Http context accessor.
		/// </summary>
		/// <remarks>
		/// This is used to determine the current user Id for populating audit columns.
		/// </remarks>
		protected IHttpContextAccessor HttpContextAccessor { get; }

		/// <summary>
		/// 
		/// </summary>
		protected DbContextConfigurator DbContextConfigurator { get; }
		
		internal DbSet<Nucleus.Abstractions.Models.Internal.Schema> Schema { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="dbContextConfigurator"></param>
		/// <param name="httpContextAccessor"></param>
		/// <param name="loggerFactory"></param>
		public DbContext(DbContextConfigurator dbContextConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory)
		{
			this.DbContextConfigurator = dbContextConfigurator;
			this.HttpContextAccessor = httpContextAccessor;
			this.LoggerFactory = loggerFactory;
		}

		///// <summary>
		///// Constructor
		///// </summary>
		///// <remarks>
		///// This constructor exists for use by the entity framework tools only.  Do not use this constructor.
		///// </remarks>
		//public DbContext(DbContextOptions options, DbContextConfigurator dbContextConfigurator): base(options)
		//{
		//	this.DbContextConfigurator = dbContextConfigurator;
		//}

		/// <summary>
		/// Call the dbContextConfigurator to configure this DbContext.
		/// </summary>
		/// <param name="options"></param>
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			options.UseLoggerFactory(this.LoggerFactory);
			
			options.ConfigureWarnings(warnings =>
			{
				#if DEBUG
				warnings.Default(WarningBehavior.Throw);
				#endif
				// EF generates this one when we use .FirstOrDefault() or .SingleOrDefault() without an ORDER BY, which we\
				// do all the time when retrieving a single row by id.
				warnings.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning);				
			});

			this.DbContextConfigurator.Configure(options);
		}

		/// <summary>
		/// Configure the <seealso cref="ModelBuilder"/> with settings for the Core Nucleus entities.
		/// </summary>
		/// <param name="builder"></param>
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.ConfigureInstanceEntities();
			builder.ConfigureSiteEntities();
			builder.ConfigurePageEntities();
			builder.ConfigureUserEntities();
			builder.ConfigureModuleEntities();
			builder.ConfigureFileSystemEntities();
		}

		/// <summary>
		/// Create a copy of the <paramref name="original"/> object and add it to the DbContext change tracker.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="original"></param>
		/// <returns></returns>
		public T AttachClone<T>(T original) where T : new()
		{
			T newEntity = new();
			this.Attach(newEntity);

			this.Entry(newEntity).CurrentValues.SetValues(original);

			// Cloning a CLR object using CurrentValues.SetValues puts entity framework in a state where it does not automatically
			// generate Id values for primary keys when a new record is created.  This code looks for primary key properties of type GUID
			// and generates values.
			Microsoft.EntityFrameworkCore.Metadata.IKey key = this.Entry(newEntity).Metadata.FindPrimaryKey();
			if (key != null)
			{
				foreach (Microsoft.EntityFrameworkCore.Metadata.RuntimeProperty keyProp in this.Entry(newEntity).Metadata.FindPrimaryKey().Properties)
				{
					if (this.Entry(newEntity).Property(keyProp.Name).CurrentValue.GetType() == typeof(System.Guid))
					{
						if ((Guid)this.Entry(newEntity).Property(keyProp.Name).CurrentValue == Guid.Empty)
						{
							this.Entry(newEntity).Property(keyProp.Name).CurrentValue = Guid.NewGuid();
						}
					}
				}
			}

			return newEntity;
		}

		/// <summary>
		/// Save changes made in this context, after updating audit properties.
		/// </summary>
		/// <param name="acceptAllChangesOnSuccess"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
		{
			ManageSpecialColumns();
			return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		}

		/// <summary>
		/// Save changes made in this context, after updating audit properties.
		/// </summary>
		/// <param name="acceptAllChangesOnSuccess"></param>
		/// <returns></returns>
		public override int SaveChanges(bool acceptAllChangesOnSuccess)
		{
			ManageSpecialColumns();
			return base.SaveChanges(acceptAllChangesOnSuccess);
		}

		/// <summary>
		/// Save changes made in this context for the type specified by <typeparamref name="TEntity"/> ONLY, after updating audit properties.
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <returns></returns>
		public async Task<int> SaveChangesAsync<TEntity>() where TEntity : class
		{
			// Save original states
			var entries = this.ChangeTracker.Entries()
				.Where(entityEntry => !typeof(TEntity).IsAssignableFrom(entityEntry.Entity.GetType()) && entityEntry.State != EntityState.Unchanged)
				.GroupBy(entityEntry => entityEntry.State)
				.ToList();

			// Set all but the nominated type (TEntity) to unchanged to prevent updates
			foreach (var entry in this.ChangeTracker.Entries().Where(entityEntry => !typeof(TEntity).IsAssignableFrom(entityEntry.Entity.GetType())))
			{
				entry.State = EntityState.Unchanged;
			}

			int rows = await base.SaveChangesAsync();

			// Detach specified entities after saving
			foreach (var entry in this.ChangeTracker.Entries().Where(entityEntry => typeof(TEntity).IsAssignableFrom(entityEntry.Entity.GetType())))
			{
				entry.State = EntityState.Detached;
			}

			// Restore original states
			foreach (var state in entries)
			{
				foreach (var entry in state)
				{
					entry.State = state.Key;
				}
			}

			return rows;
		}

		/// <summary>
		/// Save changes made in this context for the type specified by <typeparamref name="TEntity"/> ONLY, after updating audit properties.
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <returns></returns>
		public int SaveChanges<TEntity>() where TEntity : class
		{
			// Save original states
			var entries = this.ChangeTracker.Entries()
				.Where(entityEntry => !typeof(TEntity).IsAssignableFrom(entityEntry.Entity.GetType()) && entityEntry.State != EntityState.Unchanged)
				.GroupBy(entityEntry => entityEntry.State)
				.ToList();

			// Set all but the nominated type (TEntity) to unchanged to prevent updates
			foreach (var entry in this.ChangeTracker.Entries().Where(entityEntry => !typeof(TEntity).IsAssignableFrom(entityEntry.Entity.GetType())))
			{
				entry.State = EntityState.Unchanged;
			}

			int rows = base.SaveChanges();

			// Detach specified entities after saving
			foreach (var entry in this.ChangeTracker.Entries().Where(entityEntry => typeof(TEntity).IsAssignableFrom(entityEntry.Entity.GetType())))
			{
				entry.State = EntityState.Detached;
			}

			// Restore original states
			foreach (var state in entries)
			{
				foreach (var entry in state)
				{
					entry.State = state.Key;
				}
			}

			return rows;
		}

		private void ManageSpecialColumns()
		{
			foreach (EntityEntry entry in this.ChangeTracker.Entries()
									.Where(entityEntry => entityEntry.Entity is ModelBase && (entityEntry.State == EntityState.Added || entityEntry.State == EntityState.Modified)))
			{
				ModelBase model = entry.Entity as ModelBase;

				if (entry.State == EntityState.Added)
				{
					model.DateAdded = DateTime.UtcNow;
					model.AddedBy = this.CurrentUserId();
					this.Entry(model).Property(model => model.ChangedBy).IsModified = false;
					this.Entry(model).Property(model => model.DateChanged).IsModified = false;
				}
				else
				{
					model.DateChanged = DateTime.UtcNow;
					model.ChangedBy = this.CurrentUserId();
					this.Entry(model).Property(model => model.AddedBy).IsModified = false;
					this.Entry(model).Property(model => model.DateAdded).IsModified = false;
				}

				// Set properties of type Guid? where the value is Guid.Empty to null.  ASP.NET core model binding sets model values to Guid.Empty when
				// they should be null.
				foreach (System.Reflection.PropertyInfo prop in model.GetType().GetProperties().Where(prop => prop.PropertyType == typeof(Guid?)))
				{
					if ((Guid?)prop.GetValue(model) == Guid.Empty)
					{
						prop.SetValue(model, null);
					}
				}

			}
		}

		private Guid CurrentUserId()
		{
			System.Security.Claims.ClaimsPrincipal user = this.HttpContextAccessor.HttpContext?.User;
			if (user == null)
			{
				return Guid.Empty;
			}
			return user.GetUserId();
		}
	}
}
