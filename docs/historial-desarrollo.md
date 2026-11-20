# Historial de desarrollo

## Fase 0 Análisis y base · 2026-11-20

| Campo | Estado |
| --- | --- |
| Rama y commits | Ninguno. La gestión de Git está pausada por indicación del usuario hasta recibir el enlace de su repositorio. |
| Entrega | Guía de 20 páginas, tablas y 12 wireframes revisados. RF/RNF trazados, diagramas preliminares, 23 asignaciones GoF, 38 escenarios oficiales y conjunto de automatización propuesto. Solución con cuatro proyectos de producción y uno de pruebas. |
| Funcionalidad implementada | Arranque de una ventana WinForms vacía. Los flujos de negocio tendrán sus pruebas al desarrollar cada incremento. |
| Verificaciones observadas | SDK .NET 8.0.428. Restauración desde caché local y compilación de cinco proyectos, 0 advertencias y 0 errores. |
| Errores reales detectados y correcciones | El primer intento de la CLI no pudo crear un archivo de inicialización fuera del entorno escribible; se aisló DOTNET_CLI_HOME en work. La restauración offline de Microsoft.NET.Test.Sdk 17.14.1 falló con NU1102 porque requería System.Reflection.Metadata ≥ 8.0.0, ausente en la caché; se usó la versión 17.8.0 disponible y se repitió la compilación. Incidencias de preparación, sin defectos observados en lógica de negocio. |
| Patrones implementados y acreditados | Pendientes los 23. Las clases de colaboración se indican como propuestas. |
| Requisitos y pruebas | Esqueleto de cinco proyectos y grafo inicial preparados para RNF02/RNF09. RF01–RF18 y sus P01–P38 quedan pendientes de ejecución funcional. La suite aún no tiene pruebas implementadas. |
| Próximo paso | Confirmar supuestos y preparar dominio, roles, migración inicial, seed idempotente y fixture reproducible. Esperar el enlace para gestionar Git. |


## Publicación inicial · 2026-11-20

- El remoto oficial se comprobó vacío antes del arranque. La base local de main se organizó en commits de configuración, solución y documentación. develop partirá de la misma revisión publicada.
- Configuración: fc9e98f. Solución inicial: 1280e37. El commit que incorpora esta entrada registra el análisis, el plan y las matrices.
- Verificación desde la carpeta original: SDK .NET 8.0.428, cinco proyectos compilados, 0 errores y 0 advertencias. La ejecución restringida inicial no resolvió referencias de proyecto fuera del espacio escribible. La repetición con acceso a la carpeta original completó el build sin cambiar los proyectos.
- El Word original permanece local. Se revisan los archivos versionados antes de publicar. Los flujos, patrones GoF y pruebas de negocio quedan pendientes para los incrementos desde develop.

## Plantilla para próximas fases

Registrar fase y fecha, rama, cambio y flujo comprobable, commits y verificaciones de integración, requisitos cumplidos o pendientes, patrones con archivo y método, comandos y resultados, incidentes con esperado/obtenido, forma de reproducción, corrección y regresión, y siguientes pasos. Para riesgos reales, vincular evidencia de antes y después, efecto, pruebas y commit. Un estado pendiente conserva sus campos de evidencia vacíos hasta ejecutar.
