using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/*
 Database context for the RaceDay API using EF Core Code-First.
 Configures entity mappings, relationships, unique constraints, and decimal precision
 matching the approved Part 1 database script.
*/
public class RaceDayDbContext : DbContext
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

        //User entity mapping
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
            entity.Property(u => u.CreatedAt).HasPrecision(0);
        });

        //EventType entity mapping
        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("EventTypes");
            entity.HasKey(et => et.EventTypeId);
            entity.HasIndex(et => et.Name).IsUnique();
            entity.Property(et => et.Name).HasMaxLength(50).IsRequired();
        });

        //Event entity mapping
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DistanceKm).HasPrecision(6, 2).IsRequired();
            entity.Property(e => e.BannerImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(e => e.Organiser)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EventType)
                .WithMany(et => et.Events)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        //Category entity mapping
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.CategoryId);
            entity.HasIndex(c => new { c.EventId, c.Name }).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.DistanceKm).HasPrecision(6, 2);

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Enrolment entity mapping 
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments");
            entity.HasKey(e => e.EnrolmentId);
            entity.HasIndex(e => new { e.ParticipantId, e.EventId }).IsUnique();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.EnrolmentDate).HasPrecision(0);

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

        //Result entity mapping 
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(r => r.ResultId);
            entity.HasIndex(r => r.EnrolmentId).IsUnique();
            entity.Property(r => r.FinishTime).HasPrecision(0).IsRequired();
            entity.Property(r => r.FinishingPosition).IsRequired();
            entity.Property(r => r.RecordedAt).HasPrecision(0);

            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
