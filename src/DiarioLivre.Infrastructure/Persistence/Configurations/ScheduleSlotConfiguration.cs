using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class ScheduleSlotConfiguration : IEntityTypeConfiguration<ScheduleSlot>
{
    public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
    {
        builder.ToTable("ScheduleSlots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.LessonCount).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.RoomLabel).HasMaxLength(80);

        builder.HasIndex(x => new { x.AcademicYearId, x.DayOfWeek, x.StartTime });

        builder.HasOne(x => x.AcademicYear)
            .WithMany()
            .HasForeignKey(x => x.AcademicYearId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Classroom)
            .WithMany()
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
