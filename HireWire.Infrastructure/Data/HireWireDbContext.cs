using Microsoft.EntityFrameworkCore;
using HireWire.Domain.Entities;
using HireWire.Domain.Enums;

namespace HireWire.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for the HireWire application.
/// </summary>
public class HireWireDbContext : DbContext
{
    public HireWireDbContext(DbContextOptions<HireWireDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobLead> JobLeads => Set<JobLead>();
    public DbSet<OE_Config> OE_Configs => Set<OE_Config>();
    public DbSet<Communication> Communications => Set<Communication>();
    public DbSet<InterviewAsset> InterviewAssets => Set<InterviewAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Company Configuration ---
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies");
            
            entity.HasKey(c => c.Id);
            
            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(150);
                
            entity.Property(c => c.Industry)
                .HasMaxLength(100);
                
            entity.Property(c => c.ScrapedContext)
                .HasMaxLength(2000);
                
            entity.Property(c => c.CreatedAt)
                .IsRequired();
            
            // One-to-Many: Company has many JobLeads
            entity.HasMany(c => c.JobLeads)
                  .WithOne(j => j.Company)
                  .HasForeignKey(j => j.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- JobLead Configuration ---
        modelBuilder.Entity<JobLead>(entity =>
        {
            entity.ToTable("JobLeads");
            
            entity.HasKey(j => j.Id);
            
            entity.Property(j => j.JobTitle)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(j => j.Url)
                .IsRequired()
                .HasMaxLength(1000);
                
            entity.Property(j => j.Platform)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(j => j.RawDescription)
                .IsRequired();
                
            entity.Property(j => j.SalaryRange)
                .HasMaxLength(100);
            
            // Map Enum Status property to StatusId integer column
            entity.Property(j => j.Status)
                  .HasColumnName("StatusId")
                  .HasConversion<int>()
                  .IsRequired();

            // One-to-Many: JobLead has many Communications
            entity.HasMany(j => j.Communications)
                  .WithOne(c => c.JobLead)
                  .HasForeignKey(c => c.JobLeadId)
                  .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: JobLead has many InterviewAssets
            entity.HasMany(j => j.InterviewAssets)
                  .WithOne(ia => ia.JobLead)
                  .HasForeignKey(ia => ia.JobLeadId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- OE_Config Configuration ---
        modelBuilder.Entity<OE_Config>(entity =>
        {
            entity.ToTable("OE_Configs");
            
            entity.HasKey(oe => oe.Id);
            
            entity.Property(oe => oe.AiMatchScore);
            
            entity.Property(oe => oe.IsAsync)
                .IsRequired();
                
            entity.Property(oe => oe.DetectedRedFlags)
                .HasMaxLength(2000);

            // Convert TargetRole enum to string (e.g. "J1", "J2") for readable storage
            entity.Property(oe => oe.TargetRole)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsRequired();

            // One-to-One: JobLead <-> OE_Config
            entity.HasOne(oe => oe.JobLead)
                  .WithOne(j => j.OE_Config)
                  .HasForeignKey<OE_Config>(oe => oe.JobLeadId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Communication Configuration ---
        modelBuilder.Entity<Communication>(entity =>
        {
            entity.ToTable("Communications");
            
            entity.HasKey(c => c.Id);
            
            entity.Property(c => c.ContactName)
                .HasMaxLength(100);
                
            entity.Property(c => c.MessageBody)
                .IsRequired();
                
            entity.Property(c => c.SentDate)
                .IsRequired();
                
            entity.Property(c => c.RequiresFollowUp)
                .IsRequired();
                
            entity.Property(c => c.FollowUpDate);
        });

        // --- InterviewAsset Configuration ---
        modelBuilder.Entity<InterviewAsset>(entity =>
        {
            entity.ToTable("InterviewAssets");
            
            entity.HasKey(ia => ia.Id);
            
            entity.Property(ia => ia.DynamicCvPath)
                .HasMaxLength(500);
                
            entity.Property(ia => ia.CheatSheetData); // Holds JSON string
            
            entity.Property(ia => ia.ScheduledDate);
        });
    }
}
