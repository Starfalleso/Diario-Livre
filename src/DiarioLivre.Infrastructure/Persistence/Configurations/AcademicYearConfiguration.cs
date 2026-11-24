using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.ToTable("AcademicYears");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.SchoolName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.GradingSystem).IsRequired();
        builder.Property(x => x.PassingGrade).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.MinAttendancePercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.TargetSchoolDays).IsRequired().HasDefaultValue(200);
        builder.Property(x => x.GradeCalculationMode).IsRequired();
        builder.Property(x => x.RecoveryPolicy).IsRequired();

        builder.HasMany(x => x.Classrooms)
            .WithOne(x => x.AcademicYear)
            .HasForeignKey(x => x.AcademicYearId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CalendarEvents)
            .WithOne(x => x.AcademicYear)
            .HasForeignKey(x => x.AcademicYearId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
