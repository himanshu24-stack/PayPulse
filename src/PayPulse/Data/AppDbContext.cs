using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using PayPulse.Models;

namespace PayPulse.Data;

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public static string GetDatabasePath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string folder = Path.Combine(localAppData, "PayPulse");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return Path.Combine(folder, "PayPulse.db");
    }

    public static string GetConnectionString()
    {
        return $"Data Source={GetDatabasePath()}";
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Phone).IsRequired().HasMaxLength(30);
            entity.HasIndex(c => c.Name);
            entity.HasIndex(c => c.Email);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(i => i.InvoiceNumber).IsUnique();
            entity.Property(i => i.Status).HasConversion<string>();

            entity.HasOne(i => i.Customer)
                  .WithMany(c => c.Invoices)
                  .HasForeignKey(i => i.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // InvoiceItem
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(ii => ii.Id);
            entity.Property(ii => ii.Description).IsRequired().HasMaxLength(250);

            entity.HasOne(ii => ii.Invoice)
                  .WithMany(i => i.Items)
                  .HasForeignKey(ii => ii.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.PaymentMethod).IsRequired().HasMaxLength(50);

            entity.HasOne(p => p.Invoice)
                  .WithMany(i => i.Payments)
                  .HasForeignKey(p => p.InvoiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ReminderLog
        modelBuilder.Entity<ReminderLog>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ReminderType).IsRequired().HasMaxLength(50);

            entity.HasOne(r => r.Invoice)
                  .WithMany(i => i.ReminderLogs)
                  .HasForeignKey(r => r.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
