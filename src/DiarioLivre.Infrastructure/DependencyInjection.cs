using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Services;
using DiarioLivre.Infrastructure.Persistence;
using DiarioLivre.Infrastructure.Persistence.Repositories;
using DiarioLivre.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiarioLivre.Infrastructure;

/// <summary>
/// Dependency injection extensions for registering infrastructure and core domain services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDiarioLivreServices(
        this IServiceCollection services,
        string databasePath,
        string avatarsDirectory)
    {
        string? dbDirectory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        services.AddDbContextFactory<DiarioLivreDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IGradeCalculator, GradeCalculator>();
        services.AddSingleton<IAttendanceCalculator, AttendanceCalculator>();
        services.AddSingleton<ISchoolDaysCalculator, SchoolDaysCalculator>();

        services.AddSingleton<IAcademicYearRepository, AcademicYearRepository>();
        services.AddSingleton<IClassroomRepository, ClassroomRepository>();
        services.AddSingleton<IStudentRepository, StudentRepository>();
        services.AddSingleton<IGradebookRepository, GradebookRepository>();
        services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
        services.AddSingleton<IObservationRepository, ObservationRepository>();
        services.AddSingleton<ISchoolCalendarRepository, SchoolCalendarRepository>();
        services.AddSingleton<IScheduleRepository, ScheduleRepository>();
        services.AddSingleton<ILessonPlanRepository, LessonPlanRepository>();

        services.AddSingleton<IAvatarService>(_ => new SkiaAvatarService(avatarsDirectory));
        services.AddSingleton<IReportGenerator, QuestPdfReportService>();
        services.AddSingleton<IBackupService>(sp => new SqliteBackupService(
            sp.GetRequiredService<IDbContextFactory<DiarioLivreDbContext>>(),
            databasePath,
            avatarsDirectory));
        services.AddSingleton<DatabaseInitializer>();

        return services;
    }
}
