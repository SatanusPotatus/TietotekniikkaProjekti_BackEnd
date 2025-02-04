using AspNetCoreWebApp.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DataModel> Data { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataModel>().ToContainer("YourContainerName");
        modelBuilder.Entity<DataModel>().HasPartitionKey(x => x.Id);
    }
}
