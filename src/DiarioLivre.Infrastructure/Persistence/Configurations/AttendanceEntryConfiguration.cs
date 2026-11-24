using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceEntryConfiguration : IEntityTypeConfiguration<AttendanceEntry>
{
    public void Configure(EntityTypeBuilder<AttendanceEntry> builder)
    {
        builder.ToTable("AttendanceEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired();

        builder.HasIndex(x => new { x.AttendanceSessionId, x.StudentId }).IsUnique();
    }
}
