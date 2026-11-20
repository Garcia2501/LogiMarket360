# Plan maestro de desarrollo

El proyecto es individual, presencial y nuevo. La guía indica ocho horas presenciales y remite el calendario del desarrollo progresivo a la asignatura. Las fases son secuenciales por dependencias, con incrementos verticales pequeños y verificables que entregan partes del flujo completo.

| Fase | Incrementos de trabajo | Actividad de la guía | Salida verificable |
| --- | --- | --- | --- |
| 0. Análisis y base | Revisar 20 páginas y 12 wireframes; levantar RF/RNF, 23 patrones, P01–P38, diagramas preliminares y cinco proyectos; acordar supuestos, calendario e identidad académica. | 1–2 | Documentación trazable y compilación de la estructura. |
| 1. Plataforma y datos | Composición DI, usuarios, clientes, catálogo e inventario, pedidos, Composite, State, EF Core 8/SQLite, restricciones, migración, seed y fixtures. | 2–3 | P01, P18 y P31 en dominio. Base para P10, P36 y P38. |
| 2. Cotización y borradores | Sesión y autorización por operación, Iterator/Visitor, Builder, Prototype, estrategias, familias de entrega, Memento y Chain sobre el dominio ya creado. Incorporar edición, cancelación y vista temprana. | 3–6 | P12–P14, P23, P27, P29, P32 y P34. |
| 3. Confirmación y Outbox | Command, Facade, Adapter, Proxy, Decorator; transacción, idempotencia, control de sobreventa, historial; Observer/Factory Method, métricas y reintento seguro. Integrar UI de cobro. | 4–6 | P02–P08, P11, P15–P16, P19–P20, P22, P24, P30, P36–P37. |
| 4. Operaciones posteriores | Despacho/entrega y guía, devoluciones con Template Method, reintegro/reembolso, comprobantes Bridge, operaciones auditadas. | 4–6 | P09, P12, P17, P31, P33 y P35. |
| 5. Consulta e integración visual | Completar doce áreas y Mediator, accesibilidad y navegación, Flyweight, reportes Interpreter, seguimiento, acciones demo aisladas y pruebas de interacción. | 5–6 | P10, P21, P25–P26, P28 y comprobaciones visuales de las fases previas. |
| 6. Verificación y refactorización | Revisar SOLID y diez o más riesgos reales registrados durante toda la construcción. Reejecutar P01–P38, suite automática, compilación desde código limpio, DB vacía, reinicio, fallos y recuperación. | 7–9 | P38, al menos 25 aprobadas, matriz completa con evidencia, trazas y comparación Flyweight. |
| 7. Entrega y defensa | Empaquetar fuentes limpias, completar README, PDF en orden oficial, conclusiones por objetivo, recomendaciones y referencias IEEE. Ensayar demo y verificar autoría y artefactos. | 9–10 | ZIP y PDF con nomenclatura oficial, defensa reproducible y cobertura 23/23 demostrada. |

La cobertura indicada es prevista. P31 se verificó en el agregado y se revisará de nuevo con las operaciones completas. P38 y otros casos transversales se ejecutan cuando sus dependencias estén completas. Las pruebas y la evidencia se incorporan en cada fase, incluso si se reejecutan en el cierre.

## Paso inicial de implementación

1. Revisar supuestos del [diseño técnico](diseno-tecnico.md), principalmente permisos de devolución/ajuste, cómputo de plazos y condición para reintegro. Se puede avanzar con los valores oficiales mientras se confirman puntos abiertos.
2. Empezar por Domain y Application para usuario/rol, SKU y cantidad, puertos y casos de uso. Definir la primera migración, seed y fixture SQLite al incorporar Infrastructure. Verificar persistencia y restricciones con P01.
3. Montar el punto de composición y login solo después de contar con autorización de casos de uso. Incorporar después una primera venta oficial de Ana como corte vertical, con las capas y pruebas correspondientes.
4. Añadir dependencias EF Core 8 y DI en sus proyectos externos durante el primer incremento que las use, con versiones verificadas y registro de restauración. Mantener .NET 8 y validar los proyectos permitidos al compilar.

## Gestión permanente de Git y GitHub

