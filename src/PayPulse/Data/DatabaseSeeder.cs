using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayPulse.Models;

namespace PayPulse.Data;

public static class DatabaseSeeder
{
    public static async Task SeedSampleDataAsync(AppDbContext context, bool forceReset = false)
    {
        if (forceReset)
        {
            context.ReminderLogs.RemoveRange(context.ReminderLogs);
            context.Payments.RemoveRange(context.Payments);
            context.InvoiceItems.RemoveRange(context.InvoiceItems);
            context.Invoices.RemoveRange(context.Invoices);
            context.Customers.RemoveRange(context.Customers);
            await context.SaveChangesAsync();
        }

        if (await context.Customers.AnyAsync())
        {
            return; // Data already exists
        }

        var customers = new List<Customer>
        {
            new Customer
            {
                Name = "TechNova Solutions Pvt Ltd",
                ContactPerson = "Rahul Sharma",
                Email = "rahul@technova.co.in",
                Phone = "+91 98201 12345",
                BillingAddress = "Plot 42, Hitech City, Hyderabad, Telangana",
                Notes = "Enterprise software client. Regular buyer, pays within 15 days.",
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new Customer
            {
                Name = "Apex Retailers India",
                ContactPerson = "Pooja Mehta",
                Email = "accounts@apexretail.in",
                Phone = "+91 98111 67890",
                BillingAddress = "204 Nariman Point, Mumbai, Maharashtra",
                Notes = "Retail chain. History of delayed payments due to internal audit cycles.",
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            },
            new Customer
            {
                Name = "BluePeak Consulting",
                ContactPerson = "Amitabh Sen",
                Email = "amitabh@bluepeak.org",
                Phone = "+91 97400 45678",
                BillingAddress = "88 Indiranagar, Bengaluru, Karnataka",
                Notes = "Strategy consultancy. Very prompt with UPI/NEFT settlements.",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            },
            new Customer
            {
                Name = "CloudCraft Media Studio",
                ContactPerson = "Siddharth Rao",
                Email = "finance@cloudcraft.media",
                Phone = "+91 99887 54321",
                BillingAddress = "15 Connaught Place, New Delhi",
                Notes = "Creative media partner. Requires frequent follow-up.",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            }
        };

        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();

        var custTechNova = customers[0];
        var custApex = customers[1];
        var custBluePeak = customers[2];
        var custCloudCraft = customers[3];

        var today = DateTime.Today;

        // Invoice 1: Fully Paid (TechNova)
        var inv1 = new Invoice
        {
            InvoiceNumber = "INV-2026-001",
            CustomerId = custTechNova.Id,
            IssueDate = today.AddDays(-45),
            DueDate = today.AddDays(-15),
            Description = "Web Portal Architecture & Cloud Setup",
            Subtotal = 45000m,
            Discount = 2000m,
            TaxRate = 18m,
            TaxAmount = 7740m,
            TotalAmount = 50740m,
            Status = InvoiceStatus.Paid,
            Notes = "Completed milestone 1.",
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Cloud Architecture Design", Quantity = 1, UnitPrice = 25000m, Amount = 25000m },
                new InvoiceItem { Description = "Backend API Scaffolding", Quantity = 1, UnitPrice = 20000m, Amount = 20000m }
            }
        };

        // Invoice 2: Partially Paid & Overdue (Apex Retailers)
        var inv2 = new Invoice
        {
            InvoiceNumber = "INV-2026-002",
            CustomerId = custApex.Id,
            IssueDate = today.AddDays(-40),
            DueDate = today.AddDays(-10),
            Description = "Inventory POS System Integration",
            Subtotal = 80000m,
            Discount = 0m,
            TaxRate = 18m,
            TaxAmount = 14400m,
            TotalAmount = 94400m,
            Status = InvoiceStatus.Overdue,
            Notes = "50% advance received, balance overdue.",
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "POS Barcode Scanner Module", Quantity = 1, UnitPrice = 40000m, Amount = 40000m },
                new InvoiceItem { Description = "Database Synchronization Service", Quantity = 1, UnitPrice = 40000m, Amount = 40000m }
            }
        };

        // Invoice 3: Unpaid & Overdue (CloudCraft)
        var inv3 = new Invoice
        {
            InvoiceNumber = "INV-2026-003",
            CustomerId = custCloudCraft.Id,
            IssueDate = today.AddDays(-25),
            DueDate = today.AddDays(-3),
            Description = "Custom Video Rendering Engine",
            Subtotal = 35000m,
            Discount = 0m,
            TaxRate = 18m,
            TaxAmount = 6300m,
            TotalAmount = 41300m,
            Status = InvoiceStatus.Overdue,
            Notes = "High priority follow-up needed.",
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "FFmpeg pipeline integration", Quantity = 1, UnitPrice = 35000m, Amount = 35000m }
            }
        };

        // Invoice 4: Partially Paid & Upcoming (BluePeak)
        var inv4 = new Invoice
        {
            InvoiceNumber = "INV-2026-004",
            CustomerId = custBluePeak.Id,
            IssueDate = today.AddDays(-10),
            DueDate = today.AddDays(20),
            Description = "Data Analytics Dashboard Module",
            Subtotal = 60000m,
            Discount = 3000m,
            TaxRate = 18m,
            TaxAmount = 10260m,
            TotalAmount = 67260m,
            Status = InvoiceStatus.PartiallyPaid,
            Notes = "Client paid token advance.",
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Executive KPI Dashboard UI", Quantity = 1, UnitPrice = 30000m, Amount = 30000m },
                new InvoiceItem { Description = "Automated Financial Reporting", Quantity = 1, UnitPrice = 30000m, Amount = 30000m }
            }
        };

        // Invoice 5: Draft (TechNova)
        var inv5 = new Invoice
        {
            InvoiceNumber = "INV-2026-005",
            CustomerId = custTechNova.Id,
            IssueDate = today,
            DueDate = today.AddDays(30),
            Description = "Phase 2 Security Hardening & Audit",
            Subtotal = 50000m,
            Discount = 0m,
            TaxRate = 18m,
            TaxAmount = 9000m,
            TotalAmount = 59000m,
            Status = InvoiceStatus.Draft,
            Notes = "Draft proposal under review.",
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Vulnerability Assessment", Quantity = 1, UnitPrice = 50000m, Amount = 50000m }
            }
        };

        await context.Invoices.AddRangeAsync(inv1, inv2, inv3, inv4, inv5);
        await context.SaveChangesAsync();

        // Payments
        var payments = new List<Payment>
        {
            // Full payment for Inv 1
            new Payment
            {
                InvoiceId = inv1.Id,
                PaymentDate = today.AddDays(-20),
                Amount = 50740m,
                PaymentMethod = "Bank Transfer",
                TransactionReference = "NEFT-HDFC-99238472",
                Notes = "Settled in full."
            },
            // Partial payment for Inv 2 (Paid ₹47,200 of ₹94,400)
            new Payment
            {
                InvoiceId = inv2.Id,
                PaymentDate = today.AddDays(-38),
                Amount = 47200m,
                PaymentMethod = "UPI",
                TransactionReference = "UPI-ICICI-88392019",
                Notes = "50% advance."
            },
            // Partial payment for Inv 4 (Paid ₹25,000 of ₹67,260)
            new Payment
            {
                InvoiceId = inv4.Id,
                PaymentDate = today.AddDays(-8),
                Amount = 25000m,
                PaymentMethod = "Bank Transfer",
                TransactionReference = "IMPS-AXIS-11234987",
                Notes = "Token advance paid on contract signing."
            }
        };

        await context.Payments.AddRangeAsync(payments);

        // Reminder Logs
        var reminderLogs = new List<ReminderLog>
        {
            new ReminderLog
            {
                InvoiceId = inv2.Id,
                ReminderDate = today.AddDays(-5),
                ReminderType = "Phone Call",
                Notes = "Called accounts department; Pooja Mehta stated balance approval is pending with director.",
                Status = "Contacted"
            },
            new ReminderLog
            {
                InvoiceId = inv3.Id,
                ReminderDate = today.AddDays(-1),
                ReminderType = "Email Follow-up",
                Notes = "Sent formal friendly payment reminder to Siddharth Rao.",
                Status = "Contacted"
            }
        };

        await context.ReminderLogs.AddRangeAsync(reminderLogs);
        await context.SaveChangesAsync();
    }
}
