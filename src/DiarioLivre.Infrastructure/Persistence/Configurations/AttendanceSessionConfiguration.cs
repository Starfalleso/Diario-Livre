using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable("AttendanceSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.TermNumber).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.LessonCount).IsRequired();
        builder.Property(x => x.TopicCovered).HasMaxLength(500);
        builder.Property(x => x.DailyActivity).HasMaxLength(1000);
        builder.Property(x => x.Homework).HasMaxLength(500);
        builder.Property(x => x.LessonNotes).HasMaxLength(1000);

        builder.HasIndex(x => new { x.ClassroomId, x.SubjectId, x.Date });

        builder.HasMany(x => x.Entries)
            .WithOne(x => x.AttendanceSession)
            .HasForeignKey(x => x.AttendanceSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
