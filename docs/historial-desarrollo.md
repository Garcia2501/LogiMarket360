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

## Fase 1 Dominio y persistencia · 2026-11-20

- **Rama** `feature/dominio-persistencia` desde `develop` sincronizada en `9e5f254`.
- **Commits**. `1fb1375` entidades, `316238f` Composite, `fa89932` pedidos y State, `1931f18` puertos, `5a8828a` mapeo SQLite, `3c2f9b2` registros operativos, `dd756fb` migración, `64273eb` repositorios y transacción, `dc3592f` seed y hash, `e85eeae` pruebas de dominio, `47ac83b` pruebas de seed, `03990a7` restricciones, `9e62a5d` arquitectura y persistencia, `cb8cada` corrección de pruebas SQLite y `7bb3b00` arranque. Esta entrada se incorpora con el commit de documentación de fase.
- **Verificación**. Restauración correcta, compilación con 0 errores y advertencias, y 49 pruebas aprobadas, 0 fallidas y 0 omitidas (`phase1-final.trx` local). `20261120214119_EsquemaInicial` aplicada por EF Core 8.0.34 en una base nueva y desde el arranque de WinForms. La repetición del arranque sin claves produjo 5 productos, 2 kits, 4 enlaces y 4 usuarios. Ambas bases verificaron integridad y FK.
- **Incidencias**. Primera suite completa: 47 aprobadas y 2 fallidas (`phase1-first-full.trx`). La prueba de pago duplicado reutilizaba el rastreo de la relación uno a uno, por lo que EF interceptaba el segundo pago antes de SQLite. Se despejó el rastreo antes del segundo INSERT. La prueba de Outbox esperaba 2067 y SQLite devolvió 1555 por la PK compuesta. Ambas comprobaciones pasaron al reejecutar. PowerShell también separó `-m:1` al reenviarlo desde una función del script de verificación. La llamada directa a dotnet resolvió el error.
- **Cobertura**. P01, Composite en P18 y State en P31 del agregado. Quedan 35 casos oficiales sin ejecución completa. La guía aplica MAYORISTA según cantidad física. Las únicas categorías de cliente son NORMAL y VIP.
- **Pendientes**. Sesión y autorización por operación, CRUD visible, promociones, flujos completos y procesador Outbox. Composite, instantáneas y State se adelantaron a esta fase para dejar la base de dominio y persistencia verificada.

## Plantilla para próximas fases

Registrar fase y fecha, rama, cambio y flujo comprobable, commits y verificaciones de integración, requisitos cumplidos o pendientes, patrones con archivo y método, comandos y resultados, incidentes con esperado/obtenido, forma de reproducción, corrección y regresión, y siguientes pasos. Para riesgos reales, vincular evidencia de antes y después, efecto, pruebas y commit. Un estado pendiente conserva sus campos de evidencia vacíos hasta ejecutar.
