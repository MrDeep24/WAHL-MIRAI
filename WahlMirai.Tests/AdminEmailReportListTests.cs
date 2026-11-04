// ============================================================
// Archivo:       AdminEmailReportListTests.cs
// Módulo:        Panel Administrativo — Reporte de Notificaciones
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias para paginación server-side, filtros,
//                métricas KPI y orden determinista del reporte de correos (RN-9).
// Requerimientos: RN-9
// ============================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using Xunit;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias de consulta y paginación server-side en AdminEmailReportController.Index (RN-9).
/// Valida acotamiento de parámetros, orden determinista, filtros combinados y desacoplamiento de KPIs.
/// </summary>
public class AdminEmailReportListTests
{
    // NOTA: Se utiliza el proveedor InMemory de Entity Framework Core al igual que en PqrListTests.
    // Riesgo de falso verde / divergencia con MySQL utf8mb4_unicode_ci:
    // En EF InMemory, la búsqueda con .ToLower().Contains() evalúa mediante las reglas ordinales de C#,
    // mientras que en MySQL con colación utf8mb4_unicode_ci la comparación es nativamente insensible
    // a mayúsculas, minúsculas y acentos. Las pruebas validan equivalencia directa en caracteres ASCII y diacríticos estándar.
    private static (AdminEmailReportController controller, WahlMiraiDbContext context) CreateController()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new WahlMiraiDbContext(options);
        var auditService = new FakeAuditService();
        var controller = new AdminEmailReportController(context, auditService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return (controller, context);
    }

    private static Voter CreateVoter(WahlMiraiDbContext context, uint id, string name, string email)
    {
        var voter = new Voter
        {
            Id = id,
            FullName = name,
            ContactEmail = email,
            GradeId = 1,
            RoleId = 3,
            Status = "ACTIVO",
            DocumentHash = $"hash-{id}",
            EncryptedDocument = $"enc-{id}",
            PasswordHash = "hash"
        };
        context.Voters.Add(voter);
        return voter;
    }

    private static EmailReportViewModel ExtractModel(IActionResult result)
    {
        var viewResult = Assert.IsType<ViewResult>(result);
        return Assert.IsType<EmailReportViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Index_FirstPage_ReturnsExpectedPageSizeAndTotalCount()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Prueba", "elector1@colegio.edu.co");

        for (ulong i = 1; i <= 25; i++)
        {
            context.EmailQueues.Add(new EmailQueue
            {
                Id = i,
                VoterId = voter.Id,
                Voter = voter,
                EmailType = "RESPUESTA_PQR",
                Status = "ENVIADO",
                Attempts = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes((double)i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: null, page: 1, pageSize: 20);

        // Assert
        var model = ExtractModel(result);
        Assert.Equal(25, model.PagedItems.TotalCount);
        Assert.Equal(1, model.PagedItems.PageNumber);
        Assert.Equal(20, model.PagedItems.PageSize);
        Assert.Equal(2, model.PagedItems.TotalPages);
        Assert.False(model.PagedItems.HasPreviousPage);
        Assert.True(model.PagedItems.HasNextPage);
        Assert.Equal(20, model.Items.Count);
    }

    [Fact]
    public async Task Index_SecondPage_AppliesCorrectOffsetWithoutIdOverlap()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Desplazamiento", "offset@colegio.edu.co");

        for (ulong i = 1; i <= 25; i++)
        {
            context.EmailQueues.Add(new EmailQueue
            {
                Id = i,
                VoterId = voter.Id,
                Voter = voter,
                EmailType = "CAMBIO_PERFIL",
                Status = "ENVIADO",
                Attempts = 1,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes((double)i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var res1 = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: null, page: 1, pageSize: 20);
        var model1 = ExtractModel(res1);

        var res2 = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: null, page: 2, pageSize: 20);
        var model2 = ExtractModel(res2);

        // Assert
        var idsPage1 = model1.Items.Select(x => x.Id).ToList();
        var idsPage2 = model2.Items.Select(x => x.Id).ToList();

        Assert.Equal(20, idsPage1.Count);
        Assert.Equal(5, idsPage2.Count);
        Assert.Equal(2, model2.PagedItems.PageNumber);
        Assert.True(model2.PagedItems.HasPreviousPage);
        Assert.False(model2.PagedItems.HasNextPage);
        Assert.Empty(idsPage1.Intersect(idsPage2));
    }

    [Fact]
    public async Task Index_PageSizeClamped_BoundsZeroAndOneHundred()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Clamping", "clamp@colegio.edu.co");

        for (ulong i = 1; i <= 150; i++)
        {
            context.EmailQueues.Add(new EmailQueue
            {
                Id = i,
                VoterId = voter.Id,
                Voter = voter,
                EmailType = "RESPUESTA_PQR",
                Status = "ENVIADO",
                Attempts = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes((double)i)
            });
        }
        await context.SaveChangesAsync();

        // Act: pageSize <= 0 debe acotarse a 1; pageSize > 100 debe acotarse a 100
        var resZero = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: null, page: 1, pageSize: 0);
        var modelZero = ExtractModel(resZero);

        var resThousand = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: null, page: 1, pageSize: 1000);
        var modelThousand = ExtractModel(resThousand);

        // Assert
        Assert.Equal(1, modelZero.PagedItems.PageSize);
        Assert.Single(modelZero.Items);

        Assert.Equal(100, modelThousand.PagedItems.PageSize);
        Assert.Equal(100, modelThousand.Items.Count);
    }

    [Fact]
    public async Task Index_PageBeyondLast_IsClampedToTotalPages()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Exceso", "exceso@colegio.edu.co");

        for (ulong i = 1; i <= 25; i++)
        {
            context.EmailQueues.Add(new EmailQueue
            {
                Id = i,
                VoterId = voter.Id,
                Voter = voter,
                EmailType = "RESPUESTA_PQR",
                Status = "ENVIADO",
                Attempts = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes((double)i)
            });
        }
        await context.SaveChangesAsync();

        // Act: Con 25 items y pageSize 20 existen 2 páginas; se solicita página 99
        var result = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: null, page: 99, pageSize: 20);

