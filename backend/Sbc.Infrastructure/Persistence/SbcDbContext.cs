using Microsoft.EntityFrameworkCore;
using Sbc.Domain.Common;
using Sbc.Domain.Entities;

namespace Sbc.Infrastructure.Persistence;

public class SbcDbContext : DbContext
{
    public SbcDbContext(DbContextOptions<SbcDbContext> options)
        : base(options)
    {
    }

    public DbSet<Simulator> Simulators => Set<Simulator>();

    public DbSet<ProtectedSystem> ProtectedSystems => Set<ProtectedSystem>();

    public DbSet<BackupJob> BackupJobs => Set<BackupJob>();

    public DbSet<BackupEvent> BackupEvents => Set<BackupEvent>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<ManualBackupRequest> ManualBackupRequests => Set<ManualBackupRequest>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries<AuditableEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            }

            entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSimulator(modelBuilder);
        ConfigureProtectedSystem(modelBuilder);
        ConfigureBackupJob(modelBuilder);
        ConfigureBackupEvent(modelBuilder);
        ConfigureAlert(modelBuilder);
        ConfigureManualBackupRequest(modelBuilder);
    }

    private static void ConfigureSimulator(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Simulator>(entity =>
        {
            entity.ToTable("simulators");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Location)
                .HasMaxLength(150);

            entity.HasIndex(x => x.Code)
                .IsUnique();
        });
    }

    private static void ConfigureProtectedSystem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProtectedSystem>(entity =>
        {
            entity.ToTable("protected_systems");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Hostname)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.IpAddress)
                .HasMaxLength(50);

            entity.Property(x => x.OperatingSystem)
                .HasMaxLength(100);

            entity.Property(x => x.FileSystem)
                .HasMaxLength(50);

            entity.Property(x => x.PartitionScheme)
                .HasMaxLength(50);

            entity.Property(x => x.UrBackupClientId)
                .HasMaxLength(100);

            entity.Property(x => x.UrBackupClientName)
                .HasMaxLength(150);

            entity.Property(x => x.UrBackupClientVersion)
                .HasMaxLength(50);

            entity.Property(x => x.Criticality)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.BackupCapability)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.Hostname);

            entity.HasIndex(x => x.UrBackupClientId);

            entity.HasIndex(x => x.IsOnline);

            entity.HasIndex(x => x.LastSeenAtUtc);

            entity.HasOne(x => x.Simulator)
                .WithMany(x => x.Systems)
                .HasForeignKey(x => x.SimulatorId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureBackupJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackupJob>(entity =>
        {
            entity.ToTable("backup_jobs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Source)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.BackupType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.UrBackupJobId)
                .HasMaxLength(100);

            entity.HasIndex(x => x.ProtectedSystemId);

            entity.HasIndex(x => x.Status);

            entity.HasOne(x => x.ProtectedSystem)
                .WithMany(x => x.BackupJobs)
                .HasForeignKey(x => x.ProtectedSystemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureBackupEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackupEvent>(entity =>
        {
            entity.ToTable("backup_events");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.EventType)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.Severity)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Message)
                .IsRequired();

            entity.HasOne(x => x.ProtectedSystem)
                .WithMany(x => x.BackupEvents)
                .HasForeignKey(x => x.ProtectedSystemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.BackupJob)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.BackupJobId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureAlert(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("alerts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Message)
                .IsRequired();

            entity.Property(x => x.Severity)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(x => x.Code);

            entity.HasIndex(x => x.Status);

            entity.HasOne(x => x.ProtectedSystem)
                .WithMany(x => x.Alerts)
                .HasForeignKey(x => x.ProtectedSystemId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureManualBackupRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManualBackupRequest>(entity =>
        {
            entity.ToTable("manual_backup_requests");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RequestedBy)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.AssignedTo)
                .HasMaxLength(150);

            entity.Property(x => x.RelatedChangeReference)
                .HasMaxLength(100);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.ValidatedBy)
                .HasMaxLength(150);

            entity.HasIndex(x => x.Status);

            entity.HasOne(x => x.ProtectedSystem)
                .WithMany()
                .HasForeignKey(x => x.ProtectedSystemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}