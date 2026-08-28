namespace WahlMirai.Web.Models;

/// <summary>
/// Constantes de roles del sistema alineadas con la tabla 'roles' de la base de datos (v2.8).
/// ID 1 = ELECTOR, ID 2 = ADMIN, ID 3 = SUPER_ADMIN.
/// </summary>
public static class Roles
{
    public const byte Elector = 1;
    public const byte Admin = 2;
    public const byte SuperAdmin = 3;

    public const string ElectorName = "ELECTOR";
    public const string AdminName = "ADMIN";
    public const string SuperAdminName = "SUPER_ADMIN";
}
