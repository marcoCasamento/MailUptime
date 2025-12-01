using MailUptime.Models;
using Microsoft.EntityFrameworkCore;

namespace MailUptime.Data;

public class MailUptimeContext : DbContext
{
    public MailUptimeContext(DbContextOptions<MailUptimeContext> options) : base(options)
    {
    }

    public DbSet<MailCheckRecord> MailCheckRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MailCheckRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MailboxIdentifier, e.Day }).IsUnique();
            entity.Property(e => e.MailboxIdentifier).IsRequired();
            entity.Property(e => e.Day).IsRequired();
        });
    }
}
