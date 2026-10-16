---
trigger: always_on
description: Ejecutar automáticamente .\scripts\graphify-refresh.ps1 tras modificar o crear archivos en el proyecto antes de la respuesta final.
---

# Graphify Auto-Refresh

Cada vez que termines de modificar o crear archivos en el proyecto, ejecuta automáticamente antes de entregar la respuesta final:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\graphify-refresh.ps1
```

## Instrucciones:
- **Disparador:** Cualquier adición, modificación o eliminación de archivos en el repositorio.
- **Momento:** Debe ejecutarse antes de enviar la respuesta final al usuario.
- **Método:** Utiliza siempre el script `.\scripts\graphify-refresh.ps1` desde la raíz del proyecto. No ejecutes `graphify update` directamente para asegurar que se aplique la actualización forzada y el mecanismo de fallback si es necesario.
