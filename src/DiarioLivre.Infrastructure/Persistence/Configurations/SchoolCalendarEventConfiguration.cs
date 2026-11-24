using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class SchoolCalendarEventConfiguration : IEntityTypeConfiguration<SchoolCalendarEvent>
{
    public void Configure(EntityTypeBuilder<SchoolCalendarEvent> builder)
    {
        builder.ToTable("SchoolCalendarEvents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TermNumber).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.AcademicYearId, x.Date });
    }
}
