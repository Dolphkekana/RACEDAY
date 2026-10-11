using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Database context for the RaceDay API using EF Core Code-First.
/// </summary>
public sealed class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
            entity.Property(u => u.DateOfBirth).HasColumnType("date");
            entity.Property(u => u.CreatedAt)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSDATETIME()");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Users_Role",
                "[Role] IN (N'Organiser', N'Participant')"));
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("EventTypes");
            entity.HasKey(et => et.EventTypeId);
            entity.HasIndex(et => et.Name).IsUnique();
            entity.Property(et => et.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EventDate).HasColumnType("date");
            entity.Property(e => e.DistanceKm).HasPrecision(6, 2).IsRequired();
            entity.Property(e => e.BannerImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2(0)");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Events_DistanceKm", "[DistanceKm] > 0");
                table.HasCheckConstraint(
                    "CK_Events_Date",
                    "[EventDate] >= CONVERT(date, [CreatedAt])");
            });

            entity.HasOne(e => e.Organiser)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EventType)
                .WithMany(et => et.Events)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.CategoryId);
            entity.HasIndex(c => new { c.EventId, c.Name }).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.DistanceKm).HasPrecision(6, 2);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Categories_MinimumAge",
                    "[MinimumAge] IS NULL OR [MinimumAge] >= 0");
                table.HasCheckConstraint(
                    "CK_Categories_MaximumAge",
                    "[MaximumAge] IS NULL OR [MaximumAge] >= 0");
                table.HasCheckConstraint(
                    "CK_Categories_AgeRange",
                    "[MinimumAge] IS NULL OR [MaximumAge] IS NULL OR [MinimumAge] <= [MaximumAge]");
                table.HasCheckConstraint(
                    "CK_Categories_DistanceKm",
                    "[DistanceKm] IS NULL OR [DistanceKm] > 0");
            });

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments");
            entity.HasKey(e => e.EnrolmentId);
            entity.HasIndex(e => new { e.ParticipantId, e.EventId }).IsUnique();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.EnrolmentDate)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSDATETIME()");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Enrolments_Status",
                "[Status] IN (N'Pending', N'Confirmed', N'Cancelled')"));

            entity.HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(r => r.ResultId);
            entity.HasIndex(r => r.EnrolmentId).IsUnique();
            entity.Property(r => r.FinishTime).HasColumnType("time(0)").IsRequired();
            entity.Property(r => r.FinishingPosition).IsRequired();
            entity.Property(r => r.RecordedAt)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSDATETIME()");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Results_FinishingPosition",
                "[FinishingPosition] > 0"));

            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
