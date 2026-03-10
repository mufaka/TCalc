using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TCalc.Web.Models;

namespace TCalc.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SavedDataSet> SavedDataSets => Set<SavedDataSet>();
    public DbSet<DataRow> DataRows => Set<DataRow>();
    public DbSet<SavedWorkspace> SavedWorkspaces => Set<SavedWorkspace>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SavedDataSet>(e =>
        {
            e.HasIndex(d => d.UserId);
            e.HasMany(d => d.Rows)
             .WithOne(r => r.DataSet)
             .HasForeignKey(r => r.DataSetId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DataRow>(e =>
        {
            e.HasIndex(r => r.DataSetId);
        });

        builder.Entity<SavedWorkspace>(e =>
        {
            e.HasIndex(w => w.UserId);
        });
    }
}
