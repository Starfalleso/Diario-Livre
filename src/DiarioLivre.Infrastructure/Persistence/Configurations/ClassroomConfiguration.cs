using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.ToTable("Classrooms");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Shift).IsRequired();
        builder.Property(x => x.IsMultiSubject).IsRequired();
        builder.Property(x => x.SeatRows).IsRequired().HasDefaultValue(5);
        builder.Property(x => x.SeatColumns).IsRequired().HasDefaultValue(6);

        builder.HasMany(x => x.Subjects)
            .WithOne(x => x.Classroom)
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Students)
            .WithOne(x => x.Classroom)
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AttendanceSessions)
            .WithOne(x => x.Classroom)
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
