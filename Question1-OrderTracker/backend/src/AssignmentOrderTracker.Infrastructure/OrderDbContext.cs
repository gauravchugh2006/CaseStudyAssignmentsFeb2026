using AssignmentOrderTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssignmentOrderTracker.Infrastructure;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasKey(o => o.OrderId);
        modelBuilder.Entity<Order>().OwnsMany(o => o.Items);
        modelBuilder.Entity<Order>().OwnsMany(o => o.Timeline);
        modelBuilder.Entity<Order>().OwnsMany(o => o.Notes);
    }
}
