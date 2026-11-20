# Análisis de la guía APE 04

Fuente primaria: GUIA_APE_04 (1).docx, Universidad Técnica de Ambato, Carrera de Software, julio–diciembre de 2026. Se inspeccionaron el cuerpo, las tablas y las imágenes originales y se verificó en Word la paginación de 20 páginas. La evaluación se desprende de las comprobaciones, actividades, restricciones y entregables. La guía no asigna puntajes por criterio.

## Mapa de lectura

| Páginas | Contenido revisado |
| --- | --- |
| 1 | Portada institucional, objetivos, modalidad individual y presencial, 8 horas presenciales, autoría y proyecto nuevo. |
| 2–3 | Cronograma progresivo sujeto a la asignatura, caso empresarial, actores, alcance, catálogo, ciclo, políticas y cifras oficiales. |
| 4–5 | Referencias de compilación, recorrido funcional, cadena de cobro, modelo relacional, restricciones, Repository, CQRS, Unit of Work, DI y SOLID. Empieza Factory Method. |
| 5–8 | Tablas completas de 5 patrones creacionales, 7 estructurales y 11 comportamentales, con sus comprobaciones. |
| 8–9 | Equipos, C#/.NET 8/WinForms/EF Core 8/SQLite, DI, xUnit o NUnit, simulación, límites técnicos, lista TAC y actividades 1–5. |
| 10 | Actividades 6–10, interfaz mínima, reglas de interacción y modo de demostración. |
| 11–16 | Los 12 wireframes y sus aclaraciones normativas. |
| 17–18 | Matriz P01–P38, datos controlados, trazabilidad y verificación de efectos colaterales. |
| 19–20 | Evidencias, prohibiciones, ZIP, PDF, orden del informe, README, resultados, habilidades blandas, conclusiones, recomendaciones, referencias IEEE y anexos. |

El objetivo general exige un flujo de escritorio ejecutable con persistencia, trazabilidad, Clean Code, KISS, DRY, YAGNI y SOLID. Los objetivos específicos exigen analizar el negocio, demostrar dependencias, ejecutar los 23 patrones, mantener consistencia ante fallos y validar con pruebas, diagramas, Git y evidencias. La portada final debe completar nivel/paralelo, participante, asignatura y responsable con datos reales. Las casillas TAC y habilidades blandas se documentarán según lo que se emplee efectivamente.

## Valores y reglas oficiales

| SKU | Producto | Precio USD | Stock inicial | Peso kg |
| --- | --- | ---: | ---: | ---: |
| P01 | Mouse USB | 20.00 | 8 | 0.20 |
| P02 | Teclado | 30.00 | 5 | 0.70 |
| P03 | Memoria USB | 10.00 | 20 | 0.10 |
| P04 | Webcam | 40.00 | 4 | 0.35 |
| P05 | Alfombrilla | 5.00 | 30 | 0.10 |

- SKU único, precio decimal positivo, peso no negativo y stock entero no negativo. KIT_OFICINA = P01 + P02 = USD 50.00. KIT_TELETRABAJO = KIT_OFICINA + P03 = USD 60.00 y pesa 1.00 kg. Expandir hasta componentes físicos, propagar multiplicidades y rechazar ciclos directos e indirectos.
- Configurar promociones excluyentes por prioridad: MAYORISTA 15 % desde 10 unidades físicas finales, con cualquier categoría. Por debajo de 10, VIP 10 % si corresponde. NORMAL 0 % en el resto. Calcular subtotal por precio × cantidad de componentes, descuento redondeado con `MidpointRounding.AwayFromZero` a dos decimales y total = subtotal − descuento + tarifa. Mostrar dos decimales. Sin IVA. ESTÁNDAR USD 5.00 y 3 días, EXPRÉS USD 12.00 y 1 día. Conservar precios y composición por pedido.
- Ciclo BORRADOR → VALIDADO → PAGADO → DESPACHADO → ENTREGADO. CANCELADO solo desde BORRADOR/VALIDADO. Impedir retrocesos, cobros repetidos, despacho antes del pago y edición pagada. Descontar stock una vez y confirmar pedido, pago, movimientos y eventos pendientes con una transacción, con clave de idempotencia y unicidad. Fallo de validación, insuficiencia o rechazo preserva datos de negocio. Publicar posteriormente mediante Outbox.
- Devolución TOTAL de un ENTREGADO sin devolución previa. DEFECTO hasta 30 días. ARREPENTIMIENTO hasta 7 días con producto sin uso. Reintegrar una sola vez el stock cuando proceda. Reembolsar los productos menos su descuento, excluyendo envío. Las solicitudes rechazadas no cambian inventario ni generan reintegro monetario.

