using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioLivre.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RollNumber).IsRequired();
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(180);
        builder.Property(x => x.PhotoFileName).HasMaxLength(260);
        builder.Property(x => x.HasSpecialNeeds).IsRequired();
        builder.Property(x => x.SeatRow);
        builder.Property(x => x.SeatColumn);
        builder.Property(x => x.GeneralNotes).HasMaxLength(2000);

        builder.HasIndex(x => new { x.ClassroomId, x.RollNumber });

        builder.HasMany(x => x.Grades)
            .WithOne(x => x.Student)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AttendanceEntries)
            .WithOne(x => x.Student)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Observations)
            .WithOne(x => x.Student)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
