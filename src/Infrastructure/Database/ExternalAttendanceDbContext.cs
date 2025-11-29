using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;

public class ExternalAttendanceDbContext : DbContext
{
    public ExternalAttendanceDbContext(DbContextOptions<ExternalAttendanceDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// جدول الأحداث من نظام البصمات الخارجي
    /// </summary>
    public DbSet<EventTab> EventTabs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // تكوين جدول EventTab
        modelBuilder.Entity<EventTab>(entity =>
        {
            // اسم الجدول في SQL Server (قد يحتاج للتعديل حسب اسم الجدول الفعلي)
            entity.ToTable("EventTab", schema: "dbo");

            // لا يوجد Primary Key - جدول للقراءة فقط
            entity.HasNoKey();



        });
    }

    /// <summary>
    /// تعطيل Change Tracking لتحسين الأداء (قراءة فقط)
    /// </summary>
    public void ConfigureForReadOnly()
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }
}