| Escenario independiente | Cálculo oficial | Inventario esperado |
| --- | --- | --- |
| Ana VIP, KIT_TELETRABAJO ×1 ESTÁNDAR | 60.00 − 6.00 + 5.00 = 59.00 | P01 8→7, P02 5→4, P03 20→19. |
| Luis NORMAL, P04 ×2 + P05 ×2 EXPRÉS | 80.00 + 10.00 + 12.00 = 102.00 | P04 4→2, P05 30→28. |
| Carmen MAYORISTA, P03 ×10 ESTÁNDAR | 100.00 − 15.00 + 5.00 = 90.00 | P03 20→10 con base recién preparada. |
| KIT_TELETRABAJO ×6 sobre el stock inicial | Rechazo, requiere seis P02 y hay cinco. | Sin variación ni cobro. |
| Devolución por defecto de Ana entregado | 60.00 − 6.00 = 54.00 | Un reintegro de los tres SKU y un reembolso. |

Restablecer datos controlados salvo en casos de persistencia, acumulación o reintentos que exijan preservar estado.

## Actores y acceso

| Actor | Operaciones definidas |
| --- | --- |
| ADMIN | Todas. |
| VENTAS | Clientes, pedidos, validación y cobro. |
| BODEGA | Preparación, despacho y entrega. |
| CONSULTA | Solo lectura. |

Autorización en Application/servicios protegidos; botones y mensajes reflejan permisos para guiar al usuario. Propuesta conservadora para escrituras que la guía no asigna explícitamente a un rol distinto de ADMIN: alta/baja de catálogo, ajustes y devoluciones solo ADMIN. Confirmar alcance de consultas operativas y datos sensibles. Los operadores necesitan leer las vistas requeridas por sus operaciones. El modo de demostración debe estar aislado y protegido.

## Requisitos funcionales y trazabilidad

| ID | Caso de uso y requisito medible | Verificación prevista |
| --- | --- | --- |
| RF01 | Identificarse y cerrar sesión localmente con usuarios iniciales, hash y cuatro roles. | P01, P22; login correcto/incorrecto y sesión cerrada. |
| RF02 | Consultar y administrar productos con CRUD, búsqueda e invariantes del catálogo. | P01, P21; duplicado SKU y cantidades inválidas. |
| RF03 | Registrar y editar kits, impedir ciclos y expandir SKU finales. | P05, P18, P27, P34. |
| RF04 | Buscar, registrar y editar clientes con identificación, categoría, contacto y estado activo. | P23; ejercicio CRUD y cliente inactivo. |
| RF05 | Crear/editar borrador con cliente, líneas, cantidades, entrega e instantáneas; rechazar vacío. | P08, P13; modificar catálogo posterior y verificar instantánea. |
| RF06 | Clonar carrito con copia profunda y deshacer/rehacer hasta cinco estados completos, solo en borrador. | P14, P29. |
| RF07 | Calcular subtotal, promoción única, flete, plazo y total con cifras verificables. | P02–P04, P12, P32; umbral 9/10 y redondeo. |
| RF08 | Validar por cadena, cancelar únicamente en estados permitidos y consultar historial. | P08, P23, P24, P31. |
| RF09 | Confirmar con cobro simulado sustituible, registrar trazas y evitar pago doble/sobreventa. | P02–P07, P16, P19–P20, P22, P36. |
| RF10 | Preparar, despachar y entregar en orden, generar guía simulada y consultar seguimiento. | P09, P12, P31; trazabilidad de guía/entrega. |
| RF11 | Devolver totalmente desde ENTREGADO conforme a motivo, plazo y condición; reintegro y reembolso únicos. | P33, P35; límites inclusivos y producto usado. |
| RF12 | Consultar/registrar inventario y movimientos con motivo y referencia, incluido ajuste auditado. | P02–P07, P35–P36; ajuste positivo/negativo. |
| RF13 | Obtener reporte por fechas y filtros por estado, total y cliente con AND/OR y paréntesis. | P25, P26, P34; filtro por rango y resumen. |
| RF14 | Emitir comprobantes internos de venta y devolución a vista previa o TXT. | P17; cuatro combinaciones. |
| RF15 | Generar avisos simulados EMAIL/SMS, auditoría y métricas por pedido, despacho y devolución. | P11, P15, P19, P28, P30, P37. |
| RF16 | Mostrar panel con pedidos, ventas, alertas, stock y métricas; refrescar catálogo, carrito y pedidos mediante mediador. | P15, P21, P28, P30. |
| RF17 | Demostrar cambio de proveedor, fallo de cobro y fallo/reintento Outbox en entorno controlado. | P06, P16, P37. |
| RF18 | Persistir pedido, pago, stock, despacho, devolución y auditoría al reiniciar. | P01, P10, P35, P37; consulta postreinicio. |