El repositorio oficial fue creado por el usuario. La configuración de origin y la primera publicación de main y develop recibieron autorización para el arranque. A partir de ahí, crear feature/* desde develop, hacer commits locales incrementales y publicar ramas de trabajo o actualizar develop después de completar sus verificaciones. Las operaciones destructivas requieren autorización específica. Mantener main estable hasta la aprobación de una nueva versión.

| Rama | Uso y control |
| --- | --- |
| `main` | Versión estable. Reservar integración para aprobación final. Proteger en GitHub cuando esté habilitado. |
| `develop` | Integración verificada. Recibir incrementos terminados, revisados y probados. |
| `feature/*` | Incremento funcional desde develop, por ejemplo `feature/catalogo-kits`. |
| `fix/*` | Corrección reproducible desde la línea correspondiente, por ejemplo `fix/reintento-de-outbox`. |
| `docs/*` | Documentación y trazabilidad, por ejemplo `docs/analisis-ape04`. |

El asistente administra técnicamente ramas y commits. Conservar el lote inicial de configuración, solución y documentación en commits separados. Usar commits pequeños y coherentes durante cada fase. Antes de registrar un commit, revisar los cambios y comprobar que el mensaje refleja el cambio real. Para integrar en develop, revisar referencias, reglas, cobertura y resultados del incremento. Cuando esté habilitado GitHub, usar revisión del cambio y comprobaciones de Windows/.NET 8 con base aislada. Evitar squash de una fase completa para conservar progresión comprobable. Acordar identidad local cuando falte, preservar procedencia y exigir que el participante revise, comprenda y pueda defender los cambios. No atribuir autoría ni resultados sin verificación.

| Tipo | Criterio | Ejemplo de mensaje previsto |
| --- | --- | --- |
| feat | Nueva función operativa | `feat(catalogo): agrega kits anidados` |
| fix | Falla observada y corregida | `fix(outbox): evita repetir avisos al reintentar` |
| docs | Documentación | `docs(ape04): registra criterios y wireframes` |
| test | Agregar o ajustar pruebas | `test(pagos): verifica el contrato de ambas pasarelas` |
| refactor | Estructura sin cambio esperado de comportamiento | `refactor(pedidos): separa valoracion de confirmacion` |
| chore | Mantenimiento y configuración no vinculada al build | `chore(repo): excluye artefactos locales` |
| build | Dependencias, compilación y carga de proyectos | `build(dotnet): configura cinco proyectos y sdk 8` |

Tipos, ámbitos y descripciones en minúsculas, sin tildes y con cambios comprobables. Los ejemplos son candidatos sujetos al cambio real. Todos los comentarios explicativos de código en minúsculas y sin tildes. Identificar cada patrón cerca de su colaboración real, por ejemplo `// patron strategy: selecciona la politica de descuento`. Mantener los identificadores con las convenciones C#.

## Registro de fallos, riesgos y SOLID

Al observar una falla real, anotar esperado/obtenido y pasos reproducibles. Capturar el fallo con una prueba que inicialmente falle si aplica, corregirlo, reejecutar la prueba y la regresión pertinente, registrar `fix` y enlazar evidencia y commits. Clasificar fallos de entorno aparte de defectos de aplicación. No introducir defectos deliberados.

Revisar durante la construcción áreas propensas a riesgos: duplicación de fórmulas, acoplamiento UI/casos de uso, fachada saturada, validaciones dispersas, estados primitivos, sustitución de proveedores, contratos demasiado amplios, referencias a concretos, snapshots mutables, dependencia del reloj, semilla que sobrescribe datos y deduplicación de efectos. Son puntos de inspección, sin resultados atribuidos a esta fase. Registrar al menos diez hallazgos reales con ubicación, impacto verificable, prueba antes/después, refactorización concreta y commit `refactor` cuando proceda. Conservar comportamiento esperado y resultados honestos.

| Principio | Candidato de demostración real |
| --- | --- |
| SRP | Mostrar cálculo puro separado de confirmación y consumo en el flujo. |
| OCP | Registrar una promoción nueva y ejecutar A32 sin editar la calculadora. |
| LSP | Ejecutar ambos proveedores con las mismas entradas, resultados y errores de contrato. |
| ISP | Probar que lectores y escritores reciben contratos pequeños adecuados a sus operaciones. |
| DIP | Cambiar proveedor/repositorio mediante puertos inyectados y verificar grafo de .csproj y DI. |

Al cerrar cada fase actualizar [historial de desarrollo](historial-desarrollo.md) e informar rama, commits, estado del repositorio, pruebas, patrones y requisitos, fallos reales y próximos pasos. Guardar datos brutos de prueba, evidencia y fecha. Registrar solo cifras ejecutadas y resultados observados.
