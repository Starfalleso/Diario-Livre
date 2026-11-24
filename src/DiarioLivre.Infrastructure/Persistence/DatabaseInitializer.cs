using System.Data;
using System.Data.Common;
using DiarioLivre.Core.Entities;
using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Persistence;

/// <summary>
/// Ensures the offline SQLite database exists and seeds initial Brazilian school sample data on first run.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;

    public DatabaseInitializer(IDbContextFactory<DiarioLivreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);
        await EnsureSchemaUpToDateAsync(context, ct);

        if (await context.AcademicYears.AnyAsync(ct))
        {
            return;
        }

        await SeedInitialDataAsync(context, ct);
    }

    /// <summary>
    /// Safely upgrades existing local SQLite databases without losing teacher records.
    /// </summary>
    public static async Task EnsureSchemaUpToDateAsync(DiarioLivreDbContext context, CancellationToken ct = default)
    {
        DbConnection connection = context.Database.GetDbConnection();
        bool wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            await EnsureMissingColumnsAsync(
                connection,
                "AcademicYears",
                new[] { ("TargetSchoolDays", "INTEGER NOT NULL DEFAULT 200") },
                ct);

            await EnsureMissingColumnsAsync(
                connection,
                "Classrooms",
                new[]
                {
                    ("SeatRows", "INTEGER NOT NULL DEFAULT 5"),
                    ("SeatColumns", "INTEGER NOT NULL DEFAULT 6")
                },
                ct);

            await EnsureMissingColumnsAsync(
                connection,
                "Students",
                new[]
                {
                    ("SeatRow", "INTEGER NULL"),
                    ("SeatColumn", "INTEGER NULL")
                },
                ct);

            await EnsureMissingColumnsAsync(
                connection,
                "AttendanceSessions",
                new[]
                {
                    ("TermNumber", "INTEGER NOT NULL DEFAULT 1"),
                    ("DailyActivity", "TEXT NULL"),
                    ("Homework", "TEXT NULL"),
                    ("LessonNotes", "TEXT NULL")
                },
                ct);

            await using DbCommand createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS "SchoolCalendarEvents" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_SchoolCalendarEvents" PRIMARY KEY,
                    "AcademicYearId" TEXT NOT NULL,
                    "Date" TEXT NOT NULL,
                    "Type" INTEGER NOT NULL,
                    "Title" TEXT NOT NULL,
                    "TermNumber" INTEGER NOT NULL DEFAULT 1,
                    "Notes" TEXT NULL,
                    CONSTRAINT "FK_SchoolCalendarEvents_AcademicYears_AcademicYearId"
                        FOREIGN KEY ("AcademicYearId") REFERENCES "AcademicYears" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_SchoolCalendarEvents_AcademicYearId_Date"
                    ON "SchoolCalendarEvents" ("AcademicYearId", "Date");

                CREATE TABLE IF NOT EXISTS "ScheduleSlots" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_ScheduleSlots" PRIMARY KEY,
                    "AcademicYearId" TEXT NOT NULL,
                    "ClassroomId" TEXT NOT NULL,
                    "SubjectId" TEXT NULL,
                    "DayOfWeek" INTEGER NOT NULL,
                    "StartTime" TEXT NOT NULL,
                    "EndTime" TEXT NOT NULL,
                    "LessonCount" INTEGER NOT NULL DEFAULT 1,
                    "RoomLabel" TEXT NULL,
                    CONSTRAINT "FK_ScheduleSlots_AcademicYears_AcademicYearId"
                        FOREIGN KEY ("AcademicYearId") REFERENCES "AcademicYears" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ScheduleSlots_Classrooms_ClassroomId"
                        FOREIGN KEY ("ClassroomId") REFERENCES "Classrooms" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ScheduleSlots_Subjects_SubjectId"
                        FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("Id") ON DELETE SET NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_ScheduleSlots_AcademicYearId_DayOfWeek_StartTime"
                    ON "ScheduleSlots" ("AcademicYearId", "DayOfWeek", "StartTime");

                CREATE TABLE IF NOT EXISTS "LessonPlans" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_LessonPlans" PRIMARY KEY,
                    "ClassroomId" TEXT NOT NULL,
                    "SubjectId" TEXT NULL,
                    "TermNumber" INTEGER NOT NULL DEFAULT 1,
                    "PlannedDate" TEXT NULL,
                    "Title" TEXT NOT NULL,
                    "BnccCode" TEXT NULL,
                    "Objectives" TEXT NULL,
                    "PlannedActivity" TEXT NULL,
                    "Homework" TEXT NULL,
                    "Status" INTEGER NOT NULL,
                    CONSTRAINT "FK_LessonPlans_Classrooms_ClassroomId"
                        FOREIGN KEY ("ClassroomId") REFERENCES "Classrooms" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_LessonPlans_Subjects_SubjectId"
                        FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("Id") ON DELETE SET NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_LessonPlans_ClassroomId_TermNumber_PlannedDate"
                    ON "LessonPlans" ("ClassroomId", "TermNumber", "PlannedDate");
                """;
            await createTableCmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureMissingColumnsAsync(
        DbConnection connection,
        string tableName,
        IReadOnlyList<(string ColumnName, string SqlDefinition)> expectedColumns,
        CancellationToken ct)
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (DbCommand pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = $"PRAGMA table_info('{tableName}');";
            await using DbDataReader reader = await pragmaCmd.ExecuteReaderAsync(ct);
            int nameOrdinal = reader.GetOrdinal("name");
            while (await reader.ReadAsync(ct))
            {
                if (!reader.IsDBNull(nameOrdinal))
                {
                    existingColumns.Add(reader.GetString(nameOrdinal));
                }
            }
        }

        if (existingColumns.Count == 0)
        {
            return;
        }

        foreach ((string columnName, string sqlDefinition) in expectedColumns)
        {
            if (existingColumns.Contains(columnName))
            {
                continue;
            }

            await using DbCommand alterCmd = connection.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {sqlDefinition};";
            await alterCmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task SeedInitialDataAsync(DiarioLivreDbContext context, CancellationToken ct)
    {
        var academicYearId = Guid.NewGuid();
        var academicYear = new AcademicYear
        {
            Id = academicYearId,
            Year = 2026,
            SchoolName = "E.E. Prof. Paulo Freire",
            GradingSystem = GradingSystem.Semester,
            PassingGrade = 6.0m,
            MinAttendancePercentage = 75.0m,
            TargetSchoolDays = 200,
            GradeCalculationMode = GradeCalculationMode.WeightedAverage,
            RecoveryPolicy = RecoveryPolicy.ReplaceLowest
        };

        var seventhGradeId = Guid.NewGuid();
        var seventhGrade = new Classroom
        {
            Id = seventhGradeId,
            AcademicYearId = academicYearId,
            Name = "7º Ano B",
            Shift = Shift.Morning,
            IsMultiSubject = false
        };

        var thirdGradeId = Guid.NewGuid();
        var thirdGrade = new Classroom
        {
            Id = thirdGradeId,
            AcademicYearId = academicYearId,
            Name = "3º Ano A",
            Shift = Shift.Afternoon,
            IsMultiSubject = true
        };

        var mathSubjectId = Guid.NewGuid();
        var scienceSubjectId = Guid.NewGuid();
        var polyvalentSubjectId = Guid.NewGuid();

        var subjects = new List<Subject>
        {
            new() { Id = mathSubjectId, ClassroomId = seventhGradeId, Name = "Matemática" },
            new() { Id = scienceSubjectId, ClassroomId = seventhGradeId, Name = "Ciências" },
            new() { Id = polyvalentSubjectId, ClassroomId = thirdGradeId, Name = "Polivalente" }
        };

        var s1 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = seventhGradeId,
            RollNumber = 1,
            FullName = "Ana Clara Silva",
            HasSpecialNeeds = false,
            SeatRow = 0,
            SeatColumn = 0,
            GeneralNotes = "Participativa nas aulas de resolução de problemas."
        };
        var s2 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = seventhGradeId,
            RollNumber = 2,
            FullName = "Bruno Henrique Costa",
            HasSpecialNeeds = false,
            SeatRow = 0,
            SeatColumn = 1,
            GeneralNotes = "Precisa de acompanhamento na entrega de tarefas de casa."
        };
        var s3 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = seventhGradeId,
            RollNumber = 3,
            FullName = "Carla Eduarda Oliveira",
            HasSpecialNeeds = false,
            SeatRow = 0,
            SeatColumn = 2
        };
        var s4 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = seventhGradeId,
            RollNumber = 4,
            FullName = "Diego Mateus Santos",
            HasSpecialNeeds = true,
            SeatRow = 0,
            SeatColumn = 3,
            GeneralNotes = "PDI ativo: tempo adicional em avaliações e enunciados segmentados."
        };
        var s5 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = seventhGradeId,
            RollNumber = 5,
            FullName = "Eduardo Pereira Lima",
            HasSpecialNeeds = false,
            SeatRow = 1,
            SeatColumn = 0,
            GeneralNotes = "Atenção à frequência nas primeiras aulas."
        };
        var s6 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = seventhGradeId,
            RollNumber = 6,
            FullName = "Fernanda Rocha Almeida",
            HasSpecialNeeds = false,
            SeatRow = 1,
            SeatColumn = 1
        };

        var p1 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = thirdGradeId,
            RollNumber = 1,
            FullName = "Gabriel Souza Martins",
            HasSpecialNeeds = false
        };
        var p2 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = thirdGradeId,
            RollNumber = 2,
            FullName = "Helena Beatriz Ferreira",
            HasSpecialNeeds = true,
            GeneralNotes = "PDI: apoio visual para atividades de leitura e escrita."
        };
        var p3 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = thirdGradeId,
            RollNumber = 3,
            FullName = "João Pedro Carvalho",
            HasSpecialNeeds = false
        };
        var p4 = new Student
        {
            Id = Guid.NewGuid(),
            ClassroomId = thirdGradeId,
            RollNumber = 4,
            FullName = "Larissa Vitória Mendes",
            HasSpecialNeeds = false
        };

        var seventhStudents = new[] { s1, s2, s3, s4, s5, s6 };
        var thirdStudents = new[] { p1, p2, p3, p4 };

        var mathExam1 = new Assessment
        {
            Id = Guid.NewGuid(),
            SubjectId = mathSubjectId,
            TermNumber = 1,
            Title = "Prova 1",
            MaxScore = 10.0m,
            Weight = 2.0m,
            IsRecovery = false
        };
        var mathProject1 = new Assessment
        {
            Id = Guid.NewGuid(),
            SubjectId = mathSubjectId,
            TermNumber = 1,
            Title = "Trabalho em Grupo",
            MaxScore = 10.0m,
            Weight = 1.0m,
            IsRecovery = false
        };
        var mathRec1 = new Assessment
        {
            Id = Guid.NewGuid(),
            SubjectId = mathSubjectId,
            TermNumber = 1,
            Title = "Recuperação Paralela",
            MaxScore = 10.0m,
            Weight = 1.0m,
            IsRecovery = true
        };
        var mathExam2 = new Assessment
        {
            Id = Guid.NewGuid(),
            SubjectId = mathSubjectId,
            TermNumber = 2,
            Title = "Prova Bimestral",
            MaxScore = 10.0m,
            Weight = 2.0m,
            IsRecovery = false
        };
        var scienceExam1 = new Assessment
        {
            Id = Guid.NewGuid(),
            SubjectId = scienceSubjectId,
            TermNumber = 1,
            Title = "Avaliação Teórica",
            MaxScore = 10.0m,
            Weight = 1.0m,
            IsRecovery = false
        };
        var polyExam1 = new Assessment
        {
            Id = Guid.NewGuid(),
            SubjectId = polyvalentSubjectId,
            TermNumber = 1,
            Title = "Leitura e Produção",
            MaxScore = 10.0m,
            Weight = 1.0m,
            IsRecovery = false
        };

        var grades = new List<Grade>
        {
            new() { Id = Guid.NewGuid(), AssessmentId = mathExam1.Id, StudentId = s1.Id, Score = 8.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = mathProject1.Id, StudentId = s1.Id, Score = 9.0m },
            new() { Id = Guid.NewGuid(), AssessmentId = mathExam2.Id, StudentId = s1.Id, Score = 8.0m },

            new() { Id = Guid.NewGuid(), AssessmentId = mathExam1.Id, StudentId = s2.Id, Score = 4.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = mathProject1.Id, StudentId = s2.Id, Score = 5.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = mathRec1.Id, StudentId = s2.Id, Score = 6.0m },

            new() { Id = Guid.NewGuid(), AssessmentId = mathExam1.Id, StudentId = s3.Id, Score = 7.0m },
            new() { Id = Guid.NewGuid(), AssessmentId = mathProject1.Id, StudentId = s3.Id, Score = null },

            new() { Id = Guid.NewGuid(), AssessmentId = mathExam1.Id, StudentId = s4.Id, Score = 6.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = mathProject1.Id, StudentId = s4.Id, Score = 8.0m },

            new() { Id = Guid.NewGuid(), AssessmentId = mathExam1.Id, StudentId = s5.Id, Score = 3.0m },
            new() { Id = Guid.NewGuid(), AssessmentId = mathProject1.Id, StudentId = s5.Id, Score = 0.0m },

            new() { Id = Guid.NewGuid(), AssessmentId = mathExam1.Id, StudentId = s6.Id, Score = 9.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = mathProject1.Id, StudentId = s6.Id, Score = 9.0m },

            new() { Id = Guid.NewGuid(), AssessmentId = scienceExam1.Id, StudentId = s1.Id, Score = 9.0m },
            new() { Id = Guid.NewGuid(), AssessmentId = scienceExam1.Id, StudentId = s2.Id, Score = 5.0m },
            new() { Id = Guid.NewGuid(), AssessmentId = scienceExam1.Id, StudentId = s3.Id, Score = 7.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = scienceExam1.Id, StudentId = s4.Id, Score = 7.0m },
            new() { Id = Guid.NewGuid(), AssessmentId = scienceExam1.Id, StudentId = s5.Id, Score = 4.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = scienceExam1.Id, StudentId = s6.Id, Score = 8.5m },

            new() { Id = Guid.NewGuid(), AssessmentId = polyExam1.Id, StudentId = p1.Id, Score = 8.0m },
            new() { Id = Guid.NewGuid(), AssessmentId = polyExam1.Id, StudentId = p2.Id, Score = 7.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = polyExam1.Id, StudentId = p3.Id, Score = 5.5m },
            new() { Id = Guid.NewGuid(), AssessmentId = polyExam1.Id, StudentId = p4.Id, Score = 9.0m }
        };

        // 5 sessions * 2 lessons = 10 total lessons for 7º Ano B Mathematics:
        // s2 has 2 unexcused absences -> 20% (Warning)
        // s5 has 4 unexcused absences -> 40% (Critical)
        // s4 has 2 justified absences -> 0% penalty (Normal)
        var sessionDates = new[]
        {
            new DateOnly(2026, 3, 2),
            new DateOnly(2026, 3, 4),
            new DateOnly(2026, 3, 6),
            new DateOnly(2026, 3, 9),
            new DateOnly(2026, 3, 11)
        };

        var topics = new[]
        {
            "Números inteiros e operações na reta numérica",
            "Expressões numéricas e propriedades",
            "Frações equivalentes e comparação",
            "Operações com números racionais",
            "Resolução de problemas com porcentagens"
        };

        var dailyActivities = new[]
        {
            "Exercícios em dupla com cartões numéricos",
            "Lista de fixação pág. 38 + correção comentada",
            "Oficina prática com tiras de frações",
            "Atividade em grupo: desafios com racionais",
            "Resolução de situações-problema do cotidiano"
        };

        var homeworks = new[]
        {
            "Exercícios 1 a 4 da pág. 35",
            "Concluir lista B no caderno",
            "Trazer recorte com porcentagem de ofertas",
            "Revisão para avaliação mensal",
            null
        };

        var lessonNotesList = new[]
        {
            "Boa participação geral da turma.",
            "Diego utilizou material dourado de apoio.",
            null,
            "Retomar simplificação de frações na próxima aula.",
            "Turma apresentou bom avanço em regra de três simples."
        };

        var sessions = new List<AttendanceSession>();
        for (int i = 0; i < sessionDates.Length; i++)
        {
            var sessionId = Guid.NewGuid();
            var session = new AttendanceSession
            {
                Id = sessionId,
                ClassroomId = seventhGradeId,
                SubjectId = mathSubjectId,
                Date = sessionDates[i],
                TermNumber = 1,
                LessonCount = 2,
                TopicCovered = topics[i],
                DailyActivity = dailyActivities[i],
                Homework = homeworks[i],
                LessonNotes = lessonNotesList[i]
            };

            session.Entries.Add(new AttendanceEntry { Id = Guid.NewGuid(), AttendanceSessionId = sessionId, StudentId = s1.Id, Status = AttendanceStatus.Present });
            session.Entries.Add(new AttendanceEntry { Id = Guid.NewGuid(), AttendanceSessionId = sessionId, StudentId = s2.Id, Status = i == 2 ? AttendanceStatus.Absent : AttendanceStatus.Present });
            session.Entries.Add(new AttendanceEntry { Id = Guid.NewGuid(), AttendanceSessionId = sessionId, StudentId = s3.Id, Status = AttendanceStatus.Present });
            session.Entries.Add(new AttendanceEntry { Id = Guid.NewGuid(), AttendanceSessionId = sessionId, StudentId = s4.Id, Status = i == 1 ? AttendanceStatus.Justified : AttendanceStatus.Present });
            session.Entries.Add(new AttendanceEntry { Id = Guid.NewGuid(), AttendanceSessionId = sessionId, StudentId = s5.Id, Status = i is 1 or 3 ? AttendanceStatus.Absent : AttendanceStatus.Present });
            session.Entries.Add(new AttendanceEntry { Id = Guid.NewGuid(), AttendanceSessionId = sessionId, StudentId = s6.Id, Status = AttendanceStatus.Present });

            sessions.Add(session);
        }

        var polySessionId = Guid.NewGuid();
        var polySession = new AttendanceSession
        {
            Id = polySessionId,
            ClassroomId = thirdGradeId,
            SubjectId = null,
            Date = new DateOnly(2026, 3, 11),
            TermNumber = 1,
            LessonCount = 1,
            TopicCovered = "Leitura compartilhada e sistema de numeração decimal",
            DailyActivity = "Roda de leitura + ditado numérico com material dourado",
            Homework = "Ficha de leitura para assinar em casa",
            LessonNotes = "Apoio visual entregue para Helena durante a atividade."
        };
        foreach (Student student in thirdStudents)
        {
            polySession.Entries.Add(new AttendanceEntry
            {
                Id = Guid.NewGuid(),
                AttendanceSessionId = polySessionId,
                StudentId = student.Id,
                Status = student.Id == p3.Id ? AttendanceStatus.Absent : AttendanceStatus.Present
            });
        }
        sessions.Add(polySession);

        var observations = new List<StudentObservation>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = s4.Id,
                Date = new DateOnly(2026, 3, 3),
                Category = ObservationCategory.Pedagogical,
                Content = "Apresentou excelente evolução ao utilizar material concreto nas operações com frações."
            },
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = s4.Id,
                Date = new DateOnly(2026, 3, 8),
                Category = ObservationCategory.Praise,
                Content = "Auxiliou os colegas durante a atividade em dupla com muita paciência."
            },
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = s4.Id,
                Date = new DateOnly(2026, 3, 10),
                Category = ObservationCategory.FamilyContact,
                Content = "Conversa com a mãe na saída; entrega de atestado referente ao dia 04/03."
            },
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = s5.Id,
                Date = new DateOnly(2026, 3, 9),
                Category = ObservationCategory.FamilyContact,
                Content = "Contato telefônico com o responsável sobre o acúmulo de faltas no mês."
            },
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = s2.Id,
                Date = new DateOnly(2026, 3, 7),
                Category = ObservationCategory.Behavioral,
                Content = "Dispersão frequente durante as explicações; realocado para a fileira da frente."
            }
        };

        var schoolDaysCalculator = new SchoolDaysCalculator();
        var calendarEvents = new List<SchoolCalendarEvent>(
            schoolDaysCalculator.CreateBrazilianNationalHolidays(academicYearId, 2026, GradingSystem.Semester));

        calendarEvents.Add(new SchoolCalendarEvent
        {
            Id = Guid.NewGuid(),
            AcademicYearId = academicYearId,
            Date = new DateOnly(2026, 3, 14),
            Type = SchoolCalendarEventType.SchoolSaturday,
            Title = "Sábado Letivo — Feira de Ciências",
            TermNumber = 1,
            Notes = "Reposição de aula e apresentação de projetos das turmas."
        });

        calendarEvents.Add(new SchoolCalendarEvent
        {
            Id = Guid.NewGuid(),
            AcademicYearId = academicYearId,
            Date = new DateOnly(2026, 6, 29),
            Type = SchoolCalendarEventType.PedagogicalDay,
            Title = "Conselho de Classe — 1º Semestre",
            TermNumber = 1,
            Notes = "Reunião pedagógica sem aula para os alunos."
        });

        var scheduleSlots = new List<ScheduleSlot>
        {
            new() { Id = Guid.NewGuid(), AcademicYearId = academicYearId, ClassroomId = seventhGradeId, SubjectId = mathSubjectId, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(9, 10), LessonCount = 2, RoomLabel = "Sala 07" },
            new() { Id = Guid.NewGuid(), AcademicYearId = academicYearId, ClassroomId = seventhGradeId, SubjectId = scienceSubjectId, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(9, 30), EndTime = new TimeOnly(11, 10), LessonCount = 2, RoomLabel = "Lab. Ciências" },
            new() { Id = Guid.NewGuid(), AcademicYearId = academicYearId, ClassroomId = seventhGradeId, SubjectId = mathSubjectId, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(9, 10), LessonCount = 2, RoomLabel = "Sala 07" },
            new() { Id = Guid.NewGuid(), AcademicYearId = academicYearId, ClassroomId = seventhGradeId, SubjectId = scienceSubjectId, DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeOnly(9, 30), EndTime = new TimeOnly(11, 10), LessonCount = 2, RoomLabel = "Sala 07" },
            new() { Id = Guid.NewGuid(), AcademicYearId = academicYearId, ClassroomId = seventhGradeId, SubjectId = mathSubjectId, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(9, 10), LessonCount = 2, RoomLabel = "Sala 07" }
        };

        foreach (DayOfWeek day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            scheduleSlots.Add(new ScheduleSlot
            {
                Id = Guid.NewGuid(),
                AcademicYearId = academicYearId,
                ClassroomId = thirdGradeId,
                SubjectId = null,
                DayOfWeek = day,
                StartTime = new TimeOnly(13, 30),
                EndTime = new TimeOnly(17, 30),
                LessonCount = 1,
                RoomLabel = "Sala 03"
            });
        }

        var lessonPlans = new List<LessonPlan>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ClassroomId = seventhGradeId,
                SubjectId = mathSubjectId,
                TermNumber = 1,
                PlannedDate = new DateOnly(2026, 3, 2),
                Title = "Números inteiros e operações na reta numérica",
                BnccCode = "EF07MA04",
                Objectives = "Resolver e elaborar problemas que envolvam operações com números inteiros.",
                PlannedActivity = "Exercícios em dupla com cartões numéricos",
                Homework = "Exercícios 1 a 4 da pág. 35",
                Status = LessonPlanStatus.Taught
            },
            new()
            {
                Id = Guid.NewGuid(),
                ClassroomId = seventhGradeId,
                SubjectId = mathSubjectId,
                TermNumber = 1,
                PlannedDate = new DateOnly(2026, 3, 18),
                Title = "Equações do 1º grau com uma incógnita",
                BnccCode = "EF07MA18",
                Objectives = "Compreender o princípio de equivalência utilizando balança de dois pratos.",
                PlannedActivity = "Atividade prática: desafios da balança algébrica",
                Homework = "Lista de problemas contextualizados pág. 52",
                Status = LessonPlanStatus.Planned
            },
            new()
            {
                Id = Guid.NewGuid(),
                ClassroomId = thirdGradeId,
                SubjectId = polyvalentSubjectId,
                TermNumber = 1,
                PlannedDate = new DateOnly(2026, 3, 19),
                Title = "Leitura compartilhada e ortografia contextual",
                BnccCode = "EF03LP01",
                Objectives = "Identificar relações entre fonemas e grafemas na leitura em voz alta.",
                PlannedActivity = "Roda de leitura + caça-palavras silábico",
                Homework = "Trazer pequena fábula marcada no caderno",
                Status = LessonPlanStatus.Planned
            }
        };

        context.AcademicYears.Add(academicYear);
        context.Classrooms.AddRange(seventhGrade, thirdGrade);
        context.Subjects.AddRange(subjects);
        context.Students.AddRange(seventhStudents);
        context.Students.AddRange(thirdStudents);
        context.Assessments.AddRange(mathExam1, mathProject1, mathRec1, mathExam2, scienceExam1, polyExam1);
        context.Grades.AddRange(grades);
        context.AttendanceSessions.AddRange(sessions);
        context.StudentObservations.AddRange(observations);
        context.SchoolCalendarEvents.AddRange(calendarEvents);
        context.ScheduleSlots.AddRange(scheduleSlots);
        context.LessonPlans.AddRange(lessonPlans);

        await context.SaveChangesAsync(ct);
    }
}