## Requisitos no funcionales y evaluación

| ID | Dimensión y umbral o verificación | Evidencia prevista |
| --- | --- | --- |
| RNF01 | Integridad. Cero efectos de negocio parciales en rechazo. Un pago por pedido, un movimiento por SKU en confirmación, una devolución y stock ≥ 0. | P05–P08, P20, P35–P36, índices, estado de DB. |
| RNF02 | Mantenibilidad. Cinco proyectos y cero dependencias prohibidas. Patrones, SOLID, Repository, CQRS básico, transacción real y DI por constructor. | P38, _build_ limpio, .csproj, diagramas, código e invocaciones reales. |
| RNF03 | Usabilidad. Doce áreas accesibles desde la ventana principal; TAB, foco, etiquetas, validación próxima al campo, escala, mensajes, confirmación, selección persistente y dos decimales. | Guion visual de las 12 vistas, pruebas válidas e inválidas. |
| RNF04 | Rendimiento. P21 ejecutado sobre al menos 1.000 filas, con identidad compartida, instancias y memoria frente a escenario equivalente. La guía no fija latencia ni porcentaje de ahorro. | Medición reproducible, condiciones y resultado bruto obtenido. |
| RNF05 | Seguridad local. Cuatro roles, autorización de operaciones y cero contraseñas en claro; denegación de pago antes de trazas y proveedor. | P01, P22, inspección de BD, revisión del ZIP. |
| RNF06 | Trazabilidad. Registrar cambios de estado, movimientos, correlación, comandos, eventos y evidencias para todos los requisitos/patrones y casos. | Matrices, logs, captura, referencia a archivo/método/commit. |
| RNF07 | Testabilidad. Ejecutar P01–P38 y aprobar al menos 25 pruebas automáticas en dominio, aplicación e integración. | P38, informe TRX, datos repetibles y resultados obtenidos. |
| RNF08 | Recuperabilidad. Base vacía recuperable vía migraciones/seed, persistencia al reiniciar y evento reintentable sin duplicación. | P01, P10, P37, instrucciones reproducibles. |
| RNF09 | Compatibilidad y alcance. Windows, Visual Studio 2022 compatible, C#/.NET 8, WinForms, EF Core 8, SQLite y Microsoft.Extensions.DependencyInjection. Colaboradores simulados y comprobantes internos. | Compilación, UI, esquema relacional real y demostración sin servicios externos. |
| RNF10 | Calidad y autoría. Registrar al menos diez riesgos reales con refactorizaciones, Git y pruebas antes/después. Aplicar cinco principios SOLID y Clean Code/KISS/DRY/YAGNI. | Historial individual, commits y defensa reproducible. |

## Guion por wireframe

| Página | Vista | Controles, información y vínculo de verificación |
| --- | --- | --- |
| 11 | Acceso | Usuario, contraseña enmascarada, ingresar y perfil. Añadir cierre de sesión en navegación. RF01, P01/P22. |
| 11 | Panel general | Menú lateral, rol/sesión, pedidos activos, por despachar, stock bajo, ventas, pedidos recientes y alertas de stock/Outbox/devoluciones. RF16, P15/P28/P37. |
| 12 | Catálogo y kits | Buscar SKU/nombre/categoría, alta de producto/kit, grilla SKU–producto–precio–stock, árbol anidado y guardar composición. RF02–RF03, P18/P21. |
| 12 | Clientes | Buscar por nombre/identificación, lista código/nombre/categoría/contacto y ficha con alta, edición, guardado o cancelación. RF04, P23. |
| 13 | Carrito y borrador | Selector de cliente y entrega, añadir productos/kits, renglones con cantidad/precio/importe, quitar, deshacer/rehacer, desglose y validación. RF05–RF08, P02–P05/P13–P14/P29. |
| 13 | Pedidos e historial | Búsqueda, filtros por estado, código/cliente/estado/total/fecha y secuencia histórica del pedido seleccionado. RF08/RF18, P08–P10/P31. |
| 14 | Confirmación y cobro | Pedido validado y desglose, proveedor Legacy/alternativo, confirmación protegida, resultado y referencia. RF09/RF17, P06–P07/P16/P19/P22. |
| 14 | Despacho y entrega | Grilla de pedidos pagados/despachados, servicio de entrega, guía interna, acciones de despacho y entrega. RF10, P09/P12. |
| 15 | Devoluciones | Pedido entregado, motivo, fecha, condiciones de elegibilidad, reintegro aplicable, importe y confirmación. RF11, P33/P35. |
| 15 | Inventario y movimientos | SKU, producto, existencias, alertas, ajustes auditados y movimientos con referencia. RF12, P02–P07/P35–P36. |
| 16 | Reportes y comprobantes | Fechas, filtros, pedidos, ventas netas, devoluciones, vista de resultados y vista previa/exportación TXT. RF13–RF14, P17/P25–P26/P34. |
| 16 | Auditoría y simulación | Hora, evento y resultado, cambio de proveedor, rechazo de cobro, fallo Outbox y reproceso de pendientes en modo demo. RF15/RF17, P19/P22/P30/P37. |

