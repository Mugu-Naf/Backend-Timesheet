using Microsoft.EntityFrameworkCore;
using FirstAPI.Models;

namespace FirstAPI.Contexts
{
    public class TimeSheetContext : DbContext
    {
        public TimeSheetContext(DbContextOptions<TimeSheetContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<OvertimeRule> OvertimeRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Username);
                entity.Property(u => u.Username).HasMaxLength(50);
            });

            // Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);
                entity.HasOne(e => e.User)
                      .WithOne(u => u.Employee)
                      .HasForeignKey<Employee>(e => e.Username)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Timesheet
            modelBuilder.Entity<Timesheet>(entity =>
            {
                entity.HasKey(t => t.TimesheetId);
                entity.HasOne(t => t.Employee)
                      .WithMany(e => e.Timesheets)
                      .HasForeignKey(t => t.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.Project)
                      .WithMany(p => p.Timesheets)
                      .HasForeignKey(t => t.ProjectId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(t => new { t.EmployeeId, t.Date }).IsUnique();
            });

            // LeaveRequest
            modelBuilder.Entity<LeaveRequest>(entity =>
            {
                entity.HasKey(l => l.LeaveRequestId);
                entity.HasOne(l => l.Employee)
                      .WithMany(e => e.LeaveRequests)
                      .HasForeignKey(l => l.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Project
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.ProjectId);
                entity.HasIndex(p => p.ProjectName).IsUnique();
            });

            // Attendance
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(a => a.AttendanceId);
                entity.HasOne(a => a.Employee)
                      .WithMany(e => e.Attendances)
                      .HasForeignKey(a => a.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
            });

            // OvertimeRule
            modelBuilder.Entity<OvertimeRule>(entity =>
            {
                entity.HasKey(o => o.OvertimeRuleId);
            });
        }
    }
}