        // Assert
        var model = ExtractModel(result);
        Assert.Equal(2, model.PagedItems.PageNumber);
        Assert.Equal(5, model.Items.Count);
        Assert.Equal(2, model.PagedItems.TotalPages);
    }

    [Fact]
    public async Task Index_Ordering_CreatedAtDescWithIdTieBreak()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Orden", "orden@colegio.edu.co");

        var dtOlder = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var dtSameTime = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var dtNewer = new DateTime(2026, 5, 1, 15, 0, 0, DateTimeKind.Utc);

        context.EmailQueues.AddRange(
            new EmailQueue { Id = 10, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = dtOlder },
            new EmailQueue { Id = 20, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = dtSameTime },
            new EmailQueue { Id = 30, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = dtSameTime }, // Mismo CreatedAt, mayor Id
            new EmailQueue { Id = 5,  VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = dtNewer }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: null, page: 1, pageSize: 10);

        // Assert
        // Orden determinista esperado: Id 5 (15:00) -> Id 30 (12:00 desempate DESC) -> Id 20 (12:00) -> Id 10 (08:00)
        var model = ExtractModel(result);
        var ids = model.Items.Select(x => x.Id).ToList();
        Assert.Equal(new ulong[] { 5, 30, 20, 10 }, ids);
    }

    [Fact]
    public async Task Index_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Estado", "estado@colegio.edu.co");

        context.EmailQueues.AddRange(
            new EmailQueue { Id = 1, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = DateTime.UtcNow },
            new EmailQueue { Id = 2, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "FALLIDO", CreatedAt = DateTime.UtcNow },
            new EmailQueue { Id = 3, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "PENDIENTE", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var res = await controller.Index(status: "FALLIDO", emailType: null, startDate: null, endDate: null, search: null, page: 1, pageSize: 10);

        // Assert
        var model = ExtractModel(res);
        Assert.Equal(1, model.PagedItems.TotalCount);
        Assert.Single(model.Items);
        Assert.Equal("FALLIDO", model.Items[0].Status);
    }

    [Fact]
    public async Task Index_FilterByEmailType_ReturnsOnlyMatchingType()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Tipo", "tipo@colegio.edu.co");

        context.EmailQueues.AddRange(
            new EmailQueue { Id = 1, VoterId = voter.Id, Voter = voter, EmailType = "RESPUESTA_PQR", Status = "ENVIADO", CreatedAt = DateTime.UtcNow },
            new EmailQueue { Id = 2, VoterId = voter.Id, Voter = voter, EmailType = "CAMBIO_PERFIL", Status = "ENVIADO", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var res = await controller.Index(status: null, emailType: "CAMBIO_PERFIL", startDate: null, endDate: null, search: null, page: 1, pageSize: 10);

        // Assert
        var model = ExtractModel(res);
        Assert.Equal(1, model.PagedItems.TotalCount);
        Assert.Equal("CAMBIO_PERFIL", model.Items[0].EmailType);
    }

    [Fact]
    public async Task Index_FilterByInclusiveEndDate_IncludesEndOfDay()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Fechas", "fechas@colegio.edu.co");

        var baseDay = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        context.EmailQueues.AddRange(
            new EmailQueue { Id = 1, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = baseDay.AddHours(23).AddMinutes(59) }, // Dentro del día
            new EmailQueue { Id = 2, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = baseDay.AddDays(1).AddSeconds(1) } // Día siguiente
        );
        await context.SaveChangesAsync();

        // Act: Filtro endDate = 2026-06-10 debe incluir el registro creado a las 23:59
        var res = await controller.Index(status: null, emailType: null, startDate: null, endDate: baseDay, search: null, page: 1, pageSize: 10);

        // Assert
        var model = ExtractModel(res);
        Assert.Equal(1, model.PagedItems.TotalCount);
        Assert.Equal((ulong)1, model.Items[0].Id);
    }

    [Fact]
    public async Task Index_FilterDateRange_SwapsWhenStartDateGreaterThanEndDate()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Rango", "rango@colegio.edu.co");

        var d1 = new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);
        var d2 = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);
        var d3 = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);

        context.EmailQueues.AddRange(
            new EmailQueue { Id = 1, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = d1 },
            new EmailQueue { Id = 2, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = d2 },
            new EmailQueue { Id = 3, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = d3 }
        );
        await context.SaveChangesAsync();

        // Act: startDate es 2026-07-12 y endDate es 2026-07-08 (startDate > endDate).
        // El controlador intercambia el rango a [2026-07-08, 2026-07-12], encontrando únicamente d2.
        var res = await controller.Index(
            status: null,
            emailType: null,
            startDate: new DateTime(2026, 7, 12),
            endDate: new DateTime(2026, 7, 8),
            search: null,
            page: 1,
            pageSize: 10);

        // Assert
        var model = ExtractModel(res);
        Assert.Equal(1, model.PagedItems.TotalCount);
        Assert.Equal((ulong)2, model.Items[0].Id);
        Assert.Equal(new DateTime(2026, 7, 8), model.StartDate);
        Assert.Equal(new DateTime(2026, 7, 12), model.EndDate);
    }

    [Fact]
    public async Task Index_SearchByVoterNameAndEmail_IsCaseInsensitive()
    {
        // Arrange
        var (controller, context) = CreateController();
        var v1 = CreateVoter(context, 1, "Valentina Ríos", "vrios@institucion.edu.co");
        var v2 = CreateVoter(context, 2, "Santiago Gómez", "sgomez@institucion.edu.co");

        context.EmailQueues.AddRange(
            new EmailQueue { Id = 1, VoterId = v1.Id, Voter = v1, EmailType = "A", Status = "ENVIADO", CreatedAt = DateTime.UtcNow },
            new EmailQueue { Id = 2, VoterId = v2.Id, Voter = v2, EmailType = "A", Status = "ENVIADO", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act: Búsqueda insensible en nombre y en correo
        var resNameUpper = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: "RÍOS", page: 1, pageSize: 10);
        var modelName = ExtractModel(resNameUpper);

        var resEmailLower = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: "sgomez", page: 1, pageSize: 10);
        var modelEmail = ExtractModel(resEmailLower);

        // Assert
        Assert.Equal(1, modelName.PagedItems.TotalCount);
        Assert.Equal("Valentina Ríos", modelName.Items[0].VoterName);

        Assert.Equal(1, modelEmail.PagedItems.TotalCount);
        Assert.Equal("Santiago Gómez", modelEmail.Items[0].VoterName);
    }

    [Fact]
    public async Task Index_CombinedFilters_FiltersCorrectly()
    {
        // Arrange
        var (controller, context) = CreateController();
        var v1 = CreateVoter(context, 1, "Camila Torres", "ctorres@col.edu.co");
        var v2 = CreateVoter(context, 2, "Camila Morales", "cmorales@col.edu.co");

        var dt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        context.EmailQueues.AddRange(
            new EmailQueue { Id = 1, VoterId = v1.Id, Voter = v1, EmailType = "RESPUESTA_PQR", Status = "ENVIADO", CreatedAt = dt },
            new EmailQueue { Id = 2, VoterId = v1.Id, Voter = v1, EmailType = "CAMBIO_PERFIL", Status = "FALLIDO", CreatedAt = dt },
            new EmailQueue { Id = 3, VoterId = v2.Id, Voter = v2, EmailType = "RESPUESTA_PQR", Status = "ENVIADO", CreatedAt = dt }
        );
        await context.SaveChangesAsync();

        // Act: Filtro status=ENVIADO + emailType=RESPUESTA_PQR + search=Torres -> solo Id 1
        var res = await controller.Index(
            status: "ENVIADO",
            emailType: "RESPUESTA_PQR",
            startDate: dt.Date,
            endDate: dt.Date,
            search: "Torres",
            page: 1,
            pageSize: 10);

        // Assert
        var model = ExtractModel(res);
        Assert.Equal(1, model.PagedItems.TotalCount);
        Assert.Equal((ulong)1, model.Items[0].Id);
    }

    [Fact]
    public async Task Index_KpiCounts_AreIndependentOfPageAndStatusFilter()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector KPIs", "kpis@colegio.edu.co");

        // 5 ENVIADO, 3 FALLIDO, 2 PENDIENTE = 10 Total
        for (ulong i = 1; i <= 5; i++)
        {
            context.EmailQueues.Add(new EmailQueue { Id = i, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = DateTime.UtcNow });
        }
        for (ulong i = 6; i <= 8; i++)
        {
            context.EmailQueues.Add(new EmailQueue { Id = i, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "FALLIDO", CreatedAt = DateTime.UtcNow });
        }
        for (ulong i = 9; i <= 10; i++)
        {
            context.EmailQueues.Add(new EmailQueue { Id = i, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "PENDIENTE", CreatedAt = DateTime.UtcNow });
        }
        await context.SaveChangesAsync();

        // Act: Se filtra por status = "FALLIDO" con pageSize = 2 (deberían verse 2 items de 3 fallidos).
        // Los KPIs deben reflejar el resumen de TODOS los estados (10 total, 5 enviados, 3 fallidos, 2 pendientes).
        var res = await controller.Index(status: "FALLIDO", emailType: null, startDate: null, endDate: null, search: null, page: 1, pageSize: 2);

        // Assert
        var model = ExtractModel(res);
        Assert.Equal(3, model.PagedItems.TotalCount); // Total de fallidos paginados
        Assert.Equal(2, model.Items.Count); // Filas en la página actual
        Assert.Equal(10, model.TotalEmails);
        Assert.Equal(5, model.SentEmails);
        Assert.Equal(3, model.FailedEmails);
        Assert.Equal(2, model.PendingEmails);
        // % Éxito: 5 enviados / (5 enviados + 3 fallidos) = 5/8 = 62.5%
        Assert.Equal(62.5, model.SuccessPercentage);
    }

    [Fact]
    public async Task Index_LastPageEmpties_ClampsToNewLastPage()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Reintento", "reintento@colegio.edu.co");

        // 20 registros en total (con pageSize = 20 esto da exactamente 1 página)
        for (ulong i = 1; i <= 20; i++)
        {
            context.EmailQueues.Add(new EmailQueue
            {
                Id = i,
                VoterId = voter.Id,
                Voter = voter,
                EmailType = "A",
                Status = "ENVIADO",
                CreatedAt = DateTime.UtcNow.AddMinutes((double)i)
            });
        }
        await context.SaveChangesAsync();

        // Act: Petición solicitando la página 2 (que quedó vacía tras el reintento de la fila 21)
        var result = await controller.Index(status: null, emailType: null, startDate: null, endDate: null, search: null, page: 2, pageSize: 20);

        // Assert: El clamping debe retornar automáticamente la página 1 con los 20 registros
        var model = ExtractModel(result);
        Assert.Equal(1, model.PagedItems.PageNumber);
        Assert.Equal(20, model.Items.Count);
        Assert.Equal(1, model.PagedItems.TotalPages);
    }

    [Fact]
    public async Task Index_Kpis_ReflectEmailTypeFilter()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector EmailType KPI", "kpi_emailtype@col.edu.co");

        // 2 correos CAMBIO_PERFIL: 1 ENVIADO, 1 FALLIDO
        context.EmailQueues.Add(new EmailQueue { Id = 1, VoterId = voter.Id, Voter = voter, EmailType = "CAMBIO_PERFIL", Status = "ENVIADO", CreatedAt = DateTime.UtcNow });
        context.EmailQueues.Add(new EmailQueue { Id = 2, VoterId = voter.Id, Voter = voter, EmailType = "CAMBIO_PERFIL", Status = "FALLIDO", CreatedAt = DateTime.UtcNow });

        // 3 correos RESPUESTA_PQR: 2 ENVIADO, 1 PENDIENTE
        context.EmailQueues.Add(new EmailQueue { Id = 3, VoterId = voter.Id, Voter = voter, EmailType = "RESPUESTA_PQR", Status = "ENVIADO", CreatedAt = DateTime.UtcNow });
        context.EmailQueues.Add(new EmailQueue { Id = 4, VoterId = voter.Id, Voter = voter, EmailType = "RESPUESTA_PQR", Status = "ENVIADO", CreatedAt = DateTime.UtcNow });
        context.EmailQueues.Add(new EmailQueue { Id = 5, VoterId = voter.Id, Voter = voter, EmailType = "RESPUESTA_PQR", Status = "PENDIENTE", CreatedAt = DateTime.UtcNow });

        await context.SaveChangesAsync();

        // Act: Filtrar por emailType = "CAMBIO_PERFIL" y status = "FALLIDO"
        var result = await controller.Index(status: "FALLIDO", emailType: "CAMBIO_PERFIL", startDate: null, endDate: null, search: null, page: 1, pageSize: 10);

        // Assert:
        // Los KPIs deben calcularse sobre la consulta filtrada por emailType (excluyendo solo el filtro status):
        // Total = 2, Enviados = 1, Fallidos = 1, Pendientes = 0, % Éxito = 50.0%
        var model = ExtractModel(result);
        Assert.Equal(2, model.TotalEmails);
        Assert.Equal(1, model.SentEmails);
        Assert.Equal(1, model.FailedEmails);
        Assert.Equal(0, model.PendingEmails);
        Assert.Equal(50.0, model.SuccessPercentage);

        // La tabla paginada sí debe aplicar también el filtro de status (solo 1 fila FALLIDO)
        Assert.Equal(1, model.PagedItems.TotalCount);
        Assert.Single(model.Items);
        Assert.Equal((ulong)2, model.Items[0].Id);
    }

    [Fact]
    public async Task Index_Kpis_ReflectSearchFilter()
    {
        // Arrange
        var (controller, context) = CreateController();
        var v1 = CreateVoter(context, 1, "Carlos Gómez", "cgomez@col.edu.co");
        var v2 = CreateVoter(context, 2, "María López", "mlopez@col.edu.co");

        // Carlos Gómez: 1 ENVIADO, 1 FALLIDO
        context.EmailQueues.Add(new EmailQueue { Id = 1, VoterId = v1.Id, Voter = v1, EmailType = "A", Status = "ENVIADO", CreatedAt = DateTime.UtcNow });
        context.EmailQueues.Add(new EmailQueue { Id = 2, VoterId = v1.Id, Voter = v1, EmailType = "A", Status = "FALLIDO", CreatedAt = DateTime.UtcNow });

        // María López: 2 ENVIADO, 1 PENDIENTE
        context.EmailQueues.Add(new EmailQueue { Id = 3, VoterId = v2.Id, Voter = v2, EmailType = "A", Status = "ENVIADO", CreatedAt = DateTime.UtcNow });
        context.EmailQueues.Add(new EmailQueue { Id = 4, VoterId = v2.Id, Voter = v2, EmailType = "A", Status = "ENVIADO", CreatedAt = DateTime.UtcNow });
        context.EmailQueues.Add(new EmailQueue { Id = 5, VoterId = v2.Id, Voter = v2, EmailType = "A", Status = "PENDIENTE", CreatedAt = DateTime.UtcNow });

        await context.SaveChangesAsync();

        // Act: Búsqueda search = "Gómez" y status = "FALLIDO"
        var result = await controller.Index(status: "FALLIDO", emailType: null, startDate: null, endDate: null, search: "Gómez", page: 1, pageSize: 10);

        // Assert:
        // Los KPIs deben reflejar únicamente las filas de Carlos Gómez (excluyendo solo el filtro status):
        // Total = 2, Enviados = 1, Fallidos = 1, Pendientes = 0, % Éxito = 50.0%
        var model = ExtractModel(result);
        Assert.Equal(2, model.TotalEmails);
        Assert.Equal(1, model.SentEmails);
        Assert.Equal(1, model.FailedEmails);
        Assert.Equal(0, model.PendingEmails);
        Assert.Equal(50.0, model.SuccessPercentage);

        // La tabla paginada aplica además status = "FALLIDO"
        Assert.Equal(1, model.PagedItems.TotalCount);
        Assert.Single(model.Items);
        Assert.Equal((ulong)2, model.Items[0].Id);
    }

    [Fact]
    public async Task Index_Kpis_ReflectDateRangeFilter()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Rango KPI", "rango_kpi@col.edu.co");

        var day1 = new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);

        // Day 1: 1 ENVIADO, 1 FALLIDO
        context.EmailQueues.Add(new EmailQueue { Id = 1, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = day1 });
        context.EmailQueues.Add(new EmailQueue { Id = 2, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "FALLIDO", CreatedAt = day1 });

        // Day 2: 2 ENVIADO, 1 PENDIENTE
        context.EmailQueues.Add(new EmailQueue { Id = 3, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = day2 });
        context.EmailQueues.Add(new EmailQueue { Id = 4, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "ENVIADO", CreatedAt = day2 });
        context.EmailQueues.Add(new EmailQueue { Id = 5, VoterId = voter.Id, Voter = voter, EmailType = "A", Status = "PENDIENTE", CreatedAt = day2 });

        await context.SaveChangesAsync();

        // Act: Filtrar por rango de fechas cubriendo únicamente day1, y status = "FALLIDO"
        var result = await controller.Index(status: "FALLIDO", emailType: null, startDate: day1.Date, endDate: day1.Date, search: null, page: 1, pageSize: 10);

        // Assert:
        // Los KPIs deben reflejar únicamente los registros de day1 (excluyendo solo el filtro status):
        // Total = 2, Enviados = 1, Fallidos = 1, Pendientes = 0, % Éxito = 50.0%
        var model = ExtractModel(result);
        Assert.Equal(2, model.TotalEmails);
        Assert.Equal(1, model.SentEmails);
        Assert.Equal(1, model.FailedEmails);
        Assert.Equal(0, model.PendingEmails);
        Assert.Equal(50.0, model.SuccessPercentage);

        // La tabla paginada aplica además status = "FALLIDO"
        Assert.Equal(1, model.PagedItems.TotalCount);
        Assert.Single(model.Items);
        Assert.Equal((ulong)2, model.Items[0].Id);
    }
}

