// Archivo:       PqrListTests.cs
// Módulo:        M08 — Ayuda, Tutorial, PQR y Chatbot
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias para la paginación, búsqueda, ordenamiento y filtrado server-side de PQR (RF-M08-02).
// Requerimientos: RF-M08-02.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using Xunit;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias del endpoint administrativo PqrController.List (RF-M08-02).
/// Valida paginación server-side, límites, filtros, ordenamiento determinista y búsqueda.
/// </summary>
public class PqrListTests
{
    private static (PqrController controller, WahlMiraiDbContext context) CreateController()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new WahlMiraiDbContext(options);
        var controller = new PqrController(context);
        return (controller, context);
    }

    private static JsonElement UnpackJson(IActionResult result)
    {
        var jsonResult = Assert.IsType<JsonResult>(result);
        var json = JsonSerializer.Serialize(jsonResult.Value);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static Voter CreateVoter(WahlMiraiDbContext context, uint id, string name)
    {
        var voter = new Voter
        {
            Id = id,
            FullName = name,
            GradeId = 1,
            RoleId = 3,
            Status = "ACTIVO",
            DocumentHash = $"hash-{id}",
            EncryptedDocument = $"enc-{id}",
            ContactEmail = $"voter{id}@test.com",
            PasswordHash = "hash"
        };
        context.Voters.Add(voter);
        return voter;
    }

    [Fact]
    public async Task List_FirstPage_ReturnsExpectedPageSizeAndTotalCount()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Juan Pérez");

        for (uint i = 1; i <= 25; i++)
        {
            context.PqrTickets.Add(new PqrTicket
            {
                Id = i,
                UserId = voter.Id,
                Voter = voter,
                Subject = $"Asunto {i}",
                Message = $"Mensaje {i}",
                Status = "ABIERTO",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await controller.List(status: null, search: null, page: 1, pageSize: 20);

        // Assert
        var root = UnpackJson(result);
        Assert.True(root.GetProperty("ok").GetBoolean());
        Assert.Equal(25, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, root.GetProperty("pageNumber").GetInt32());
        Assert.Equal(20, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, root.GetProperty("totalPages").GetInt32());
        Assert.False(root.GetProperty("hasPreviousPage").GetBoolean());
        Assert.True(root.GetProperty("hasNextPage").GetBoolean());

        var items = root.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(20, items.Count);
    }

    [Fact]
    public async Task List_SecondPage_AppliesCorrectOffset()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Estudiante Prueba");

        for (uint i = 1; i <= 25; i++)
        {
            context.PqrTickets.Add(new PqrTicket
            {
                Id = i,
                UserId = voter.Id,
                Voter = voter,
                Subject = $"Ticket #{i}",
                Message = $"Contenido #{i}",
                Status = "ABIERTO",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var resultPage1 = await controller.List(status: null, search: null, page: 1, pageSize: 20);
        var resultPage2 = await controller.List(status: null, search: null, page: 2, pageSize: 20);

        // Assert
        var root1 = UnpackJson(resultPage1);
        var root2 = UnpackJson(resultPage2);

        var items1 = root1.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetUInt64()).ToList();
        var items2 = root2.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetUInt64()).ToList();

        Assert.Equal(20, items1.Count);
        Assert.Equal(5, items2.Count);
        Assert.Equal(2, root2.GetProperty("pageNumber").GetInt32());
        Assert.True(root2.GetProperty("hasPreviousPage").GetBoolean());
        Assert.False(root2.GetProperty("hasNextPage").GetBoolean());

        // Asegurar que no hay traslape entre página 1 y página 2
        Assert.Empty(items1.Intersect(items2));
    }

    [Fact]
    public async Task List_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Carlos Mora");

        for (uint i = 1; i <= 10; i++)
        {
            context.PqrTickets.Add(new PqrTicket
            {
                Id = i,
                UserId = voter.Id,
                Voter = voter,
                Subject = $"Ticket Abierto {i}",
                Message = "Mensaje",
                Status = "ABIERTO",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }

        for (uint i = 11; i <= 15; i++)
        {
            context.PqrTickets.Add(new PqrTicket
            {
                Id = i,
                UserId = voter.Id,
                Voter = voter,
                Subject = $"Ticket Resuelto {i}",
                Message = "Mensaje",
                Status = "RESUELTO",
                AdminResponse = "Solucionado",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var resAbiertos = await controller.List(status: "ABIERTO", search: null, page: 1, pageSize: 20);
        var resResueltos = await controller.List(status: "RESUELTO", search: null, page: 1, pageSize: 20);
        var resTodas = await controller.List(status: "TODAS", search: null, page: 1, pageSize: 20);

        // Assert
        var rootAbiertos = UnpackJson(resAbiertos);
        Assert.Equal(10, rootAbiertos.GetProperty("totalCount").GetInt32());
        var itemsAbiertos = rootAbiertos.GetProperty("items").EnumerateArray().ToList();
        Assert.All(itemsAbiertos, item => Assert.Equal("ABIERTO", item.GetProperty("status").GetString()));

        var rootResueltos = UnpackJson(resResueltos);
        Assert.Equal(5, rootResueltos.GetProperty("totalCount").GetInt32());
        var itemsResueltos = rootResueltos.GetProperty("items").EnumerateArray().ToList();
        Assert.All(itemsResueltos, item => Assert.Equal("RESUELTO", item.GetProperty("status").GetString()));

        var rootTodas = UnpackJson(resTodas);
        Assert.Equal(15, rootTodas.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task List_SearchByUserNameAndSubject_IsCaseInsensitive()
    {
        // Arrange
        var (controller, context) = CreateController();
        var v1 = CreateVoter(context, 1, "Alejandro Magno");
        var v2 = CreateVoter(context, 2, "Beatriz Pinzón");
        var v3 = CreateVoter(context, 3, "Carlos Valderrama");

        context.PqrTickets.AddRange(
            new PqrTicket { Id = 1, UserId = v1.Id, Voter = v1, Subject = "Fallo de conexión WiFi", Message = "Msg", Status = "ABIERTO", CreatedAt = DateTime.UtcNow },
            new PqrTicket { Id = 2, UserId = v2.Id, Voter = v2, Subject = "Carné estudiantil deteriorado", Message = "Msg", Status = "ABIERTO", CreatedAt = DateTime.UtcNow },
            new PqrTicket { Id = 3, UserId = v3.Id, Voter = v3, Subject = "Consulta sobre votaciones", Message = "Msg", Status = "ABIERTO", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act - Búsqueda por nombre de votante (mayúsculas/minúsculas)
        var resVoterUpper = await controller.List(status: null, search: "PINZÓN", page: 1, pageSize: 20);
        var resVoterLower = await controller.List(status: null, search: "pinzón", page: 1, pageSize: 20);

        // Act - Búsqueda por asunto (mayúsculas/minúsculas)
        var resSubjectUpper = await controller.List(status: null, search: "WIFI", page: 1, pageSize: 20);
        var resSubjectLower = await controller.List(status: null, search: "wifi", page: 1, pageSize: 20);

        // Assert
        var rootVoterUpper = UnpackJson(resVoterUpper);
        var rootVoterLower = UnpackJson(resVoterLower);
        Assert.Equal(1, rootVoterUpper.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, rootVoterLower.GetProperty("totalCount").GetInt32());
        Assert.Equal("Beatriz Pinzón", rootVoterUpper.GetProperty("items")[0].GetProperty("userName").GetString());

        var rootSubUpper = UnpackJson(resSubjectUpper);
        var rootSubLower = UnpackJson(resSubjectLower);
        Assert.Equal(1, rootSubUpper.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, rootSubLower.GetProperty("totalCount").GetInt32());
        Assert.Equal("Fallo de conexión WiFi", rootSubUpper.GetProperty("items")[0].GetProperty("subject").GetString());
    }

    [Fact]
    public async Task List_SearchAndStatusCombined_FiltersCorrectly()
    {
        // Arrange
        var (controller, context) = CreateController();
        var v1 = CreateVoter(context, 1, "Diana Uribe");
        var v2 = CreateVoter(context, 2, "Diana Marcela");

        context.PqrTickets.AddRange(
            new PqrTicket { Id = 1, UserId = v1.Id, Voter = v1, Subject = "Petición biblioteca", Message = "Msg", Status = "ABIERTO", CreatedAt = DateTime.UtcNow },
            new PqrTicket { Id = 2, UserId = v1.Id, Voter = v1, Subject = "Queja cafetería", Message = "Msg", Status = "RESUELTO", CreatedAt = DateTime.UtcNow },
            new PqrTicket { Id = 3, UserId = v2.Id, Voter = v2, Subject = "Petición biblioteca", Message = "Msg", Status = "ABIERTO", CreatedAt = DateTime.UtcNow },
            new PqrTicket { Id = 4, UserId = v2.Id, Voter = v2, Subject = "Reclamo notas", Message = "Msg", Status = "RESUELTO", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act: Filtro ABIERTO + búsqueda "Diana Uribe" -> solo ticket 1
        var res1 = await controller.List(status: "ABIERTO", search: "Diana Uribe", page: 1, pageSize: 20);
        // Act: Filtro RESUELTO + búsqueda "Diana" -> tickets 2 y 4
        var res2 = await controller.List(status: "RESUELTO", search: "Diana", page: 1, pageSize: 20);

        // Assert
        var root1 = UnpackJson(res1);
        Assert.Equal(1, root1.GetProperty("totalCount").GetInt32());
        Assert.Equal((ulong)1, root1.GetProperty("items")[0].GetProperty("id").GetUInt64());

        var root2 = UnpackJson(res2);
        Assert.Equal(2, root2.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task List_PageBeyondLast_IsClampedToTotalPages()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector 1");

        for (uint i = 1; i <= 25; i++)
        {
            context.PqrTickets.Add(new PqrTicket
            {
                Id = i,
                UserId = voter.Id,
                Voter = voter,
                Subject = $"Asunto {i}",
                Message = "Msg",
                Status = "ABIERTO",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Act: Con 25 items y pageSize 20 -> 2 páginas. Se pide página 10.
        var result = await controller.List(status: null, search: null, page: 10, pageSize: 20);

        // Assert: Debe ser acotado a la página 2
        var root = UnpackJson(result);
        Assert.Equal(2, root.GetProperty("pageNumber").GetInt32());
        Assert.Equal(5, root.GetProperty("items").EnumerateArray().Count());
        Assert.Equal(2, root.GetProperty("totalPages").GetInt32());
    }

    [Fact]
    public async Task List_PageSizeClamped_BoundsZeroAndOneThousand()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Límites");

        for (uint i = 1; i <= 150; i++)
        {
            context.PqrTickets.Add(new PqrTicket
            {
                Id = i,
                UserId = voter.Id,
                Voter = voter,
                Subject = $"Ticket {i}",
                Message = "Msg",
                Status = "ABIERTO",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Act: pageSize 0 -> debe ajustarse a 1
        var resZero = await controller.List(status: null, search: null, page: 1, pageSize: 0);
        // Act: pageSize 1000 -> debe ajustarse a 100
        var resThousand = await controller.List(status: null, search: null, page: 1, pageSize: 1000);

        // Assert
        var rootZero = UnpackJson(resZero);
        Assert.Equal(1, rootZero.GetProperty("pageSize").GetInt32());
        Assert.Single(rootZero.GetProperty("items").EnumerateArray());

        var rootThousand = UnpackJson(resThousand);
        Assert.Equal(100, rootThousand.GetProperty("pageSize").GetInt32());
        Assert.Equal(100, rootThousand.GetProperty("items").EnumerateArray().Count());
    }

    [Fact]
    public async Task List_Ordering_CreatedAtDescWithIdTieBreak()
    {
        // Arrange
        var (controller, context) = CreateController();
        var voter = CreateVoter(context, 1, "Elector Orden");

        var dtOlder = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var dtSameTime = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var dtNewer = new DateTime(2026, 5, 1, 15, 0, 0, DateTimeKind.Utc);

        context.PqrTickets.AddRange(
            new PqrTicket { Id = 10, UserId = voter.Id, Voter = voter, Subject = "T10", Message = "Msg", Status = "ABIERTO", CreatedAt = dtOlder },
            new PqrTicket { Id = 20, UserId = voter.Id, Voter = voter, Subject = "T20", Message = "Msg", Status = "ABIERTO", CreatedAt = dtSameTime },
            new PqrTicket { Id = 30, UserId = voter.Id, Voter = voter, Subject = "T30", Message = "Msg", Status = "ABIERTO", CreatedAt = dtSameTime }, // Mismo CreatedAt, mayor Id
            new PqrTicket { Id = 5,  UserId = voter.Id, Voter = voter, Subject = "T5",  Message = "Msg", Status = "ABIERTO", CreatedAt = dtNewer }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await controller.List(status: null, search: null, page: 1, pageSize: 10);

        // Assert
        // Orden esperado:
        // 1) Id 5  (15:00)
        // 2) Id 30 (12:00, desempate por Id DESC)
        // 3) Id 20 (12:00)
        // 4) Id 10 (08:00)
        var root = UnpackJson(result);
        var ids = root.GetProperty("items")
            .EnumerateArray()
            .Select(x => x.GetProperty("id").GetUInt64())
            .ToList();

        Assert.Equal(new ulong[] { 5, 30, 20, 10 }, ids);
    }
}
