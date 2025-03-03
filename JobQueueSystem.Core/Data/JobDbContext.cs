using JobQueueSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobQueueSystem.Core.Data
{
    public class JobDbContext : DbContext
    {
        public JobDbContext(DbContextOptions<JobDbContext> options) : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<WorkerNode> WorkerNodes { get; set; }
        public DbSet<JobExecutionLog> JobExecutionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Job entity configuration
            modelBuilder.Entity<Job>(entity =>
            {
                entity.ToTable("Jobs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.JobName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Priority).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.JobData).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

                // Indexes for performance
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Priority);
                entity.HasIndex(e => e.ScheduledTime);
                entity.HasIndex(e => e.AssignedWorker);
            });

            // Worker node entity configuration
            modelBuilder.Entity<WorkerNode>(entity =>
            {
                entity.ToTable("WorkerNode");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).HasConversion<int>();

                // Indexes for performance
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.LastHeartbeat);
            });

            // Job execution log entity configuration
            modelBuilder.Entity<JobExecutionLog>(entity =>
            {
                entity.ToTable("JobExecutionLog");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.JobId).IsRequired();
                entity.Property(e => e.LogLevel).HasConversion<int>();
                entity.Property(e => e.Message).IsRequired();

                // Index for job correlation
                entity.HasIndex(e => e.JobId);
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }

  
}
