using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Score).HasPrecision(5, 2);

        builder.HasIndex(x => new { x.AssessmentId, x.StudentId }).IsUnique();
    }
}
