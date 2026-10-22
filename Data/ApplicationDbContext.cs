
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkTime.Api.Models;

namespace WorkTime.Api.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees
        => Set<Employee>();

    public DbSet<AttendanceRecord> AttendanceRecords
        => Set<AttendanceRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.CheckInMethod)
                .HasMaxLength(30);

            entity.HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(u => u.EmployeeId)
                .IsUnique();
        });
    }
}
