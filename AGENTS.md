# Directrices y Reglas del Proyecto WAHL-MIRAI

## Política de Actualización del Grafo de Conocimiento (Graphify)

Ejecuta el script de refresco **UNA SOLA VEZ AL FINAL** de la tarea, antes del informe final, y **únicamente** si se crearon, modificaron, renombraron o eliminaron archivos que el grafo indexa:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\graphify-refresh.ps1
```

### Reglas de aplicación:
1. **Cuándo ejecutar:** Solo al concluir la tarea y si hubo cambios en archivos indexables por el grafo (código de aplicación en `WahlMirai.Web/` y `WahlMirai.Tests/`, `scripts/`, o SQL/docs bajo `docs/` no excluidos por `.graphifyignore`). **NO** ejecutar tras cada edición individual.
2. **Tareas de solo lectura o archivos ignorados:** **NO** ejecutar en diagnósticos, auditorías y cualquier tarea de solo lectura, ni cuando únicamente cambien `REGISTRO_CAMBIOS_*.md`, `graphify-out/` u otras rutas ignoradas. Las tareas de solo lectura deben dejar `git status` limpio.
3. **Fallos no bloqueantes:** Si el script falla (incluyendo el error intermitente `WinError 2` por carrera de temporales en Windows), reintenta una sola vez. Si vuelve a fallar, repórtalo en una sola línea y continúa. **Nunca edites `graphify-out/` a mano**.
4. **Tratamiento de `graphify-out/`:** Los cambios en `graphify-out/` son efectos secundarios generados; lístalos en una sección separada del informe final, exclúyelos de cualquier verificación de "solo archivos en whitelist" y **NUNCA** los incluyas en commits de features; se commitean aparte (`chore(graphify): ...`).
5. **Conflictos de merge:** Si un merge genera conflictos dentro de `graphify-out/`, no los resuelvas a mano: acepta cualquiera de los dos lados (`ours` o `theirs`) y corre el script de refresco una vez al finalizar.
