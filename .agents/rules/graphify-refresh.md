---
trigger: always_on
description: Política de Graphify: no actualizar automáticamente el grafo; actualización manual por el usuario o bajo petición expresa.
---

# Política de Refresco de Graphify

**NO actualizar automáticamente el grafo.** Los agentes **NUNCA** deben ejecutar el script de refresco ni modificar el grafo de conocimiento por iniciativa propia al terminar una tarea o al modificar código. La actualización del grafo es una operación **exclusivamente manual** gestionada por el usuario, o ejecutada por el agente **únicamente** cuando el usuario lo solicite de forma expresa y directa.

Si el usuario solicita explícitamente actualizar el grafo (o mediante `/graphify`), el comando soportado es:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\graphify-refresh.ps1
```

## Directrices de la Política:
1. **Prohibición de auto-actualización:** Al concluir tareas de desarrollo, correcciones, refactorizaciones o cualquier edición de archivos indexables, **NO** ejecutes `graphify-refresh.ps1`. Deja los archivos de `graphify-out/` intactos a menos que el usuario lo haya ordenado explícitamente en la solicitud actual.
2. **Actualización bajo demanda explícita:** Solo ejecuta el script si el usuario pide directamente refrescar/actualizar el grafo.
3. **Fallos no bloqueantes:** Si se ejecuta por orden del usuario y el script falla (incluyendo el error intermitente `WinError 2` por carrera de temporales en Windows), reintenta una sola vez. Si vuelve a fallar, repórtalo en una sola línea y continúa. **Nunca edites `graphify-out/` a mano**.
4. **Tratamiento de `graphify-out/`:** Cuando el usuario ordene actualizarlo, los cambios en `graphify-out/` son efectos secundarios generados; lístalos en una sección separada del informe final, exclúyelos de cualquier verificación de "solo archivos en whitelist" y **NUNCA** los incluyas en commits de features; se commitean aparte (`chore(graphify): ...`).
5. **Conflictos de merge:** Si un merge genera conflictos dentro de `graphify-out/`, no los resuelvas a mano: acepta cualquiera de los dos lados (`ours` o `theirs`) y, si el usuario lo instruye, ejecuta el script de refresco.
