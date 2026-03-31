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
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }

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
                // Removed unique index on EmployeeId+Date to allow multiple sessions per day
                entity.HasIndex(a => new { a.EmployeeId, a.Date });
            });

            // OvertimeRule
            modelBuilder.Entity<OvertimeRule>(entity =>
            {
                entity.HasKey(o => o.OvertimeRuleId);
            });

            // AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.AuditLogId);
                entity.HasIndex(a => a.Username);
                entity.HasIndex(a => a.Timestamp);
                entity.HasIndex(a => new { a.EntityType, a.EntityId });
            });

            // LeaveBalance
            modelBuilder.Entity<LeaveBalance>(entity =>
            {
                entity.HasKey(lb => lb.LeaveBalanceId);
                entity.HasIndex(lb => new { lb.EmployeeId, lb.Year }).IsUnique();
                entity.HasOne(lb => lb.Employee)
                      .WithMany()
                      .HasForeignKey(lb => lb.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ProjectMember
            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasKey(pm => pm.ProjectMemberId);
                entity.HasIndex(pm => new { pm.ProjectId, pm.EmployeeId }).IsUnique();
                entity.HasOne(pm => pm.Project)
                      .WithMany()
                      .HasForeignKey(pm => pm.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(pm => pm.Employee)
                      .WithMany()
                      .HasForeignKey(pm => pm.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
