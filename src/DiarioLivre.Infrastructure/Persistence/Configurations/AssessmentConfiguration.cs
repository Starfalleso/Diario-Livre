using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TermNumber).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(150);
        builder.Property(x => x.MaxScore).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.Weight).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.IsRecovery).IsRequired();

        builder.HasMany(x => x.Grades)
            .WithOne(x => x.Assessment)
            .HasForeignKey(x => x.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
