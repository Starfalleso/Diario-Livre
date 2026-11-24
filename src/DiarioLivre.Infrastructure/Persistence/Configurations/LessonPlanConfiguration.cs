using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class LessonPlanConfiguration : IEntityTypeConfiguration<LessonPlan>
{
    public void Configure(EntityTypeBuilder<LessonPlan> builder)
    {
        builder.ToTable("LessonPlans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TermNumber).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.BnccCode).HasMaxLength(40);
        builder.Property(x => x.Objectives).HasMaxLength(1500);
        builder.Property(x => x.PlannedActivity).HasMaxLength(1000);
        builder.Property(x => x.Homework).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired();

        builder.HasIndex(x => new { x.ClassroomId, x.TermNumber, x.PlannedDate });

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
