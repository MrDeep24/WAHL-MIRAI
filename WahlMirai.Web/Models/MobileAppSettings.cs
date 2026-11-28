namespace WahlMirai.Web.Models;

/// <summary>
/// Configuración para la descarga y verificación de integridad de la aplicación móvil Android (APK).
/// Mapea la sección "MobileApp" de appsettings.json.
/// </summary>
public class MobileAppSettings
{
    /// <summary>
    /// URL directa de descarga del asset en GitHub Releases.
    /// </summary>
    public string ApkDirectUrl { get; set; } = "https://github.com/MrDeep24/WAHL-MIRAI/releases/download/M%C3%B3vil/Wahl-Mirai.apk";

    /// <summary>
    /// Hash SHA-256 esperado del archivo APK para verificación de integridad.
    /// </summary>
    public string Sha256 { get; set; } = "47a3dc458b070fd6b45608d9fc75d92d99938b7461e63a112a57a081ff5ade88";

    /// <summary>
    /// Ruta relativa dentro de wwwroot para servir el APK en escenarios offline o de intranet.
    /// </summary>
    public string LocalFallbackPath { get; set; } = "downloads/wahl-mirai.apk";
}
