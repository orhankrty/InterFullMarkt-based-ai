namespace InterFullMarkt.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using InterFullMarkt.Domain;
using InterFullMarkt.Domain.Entities;
using InterFullMarkt.Domain.ValueObjects;
using InterFullMarkt.Infrastructure.Data.Seeds;
using InterFullMarkt.Application.Abstractions;

/// <summary>
/// InterFullMarkt veritabanı konteksti.
/// .NET 10.0 Primary Constructor ile yazılmış, Fluent API ile yapılandırılmış.
/// Global Query Filter ile Soft Delete otomasyonu sağlar.
/// </summary>
public sealed class InterFullMarktDbContext(DbContextOptions<InterFullMarktDbContext> options) : DbContext(options), IDbContext
{
    /// <summary>
    /// Kullanıcılar tablosu
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Futbolcular tablosu
    /// </summary>
    public DbSet<Player> Players { get; set; } = null!;

    /// <summary>
    /// Kulüpler tablosu
    /// </summary>
    public DbSet<Club> Clubs { get; set; } = null!;

    /// <summary>
    /// Ligler tablosu
    /// </summary>
    public DbSet<League> Leagues { get; set; } = null!;
    public DbSet<Transfer> Transfers { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;

    /// <summary>
    /// DbContext konfigürasyonu:
    /// - Entity Type Configurations
    /// - Global Query Filters (Soft Delete)
    /// - Seed Data
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InterFullMarktDbContext).Assembly);

        modelBuilder.Entity<Order>().OwnsOne(o => o.TotalAmount);
        modelBuilder.Entity<OrderItem>().OwnsOne(oi => oi.UnitPrice);
        modelBuilder.Entity<OrderItem>().OwnsOne(oi => oi.TotalPrice);
        modelBuilder.Entity<Product>().OwnsOne(p => p.Price);

        // Global Query Filters
        ApplyGlobalQueryFilters(modelBuilder);
    }

    /// <summary>
    /// ISoftDelete arayüzüne sahip tüm varlıklar için Global Query Filter uygular.
    /// Silinmiş (IsDeleted = true) veriler otomatikman filtrelenir.
    /// </summary>
    private static void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        var softDeleteEntities = modelBuilder.Model
            .GetEntityTypes()
            .Where(et => et.ClrType != null && typeof(ISoftDelete).IsAssignableFrom(et.ClrType))
            .ToList();

        foreach (var entityType in softDeleteEntities)
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType!);
            var filterExpression = System.Linq.Expressions.Expression.Lambda(
                System.Linq.Expressions.Expression.Equal(
                    System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)),
                    System.Linq.Expressions.Expression.Constant(false, typeof(bool))),
                parameter);

            modelBuilder.Entity(entityType.ClrType!).HasQueryFilter(filterExpression);
        }
    }

    /// <summary>
    /// SaveChanges öncesinde Audit bilgilerini doldurur.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Başarılı olursa kabul et</param>
    /// <returns>Etkilenen satır sayısı</returns>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateAuditableEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// SaveChangesAsync öncesinde Audit bilgilerini doldurur.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Başarılı olursa kabul et</param>
    /// <param name="cancellationToken">İptal tokeni</param>
    /// <returns>Etkilenen satır sayısı</returns>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// IAuditEntity arayüzüne sahip varlıkların audit bilgilerini otomatik doldurur.
    /// </summary>
    private void UpdateAuditableEntities()
    {
        var auditableEntities = ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAuditEntity && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified))
            .ToList();

        foreach (var entry in auditableEntities)
        {
            if (entry.Entity is not IAuditEntity auditEntity)
                continue;

            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                auditEntity.CreatedDate = now;
                auditEntity.UpdatedDate = now;
                if (string.IsNullOrEmpty(auditEntity.CreatedByUserId))
                    auditEntity.CreatedByUserId = "System";
            }

            if (entry.State == EntityState.Modified)
            {
                auditEntity.UpdatedDate = now;
                if (string.IsNullOrEmpty(auditEntity.UpdatedByUserId))
                    auditEntity.UpdatedByUserId = "System";
            }
        }
    }
}
