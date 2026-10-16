# Directrices y Reglas del Proyecto WAHL-MIRAI

## Actualización del Grafo de Conocimiento (Graphify)

Cada vez que termines de modificar o crear archivos en el proyecto, ejecuta automáticamente antes de entregar la respuesta final:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\graphify-refresh.ps1
```

- **Disparador:** Siempre que se añadan, modifiquen o eliminen archivos en el repositorio.
- **Momento:** Ejecutar antes de entregar la respuesta final al usuario.
- **Método:** Usar exclusivamente `.\scripts\graphify-refresh.ps1` desde la raíz del repositorio (no ejecutar `graphify update` directamente).
