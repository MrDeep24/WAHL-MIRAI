# Diagramas del Sistema Wahl Mirai (Sincronizado v3.2 + App Móvil)

Esta carpeta contiene el catálogo oficial de diagramas de software y especificación técnica para **Wahl Mirai** (backend ASP.NET Core 9.0 MVC + App Móvil Android `WahlMirai.Movil` + despliegue con Cloudflare Tunnel), estructurado con base en la plantilla del **Manual Técnico de Referencia (SIGA-UNI, Secciones 2 y 3)**.

> **Nota sobre exportaciones PNG:** Los archivos `.mmd` y los documentos Markdown están sincronizados con el estado actual del proyecto. Si regeneras las imágenes estáticas (`.png`) con Mermaid CLI (`mmdc`), recuerda actualizarlas a partir de estos `.mmd`.

---

## Índice de Documentos y Archivos

### 1. Documentos Consolidados
* 📘 **[Manual Técnico de Diagramas (Documento Principal)](./diagramas_manual_tecnico_v3_1.md):** Consolida las 5 figuras exigidas, la explicación técnica de cada componente y los diagramas Mermaid actualizados.
* 📊 **[Diccionario de Datos Estructurado (Sección 3.2)](./diccionario_de_datos_v3_1.md):** Especificación tabular completa de las 17 tablas físicas de `wahl_mirai_db` y las 5 vistas SQL keyless mapeadas en Entity Framework Core.

### 2. Archivos Mermaid Independientes (`.mmd`)
Para visualización aislada, renderizado automatizado o exportación a formatos gráficos (PNG, SVG, PDF):

| Archivo | Figura en Manual | Descripción |
| :--- | :--- | :--- |
| **[`figura_1_arquitectura_y_despliegue_v3_1.mmd`](./figura_1_arquitectura_y_despliegue_v3_1.mmd)** | Figura 1 (Sec. 2.1 / 2.3) | Arquitectura General y Despliegue Físico: Clientes Web y App Android (`WahlMirai.Movil`), Cloudflare Tunnel (`https://wahl-mirai.lat/`), Kestrel (`WahlMirai.Web` + `MobileApiController`), almacenes singleton en RAM, almacenamiento cifrado `.enc` (`/Media/Serve` y `/api/mobile/media`), MySQL/MariaDB y relay SMTP. |
| **[`figura_2_casos_de_uso_por_rol_v3_1.mmd`](./figura_2_casos_de_uso_por_rol_v3_1.mmd)** | Figura 2 (Sec. 2.3) | Diagrama de Casos de Uso por Rol: 4 actores (Visitante, Elector, Admin, Súper Admin con herencia de permisos) y 20 casos de uso entre Web y App Móvil (`/Pqr`, `/descargar-app`, `/Legal`). |
| **[`figura_3_componentes_y_dependencias_v3_1.mmd`](./figura_3_componentes_y_dependencias_v3_1.mmd)** | Figura 3 (Sec. 2.3) | Diagrama de Componentes y Dependencias: Capa Cliente (Web Razor/JS/SignalR + App Móvil Compose/Retrofit), Capa Backend (Pipeline `ForwardedHeaders`/RateLimiting/Cookies+Bearer, RAM stores, Módulos M01–M10, `MediaController`, `StartupDiagnostics`, Worker) y Capa de Datos (EF Core + MySQL). |
| **[`figura_4_1_secuencia_autoregistro_otp_v3_1.mmd`](./figura_4_1_secuencia_autoregistro_otp_v3_1.mmd)** | Figura 4.1 (Sec. 2.3) | Secuencia: Auto-registro del elector con verificación OTP en 3 pasos (RF-M01-00). |
| **[`figura_4_2_secuencia_autopostulacion_v3_1.mmd`](./figura_4_2_secuencia_autopostulacion_v3_1.mmd)** | Figura 4.2 (Sec. 2.3) | Secuencia: Autopostulación de candidaturas, revisión administrativa con excepción/subsanación y entrega segura de fotos/planes vía `/Media/Serve` y `/api/mobile/media` (RF-M04-01, RF-M04-02). |
| **[`figura_4_3_secuencia_votacion_escrutinio_v3_1.mmd`](./figura_4_3_secuencia_votacion_escrutinio_v3_1.mmd)** | Figura 4.3 (Sec. 2.3) | Secuencia: Votación secreta y única desde Web (`/Elector/ConfirmarVoto`) y App Móvil (`/api/mobile/vote`) con escrutinio en tiempo real vía SignalR (`/Results/Index/{id}`) y auto-refresco móvil (RF-M05-00, RF-M06-00). |
| **[`figura_5_entidad_relacion_v3_1.mmd`](./figura_5_entidad_relacion_v3_1.mmd)** | Figura 5 (Sec. 3.1) | Diagrama Entidad-Relación (DER): Esquema completo de las 17 tablas físicas (incluyendo `voting_events.description`) y referencia a las 5 vistas SQL keyless. |