Usar DataGridView en productos, pedidos, inventario, movimientos y auditoría. El diseño visual admite cambios de distribución conservando funciones, campos pertinentes, permisos y respuestas. Las cifras y nombres ilustrativos de los wireframes ceden ante las reglas oficiales, especialmente P03 Memoria USB, stocks de carga y devolución de Ana por USD 54.00. En reportes, el medio explícito de archivo es TXT.

## Obligaciones de evidencia y entrega

En la carpeta de proyecto se preparará la instalación desde código limpio y una demostración de extremo a extremo, incluidos rechazo, reintento y reinicio. Capturar datos antes/después y las 12 áreas de WinForms. Para P19, P22 y P37 registrar secuencias de trazas, ausencia de invocaciones prohibidas y recuperación. Para P21 conservar identidad, instancias, tamaño de muestra y condiciones comparables. El cierre incluirá cobertura 23/23 sustentada, aprobadas/ejecutadas, fallos y correcciones reproducibles, cifras, riesgos y decisiones demostradas. Prohibidos lógica de negocio en formularios o Program.cs, SQL/EF en capas internas, concretos de persistencia en casos de uso, formularios acoplados, clases vacías presentadas como patrones, resultados fijos, secretos reales y ocultación de fallos. La solución es nueva, de autoría individual verificable y sin reutilizar APE anteriores. El alcance prescinde de cobros/envíos externos, IVA, facturas tributarias, REST, nube y contenedores.

1. **APE04_LogiMarket360_APELLIDOS_NOMBRES.zip**. Solución .sln/.slnx, cinco proyectos, formularios, .csproj, migraciones, seed, configuración de ejemplo y README con requisitos, instalación .NET 8/paquetes, restauración, compilación, migración/seed, ejecución, cuentas demo, pruebas y reinicio seguro. Excluir bin/, obj/, .vs/, contraseñas reales y base de ejecución con información personal.
2. **APE04_Informe_APELLIDOS_NOMBRES.pdf**. Orden obligatorio: portada institucional, resumen ejecutivo, problema/requisitos, dominio y datos, arquitectura/dependencias, Clean Code/SOLID, creacionales, estructurales, comportamentales, desarrollo visual/flujo, pruebas/resultados, riesgos/limitaciones, conclusiones por objetivo, recomendaciones por conclusión y limitación verificable, referencias IEEE y anexos. Las recomendaciones separarán mejoras futuras de obligaciones. Anexos complementan la entrega y defensa.

La guía se desarrolla en diez actividades. Sus entregables y controles están trazados en el [plan maestro](plan-maestro.md).


## Avance verificado en fase 1

| Requisito | Resultado de fase | Evidencia |
| --- | --- | --- |
| RF01 | Cuatro roles y usuarios ficticios con hash. Quedan inicio/cierre de sesión y autorización por caso de uso. | P01, `InicializacionTests`. |
| RF02 | Producto con invariantes, SKU único e índices. Quedan CRUD, permisos y filtros de interfaz. | `CatalogoTests`, `RestriccionesSqliteTests`. |
| RF03 | Kits anidados, cantidades y detección de ciclos en dominio y SQLite. Queda la edición en WinForms. | P18. |
| RF04 | Cliente con categoría, disponibilidad y repositorio. Queda gestión desde Application y WinForms. | `PedidoTests`, `PersistenciaTests`. |
| RF05 y RF08 | Pedido, líneas, instantáneas, transiciones e historial. Pendientes Builder, Chain, comandos y flujos por rol. | P31, `PedidoTests`, `PersistenciaTests`. |
| RF07, RF09–RF12, RF15 y RF18 | Subtotal histórico, almacenamiento de registros operativos, transacción, concurrencia y reapertura de SQLite. Quedan las políticas y flujos completos y el procesamiento Outbox. | `PersistenciaTests`, `RestriccionesSqliteTests`. |
| RNF01, RNF02, RNF05, RNF07–RNF09 | Integridad y unicidad en esquema, grafo de cinco proyectos, hash, 49 pruebas aprobadas, migración y seed comprobados. Queda la validación integral. | P01, `ReferenciasTests` e historial. |

Los demás requisitos conservan sus comprobaciones previstas. En el ejemplo de Carmen, MAYORISTA identifica la promoción por las diez unidades físicas. El dominio usa NORMAL y VIP como categorías de cliente.
