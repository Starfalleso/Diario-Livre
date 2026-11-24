using DiarioLivre.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for local offline SQLite persistence.
/// </summary>
public sealed class DiarioLivreDbContext : DbContext
{
    public DiarioLivreDbContext(DbContextOptions<DiarioLivreDbContext> options)
        : base(options)
    {
    }

    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceEntry> AttendanceEntries => Set<AttendanceEntry>();
    public DbSet<StudentObservation> StudentObservations => Set<StudentObservation>();
    public DbSet<SchoolCalendarEvent> SchoolCalendarEvents => Set<SchoolCalendarEvent>();
    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
    public DbSet<LessonPlan> LessonPlans => Set<LessonPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiarioLivreDbContext).Assembly);
    }
}
