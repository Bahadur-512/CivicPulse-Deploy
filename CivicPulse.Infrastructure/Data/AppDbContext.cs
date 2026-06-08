using Microsoft.EntityFrameworkCore;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;

namespace CivicPulse.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ComplaintAttachment> ComplaintAttachments => Set<ComplaintAttachment>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<SlaConfig> SlaConfigs => Set<SlaConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.CNIC).HasMaxLength(13);

        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasIndex(e => e.ComplaintNumber).IsUnique();
            entity.Property(e => e.ComplaintNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.LocationDescription).HasMaxLength(500);
            entity.Property(e => e.ReopenReason).HasMaxLength(500);

            entity.HasOne(e => e.Citizen)
                .WithMany(u => u.Complaints)
                .HasForeignKey(e => e.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Complaints)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedAdmin)
                .WithMany()
                .HasForeignKey(e => e.AssignedAdminId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IconName).HasMaxLength(50);
            entity.Property(e => e.ColorHex).HasMaxLength(7);
            entity.Property(e => e.Keywords).HasMaxLength(1000);

        });

        modelBuilder.Entity<ComplaintAttachment>(entity =>
        {
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(100);

            entity.HasOne(e => e.Complaint)
                .WithMany(c => c.Attachments)
                .HasForeignKey(e => e.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StatusHistory>(entity =>
        {
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(e => e.Complaint)
                .WithMany(c => c.StatusHistories)
                .HasForeignKey(e => e.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedBy)
                .WithMany()
                .HasForeignKey(e => e.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Body).HasMaxLength(1000);


            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RelatedComplaint)
                .WithMany()
                .HasForeignKey(e => e.RelatedComplaintId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.Property(e => e.Comment).HasMaxLength(500);

            entity.HasOne(e => e.Complaint)
                .WithOne(c => c.Feedback)
                .HasForeignKey<Feedback>(e => e.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Citizen)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(e => e.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SlaConfig>(entity =>
        {
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CategoryId, e.Priority }).IsUnique();
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Property(e => e.CreatedAt).IsModified = false;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
