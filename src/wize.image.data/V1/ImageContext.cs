using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using wize.common.tenancy.Interfaces;
using wize.image.data.V1.Models;

namespace wize.image.data.V1
{
    public class ImageContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;
        public ImageContext(DbContextOptions<ImageContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public virtual DbSet<Image> Images { get; set; }

        public override TEntity Find<TEntity>(params object[] keyValues)
        {
            var model = base.Find<TEntity>(keyValues);
            var tenantId = _tenantProvider.GetTenantId();
            var modelTenantId = base.Entry(model).CurrentValues.GetValue<Guid>("TenantId");
            if (!tenantId.HasValue || modelTenantId != tenantId.Value)
                return default;

            return model;
        }

        public override object Find(Type entityType, params object[] keyValues)
        {
            var model = base.Find(entityType, keyValues);
            var tenantId = _tenantProvider.GetTenantId();
            var modelTenantId = base.Entry(model).CurrentValues.GetValue<Guid>("TenantId");
            if (!tenantId.HasValue || modelTenantId != tenantId.Value)
                return default;

            return model;
        }

        public override ValueTask<object> FindAsync(Type entityType, params object[] keyValues)
        {
            var model = base.FindAsync(entityType, keyValues);
            var tenantId = _tenantProvider.GetTenantId();
            var modelTenantId = base.Entry(model).CurrentValues.GetValue<Guid>("TenantId");
            if (!tenantId.HasValue || modelTenantId != tenantId.Value)
                return default;

            return model;
        }

        public override ValueTask<TEntity> FindAsync<TEntity>(params object[] keyValues)
        {
            var model = base.FindAsync<TEntity>(keyValues);
            var tenantId = _tenantProvider.GetTenantId();
            var modelTenantId = base.Entry(model).CurrentValues.GetValue<Guid>("TenantId");
            if (!tenantId.HasValue || modelTenantId != tenantId.Value)
                return default;

            return model;
        }

        public override ValueTask<TEntity> FindAsync<TEntity>(object[] keyValues, CancellationToken cancellationToken)
        {
            var model = base.FindAsync<TEntity>(keyValues, cancellationToken);
            var tenantId = _tenantProvider.GetTenantId();
            var modelTenantId = base.Entry(model).CurrentValues.GetValue<Guid>("TenantId");
            if (!tenantId.HasValue || modelTenantId != tenantId.Value)
                return default;

            return model;
        }

        public override ValueTask<object> FindAsync(Type entityType, object[] keyValues, CancellationToken cancellationToken)
        {
            var model = base.FindAsync(entityType, keyValues, cancellationToken);
            var tenantId = _tenantProvider.GetTenantId();
            var modelTenantId = base.Entry(model).CurrentValues.GetValue<Guid>("TenantId");
            if (!tenantId.HasValue || modelTenantId != tenantId.Value)
                return default;

            return model;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder = AddTenancy(modelBuilder);
            //modelBuilder = AddDeleted(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            ApplyTenancy();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyTenancy();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenancy();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyTenancy();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ApplyTenancy()
        {
            var modified = ChangeTracker.Entries<ITenantModel>().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            foreach (var entity in modified)
            {
                var property = entity.Property("TenantId");
                if (property != null)
                {
                    property.CurrentValue = _tenantProvider.GetTenantId();
                }
            }
        }

        private ModelBuilder AddTenancy(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Image>().Property<Guid>("TenantId");

            return modelBuilder;
        }

        /// <summary>
        /// not implemented yet
        /// </summary>
        /// <param name="modelBuilder"></param>
        /// <returns></returns>
        private ModelBuilder AddDeleted(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Image>().Property<bool>("IsDeleted");
            modelBuilder.Entity<Image>().HasQueryFilter(f => EF.Property<bool>(f, "IsDeleted"));

            return modelBuilder;
        }
    }
}
