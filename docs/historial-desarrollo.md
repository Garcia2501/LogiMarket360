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

## Fase 2 Seguridad y núcleo transaccional · 2026-11-20

- **Rama** `feature/nucleo-transaccional` desde `develop` sincronizada en `d6ab655`.
- **Funcionalidad**. Acceso local y autorización, catálogo y clientes, borradores, valoración, cobro simulado en dos proveedores, despacho/entrega, devolución total y Outbox durable con avisos locales. Panel WinForms de acceso y recorrido básico.
- **Commits feat**. `64206ca`, `7530805`, `9061811`, `d4b10c5`, `da70dbe`, `f4d5689`, `c0616d3`, `b0b820b`, `1437d7e`, `45cc43d`, `8883b19`, `8c59efa`, `10a8197`.
- **Commits test**. `2bf896a`, `11ab3e6`, `7b7d128`, `2a75ed5`, `26df60e`, `e10a66b`, `1304790`.
- **Defecto real y fix**. Al cerrar la sesión entre la primera autorización y el Proxy, la denegación se escribía en la transacción de pago que luego se revertía. `26df60e` reprodujo la ausencia del registro. `c4eb737` persistió esa auditoría en una unidad independiente después del rollback y pasó las 17 pruebas de seguridad y confirmación seleccionadas.
- **Riesgo de diseño y refactor**. Cotización y cobro duplicaban la composición de tarifas, cantidades físicas y promociones. `dd57d52` centralizó esa valoración en `ValorarPedido`. Las 18 pruebas seleccionadas de ambos flujos mantuvieron sus resultados.
- **Incidencia de prueba**. La primera prueba de rollback P20 no inyectaba el adaptador que genera una FK inválida. Se corrigió el fixture y se observó el fallo real de SQLite con reversión completa. Se registró en el commit de pruebas `7b7d128`.
- **Verificación final**. Restauración y compilación Release con cero errores y advertencias. 87 pruebas aprobadas, 0 fallidas y 0 omitidas, con 60 de integración, 21 de dominio y 6 de arquitectura. TRX local `phase2-final.trx`. Migración `20261120214119_EsquemaInicial` aplicada desde una base inexistente. Dos arranques consecutivos en modo inicialización, el segundo sin volver a pasar claves. Comprobaciones SQLite de integridad y FK correctas.
- **Patrones y requisitos**. La matriz registra 13 patrones integrados en esta fase, además de Composite y State existentes. La matriz de aceptación registra 23 casos aprobados, 6 parciales y 9 pendientes de ejecución. Quedan las pruebas visuales, vistas completas, reportes, comprobantes y los ocho patrones restantes.
- **Riesgos pendientes**. Un fallo de almacenamiento de diagnósticos puede ocurrir después del commit del negocio; la confirmación puede consultarse o repetirse con la misma clave. El reproceso se inicia mediante una operación ADMIN. Probar visualmente la inducción y recuperación de fallos cuando se complete el panel de demostración.

## Fase 3 Patrones GoF restantes · 2026-11-22

- **Rama y auditoría inicial.** `feature/patrones-gof-restantes` desde `develop` sincronizada en `71693d2`. Se releyeron las definiciones y la aceptación de la guía Word original de 20 páginas. La matriz de patrones vigente en el repositorio se llama `matriz-gof.md`. Código y matriz confirmaron los ocho pendientes. Singleton y Command tenían evidencia visual parcial al inicio.
- **Incrementos feat.** `37639e8` Iterator y Visitor, `28055d0` Prototype y Memento, `bb18c36` Interpreter, `00ddf7c` Bridge, `508c8e6` Flyweight y `02fe769` Mediator y conexiones WinForms. Cada incremento incluye sus pruebas, ejecutadas antes del commit. El commit de cierre actualiza matrices, diseño y evidencia técnica.
- **Verificación de cierre.** 108 casos aprobados, 0 fallidos, 0 omitidos. Release con 0 errores y 0 advertencias. Se aplicó `20261120214119_EsquemaInicial` a una base nueva y EF Core no detectó divergencia del modelo. El recorrido WinForms aislado mostró ambas vistas previas y guardó los dos TXT. Tras cobro se vieron stock 7/4/19, estado PAGADO, carrito cerrado y métrica de un pedido cobrado. Se consultó la auditoría SQLite de los comandos emitidos.
- **Defectos y riesgos.** No se reprodujo un defecto de producto que requiriera un commit fix. La evaluación en memoria del filtro es una decisión de escala académica. El historial es local al proceso y coteja la versión para no deshacer ediciones ajenas. La mediación informa fallos de refresco por separado del resultado comercial.
- **Cobertura.** 23 patrones integrados con pruebas técnicas. Singleton conserva pendiente P15 a nivel visual. 33 casos de aceptación verificados y 5 parciales. Quedan las vistas finales que exige la guía y el cierre de P08, P10, P15, P37 y P38.

## Plantilla para próximas fases

Registrar fase y fecha, rama, cambio y flujo comprobable, commits y verificaciones de integración, requisitos cumplidos o pendientes, patrones con archivo y método, comandos y resultados, incidentes con esperado/obtenido, forma de reproducción, corrección y regresión, y siguientes pasos. Para riesgos reales, vincular evidencia de antes y después, efecto, pruebas y commit. Un estado pendiente conserva sus campos de evidencia vacíos hasta ejecutar.
