using Microsoft.EntityFrameworkCore;

namespace HedgeCalc.Api.Data;

public sealed class HedgeDbContext(DbContextOptions<HedgeDbContext> options) : DbContext(options)
{
    public DbSet<HedgeCalculation> Calculations => Set<HedgeCalculation>();
    public DbSet<HedgeCalculationLine> CalculationLines => Set<HedgeCalculationLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HedgeCalculation>(entity =>
        {
            entity.ToTable("HedgeCalculations");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.BaseCurrency).HasMaxLength(3).IsRequired();
            entity.HasMany(c => c.Lines)
                  .WithOne()
                  .HasForeignKey(l => l.HedgeCalculationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HedgeCalculationLine>(entity =>
        {
            entity.ToTable("HedgeCalculationLines");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Currency).HasMaxLength(3).IsRequired();

            // Explicit precision: SQLite ignores it (stores TEXT), but SQL Server would
            // otherwise default to decimal(18,2) and silently truncate.
            entity.Property(l => l.Exposure).HasPrecision(28, 8);
            entity.Property(l => l.HedgeRatio).HasPrecision(9, 8);
            entity.Property(l => l.TargetNotional).HasPrecision(28, 8);

            // Store the enum as readable text ("SellForeign") rather than an int.
            entity.Property(l => l.Direction).HasConversion<string>().HasMaxLength(16);
        });
    }
}
