using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Data;

public static class DbInitializer
{
    public static void Initialize(WahlMiraiDbContext context)
    {
        // 1. Seed current Academic Year if missing
        if (!context.AcademicYears.Any())
        {
            context.AcademicYears.Add(new AcademicYear
            {
                Year = 2026,
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        // 2. Seed Voters if table is empty
        if (!context.Voters.Any())
        {
            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "ADMIN");
            var electorRole = context.Roles.FirstOrDefault(r => r.Name == "ELECTOR");
            // grade_id = 6 → 11° (is_last_grade = 1), per seed order in SQL v2.3
            var grade11 = context.Grades.FirstOrDefault(g => g.Name == "11°");

            if (adminRole == null || electorRole == null) return;

            // --- ADMIN ---
            // Documento:  admin.electoral
            // Contraseña: Admin#2026!
            var adminDoc = "admin.electoral";
            var admin = new Voter
            {
                RoleId            = adminRole.Id,
                GradeId           = null,
                DocumentHash      = HashDocument(adminDoc),
                EncryptedDocument = adminDoc,
                FullName          = "Coordinación Electoral",
                ContactEmail      = "coordinacion.electoral@colegio.edu.co",
                PasswordHash      = BCrypt.Net.BCrypt.HashPassword("Admin#2026!"),
                ExcluirDePromocion = false,
                Status            = "ACTIVO",
                RegisteredAt      = DateTime.UtcNow
            };
            context.Voters.Add(admin);
            context.SaveChanges();

            // --- ELECTOR DE PRUEBA ---
            // Documento:  1001234567
            // Contraseña: 1001234567.2026  (ejemplo; producción envía clave aleatoria por correo — RN-2)
            var student1Doc = "1001234567";
            var student1 = new Voter
            {
                RoleId            = electorRole.Id,
                GradeId           = grade11?.Id,
                DocumentHash      = HashDocument(student1Doc),
                EncryptedDocument = student1Doc,
                FullName          = "Ana María López Pérez",
                ContactEmail      = "acudiente.ana.lopez@example.com",
                PasswordHash      = BCrypt.Net.BCrypt.HashPassword("1001234567.2026"),
                ExcluirDePromocion = false,
                Status            = "ACTIVO",
                RegisteredAt      = DateTime.UtcNow
            };
            context.Voters.Add(student1);
            context.SaveChanges();

            // 3. Seed sample voting event if none exists
            if (!context.VotingEvents.Any())
            {
                var votingEvent = new VotingEvent
                {
                    CreatedByVoterId = admin.Id,
                    Title            = "Personería Estudiantil 2026",
                    Description      = "Elección del Personero Estudiantil para el año lectivo en curso.",
                    ElectionType     = "PERSONAS",
                    StartDate        = DateOnly.FromDateTime(DateTime.Today),
                    StartTime        = new TimeOnly(8, 0),
                    EndDate          = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    EndTime          = new TimeOnly(18, 0),
                    Status           = "ACTIVA",
                    CreatedAt        = DateTime.UtcNow
                };
                context.VotingEvents.Add(votingEvent);
                context.SaveChanges();

                // Enable event for all grades
                var allGrades = context.Grades.ToList();
                foreach (var g in allGrades)
                {
                    context.EventGrades.Add(new EventGrade
                    {
                        VotingEventId = votingEvent.Id,
                        GradeId       = g.Id
                    });
                }

                // Seed candidate
                var cand1 = new Candidate
                {
                    VotingEventId = votingEvent.Id,
                    VoterId       = student1.Id,
                    Name          = "Ana María López Pérez",
                    Slogan        = "Liderazgo, transparencia y unión estudiantil",
                    PhotoUrl      = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300",
                    IsBlankVote   = false,
                    Status        = "APROBADO",
                    EnrolledAt    = DateTime.UtcNow
                };
                context.Candidates.Add(cand1);

                var candBlank = new Candidate
                {
                    VotingEventId = votingEvent.Id,
                    VoterId       = null,
                    Name          = "Voto en Blanco",
                    Slogan        = "Opción de abstención o desacuerdo formal",
                    PhotoUrl      = null,
                    IsBlankVote   = true,
                    Status        = "APROBADO",
                    EnrolledAt    = DateTime.UtcNow
                };
                context.Candidates.Add(candBlank);
                context.SaveChanges();

                // Proposals for candidate 1
                context.CandidateProposals.Add(new CandidateProposal { CandidateId = cand1.Id, Content = "Mejoramiento de las zonas de descanso y cafetería", DisplayOrder = 1 });
                context.CandidateProposals.Add(new CandidateProposal { CandidateId = cand1.Id, Content = "Implementación de torneos interclases mensuales", DisplayOrder = 2 });

                context.SaveChanges();
            }
        }
    }

    private static string HashDocument(string document)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(document));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}
