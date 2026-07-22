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
            var grade11 = context.Grades.FirstOrDefault(g => g.Name == "11°");
            var grade9 = context.Grades.FirstOrDefault(g => g.Name == "9°");

            if (adminRole == null || electorRole == null) return;

            // Admin
            var adminDoc = "admin.electoral";
            var admin = new Voter
            {
                RoleId = adminRole.Id,
                GradeId = null,
                DocumentHash = HashDocument(adminDoc),
                EncryptedDocument = adminDoc,
                FullName = "Coordinación Electoral SENA",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                RequiereCambioClave = false,
                ExcluirDePromocion = false,
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            };
            context.Voters.Add(admin);
            context.SaveChanges();

            // Elector 1 (Active, already changed password)
            var student1Doc = "1001234567";
            var student1 = new Voter
            {
                RoleId = electorRole.Id,
                GradeId = grade11?.Id,
                DocumentHash = HashDocument(student1Doc),
                EncryptedDocument = student1Doc,
                FullName = "Ana María López Pérez",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("estudiante123"),
                RequiereCambioClave = false,
                ExcluirDePromocion = false,
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            };
            context.Voters.Add(student1);

            // Elector 2 (New student, initial password document.year, forces change)
            var student2Doc = "1007654321";
            var student2 = new Voter
            {
                RoleId = electorRole.Id,
                GradeId = grade9?.Id,
                DocumentHash = HashDocument(student2Doc),
                EncryptedDocument = student2Doc,
                FullName = "Andrés Felipe Martínez",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("1007654321.2026"),
                RequiereCambioClave = true,
                ExcluirDePromocion = false,
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            };
            context.Voters.Add(student2);
            context.SaveChanges();

            // 3. Seed sample voting event if none exists
            if (!context.VotingEvents.Any())
            {
                var votingEvent = new VotingEvent
                {
                    CreatedByVoterId = admin.Id,
                    Title = "Personería Estudiantil 2026",
                    Description = "Elección del Personero Estudiantil para el año lectivo en curso.",
                    ElectionType = "PERSONAS",
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    StartTime = new TimeOnly(8, 0),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    EndTime = new TimeOnly(18, 0),
                    Status = "ACTIVA",
                    CreatedAt = DateTime.UtcNow
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
                        GradeId = g.Id
                    });
                }

                // Seed candidates
                var cand1 = new Candidate
                {
                    VotingEventId = votingEvent.Id,
                    VoterId = student1.Id,
                    Name = "Ana María López Pérez",
                    Slogan = "Liderazgo, transparencia y unión estudiantil",
                    PhotoUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300",
                    IsBlankVote = false,
                    Status = "APROBADO",
                    EnrolledAt = DateTime.UtcNow
                };
                context.Candidates.Add(cand1);

                var cand2 = new Candidate
                {
                    VotingEventId = votingEvent.Id,
                    VoterId = student2.Id,
                    Name = "Andrés Felipe Martínez",
                    Slogan = "Innovación tecnológica para nuestro colegio",
                    PhotoUrl = "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=300",
                    IsBlankVote = false,
                    Status = "APROBADO",
                    EnrolledAt = DateTime.UtcNow
                };
                context.Candidates.Add(cand2);

                var candBlank = new Candidate
                {
                    VotingEventId = votingEvent.Id,
                    VoterId = null,
                    Name = "Voto en Blanco",
                    Slogan = "Opción de abstención o desacuerdo formal",
                    PhotoUrl = null,
                    IsBlankVote = true,
                    Status = "APROBADO",
                    EnrolledAt = DateTime.UtcNow
                };
                context.Candidates.Add(candBlank);
                context.SaveChanges();

                // Proposals for candidate 1
                context.CandidateProposals.Add(new CandidateProposal { CandidateId = cand1.Id, Content = "Mejoramiento de las zonas de descanso y cafetería", DisplayOrder = 1 });
                context.CandidateProposals.Add(new CandidateProposal { CandidateId = cand1.Id, Content = "Implementación de torneos interclases mensuales", DisplayOrder = 2 });

                // Proposals for candidate 2
                context.CandidateProposals.Add(new CandidateProposal { CandidateId = cand2.Id, Content = "Digitalización del carnet estudiantil y la biblioteca", DisplayOrder = 1 });
                context.CandidateProposals.Add(new CandidateProposal { CandidateId = cand2.Id, Content = "Talleres de programación y habilidades digitales gratis", DisplayOrder = 2 });

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
