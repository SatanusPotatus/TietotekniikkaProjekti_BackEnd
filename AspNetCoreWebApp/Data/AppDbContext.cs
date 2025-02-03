using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DataModel> Data { get; set; }
}

public class DataModel
{
    public int Id { get; set; }
    public string Name { get; set; }
}
