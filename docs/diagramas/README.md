# Diagramas del Sistema Wahl Mirai (Versión 3.1)

Esta carpeta contiene el catálogo oficial de diagramas de software y especificación técnica para **Wahl Mirai v3.1**, estructurado con base en la plantilla del **Manual Técnico de Referencia (SIGA-UNI, Secciones 2 y 3)**.

---

## Índice de Documentos y Archivos

### 1. Documentos Consolidados
* 📘 **[Manual Técnico de Diagramas v3.1 (Documento Principal)](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/diagramas_manual_tecnico_v3_1.md):** Consolida las 5 figuras exigidas, la explicación técnica de cada componente y los diagramas Mermaid interactivos.
* 📊 **[Diccionario de Datos Estructurado (Sección 3.2)](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/diccionario_de_datos_v3_1.md):** Especificación tabular completa de las 17 tablas físicas de `wahl_mirai_db` y las 5 vistas SQL keyless mapeadas en Entity Framework Core.

### 2. Archivos Mermaid Independientes (`.mmd`)
Para visualización aislada, renderizado automatizado o exportación a formatos gráficos (PNG, SVG, PDF):

| Archivo | Figura en Manual | Descripción |
| :--- | :--- | :--- |
| **[`figura_1_arquitectura_y_despliegue_v3_1.mmd`](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_1_arquitectura_y_despliegue_v3_1.mmd)** | Figura 1 (Sec. 2.1 / 2.3) | Arquitectura General y Despliegue Físico: Nodos cliente, proxy inverso Nginx/Apache, Kestrel, almacenes singleton en RAM, sistema de archivos cifrado `.enc`, MySQL y relay SMTP. |
| **[`figura_2_casos_de_uso_por_rol_v3_1.mmd`](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_2_casos_de_uso_por_rol_v3_1.mmd)** | Figura 2 (Sec. 2.3) | Diagrama de Casos de Uso por Rol: 4 actores (Visitante, Elector, Admin, Súper Admin con herencia de permisos) y 19 casos de uso organizados por módulo de dominio. |
| **[`figura_3_componentes_y_dependencias_v3_1.mmd`](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_3_componentes_y_dependencias_v3_1.mmd)** | Figura 3 (Sec. 2.3) | Diagrama de Componentes y Dependencias: Capa Cliente (Layouts, i18n, Theme, Chatbot, SignalR, AJAX), Capa Backend (Pipeline, RAM stores, Módulos M01-M09, Worker, Servicios transversales) y Capa de Datos (EF Core + MySQL). |
| **[`figura_4_1_secuencia_autoregistro_otp_v3_1.mmd`](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_4_1_secuencia_autoregistro_otp_v3_1.mmd)** | Figura 4.1 (Sec. 2.3) | Secuencia: Auto-registro del elector con verificación OTP en 3 pasos (RF-M01-00). |
| **[`figura_4_2_secuencia_autopostulacion_v3_1.mmd`](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_4_2_secuencia_autopostulacion_v3_1.mmd)** | Figura 4.2 (Sec. 2.3) | Secuencia: Autopostulación de candidaturas y revisión administrativa con excepción/subsanación (RF-M04-01, RF-M04-02). |
| **[`figura_4_3_secuencia_votacion_escrutinio_v3_1.mmd`](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_4_3_secuencia_votacion_escrutinio_v3_1.mmd)** | Figura 4.3 (Sec. 2.3) | Secuencia: Votación de sufragio secreto y único con transmisión de escrutinio en tiempo real vía SignalR (RF-M05-00, RF-M06-00). |
| **[`figura_5_entidad_relacion_v3_1.mmd`](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_5_entidad_relacion_v3_1.mmd)** | Figura 5 (Sec. 3.1) | Diagrama Entidad-Relación (DER): Esquema completo de las 17 tablas físicas y relaciones referenciales. |

