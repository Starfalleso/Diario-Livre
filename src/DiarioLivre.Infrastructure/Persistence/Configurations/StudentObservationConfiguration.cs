using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class StudentObservationConfiguration : IEntityTypeConfiguration<StudentObservation>
{
    public void Configure(EntityTypeBuilder<StudentObservation> builder)
    {
        builder.ToTable("StudentObservations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.Category).IsRequired();
        builder.Property(x => x.Content).IsRequired().HasMaxLength(2000);
    }
}
