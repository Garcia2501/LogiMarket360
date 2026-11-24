# Auditoría integral de LogiMarket 360

Fecha: 2026-11-23. Base auditada: `a4e428b`, rama `feature/interfaz-winforms`. El trabajo se registró en `audit/calidad-integral`. La interfaz seguía fuera de `develop` (`4668f63`), porque las capturas al 125 % y 150 % reproducían recortes **antes** de la corrección DPI. No hay capturas equivalentes de la corrección a esas escalas. `main` permaneció en `9e5f254`.

**NO LISTO PARA ENTREGA.** El bloqueo es verificar la compilación corregida al 125 % y 150 %, con ventana normal y maximizada, ambos rótulos completos, cifras visibles y foco por teclado. La revisión de los 38 escenarios de guía y de sus patrones es independiente de esta comprobación de presentación. [Matriz](matriz-cumplimiento-final.md) · [Evidencias](evidencias-auditoria.md) · [Riesgos](riesgos-refactorizaciones.md)

## Alcance y arquitectura

Fuente: Word original de 20 páginas y 12 tablas, SHA-256 `b99ac3716152b99c42fbfedc97b230110a9a91e7f7ae93d8d16c0bf404adcd5a`. Se contrastaron las secciones de arquitectura, patrones y escenarios P01–P38 (pp. 4–8 y 17–18), los wireframes analizados en la fase anterior y la evidencia reutilizable. El archivo de la asignatura sigue fuera de Git.

| Proyecto | Referencias directas comprobadas | Verificación de comportamiento |
| --- | --- | --- |
| Domain | Ningún proyecto propio | `Pedido`, `EstadoDelPedido`, kits, snapshots y stock. Sin EF, SQLite ni UI. |
| Application | Domain | Casos de uso, puertos, permisos y estados. `OperacionesPedido` e `InvocadorComandos` ejecutan escrituras. `ConsultarReportePedidos` y `ConsultarEscritorio` proyectan lecturas. |
| Infrastructure | Domain y Application | `RepositorioPedidos.BuscarAsync` e inventario/catalogo EF. `UnidadDeTrabajoSqlite.EnTransaccionAsync` inicia, guarda y confirma; ante error se descarta el rastreo y se dispone la transacción. P20 y pruebas de persistencia verifican reversión real. |
| WinForms | Application e Infrastructure | `Program.CrearHost` es la referencia directa a Infrastructure y configura DI. La entrada de datos, formato, permisos visibles y navegación quedan en formularios; tarifas, descuentos, disponibilidad, permisos efectivos y transacciones se resuelven en los casos de uso. |
| Tests | Domain, Application e Infrastructure | `ReferenciasTests` verifica grafo y ensamblados internos. `ContratosEscritorioTests` construye DI con validación de ámbitos y contratos. |

## SOLID

| Principio | Estado y evidencia | Riesgo residual |
| --- | --- | --- |
| SRP | COMPLETO en casos revisados. `Pagos/ConfirmacionPedidoFacade.cs` delega políticas, comprobación de existencias y persistencia; `Precios/ValorarPedido.cs` compone subtotal, estrategia y tarifa. P02–P07 y P20. | La fachada tiene varios colaboradores; mantener responsabilidades acotadas al flujo. |
| OCP | COMPLETO. `Precios/CalculadoraPedido.cs`, método `Calcular`, recibe políticas por constructor y elige por prioridad. P32 registra una política nueva y prueba midpoint. `FamiliasEntrega.Obtener` selecciona fábricas registradas. | Prioridades empatadas requerirían definir criterio si se agregan políticas. |
| LSP | COMPLETO para `IPasarelaPago`. P16 sustituye Adapter legado y proveedor alterno desde `IResolutorPasarelas.Obtener`, prueba aprobación, repetición e importes inválidos. Se cotejaron trazas de ambas implementaciones. | Cambiar a un proveedor externo exigiría un contrato adicional de reconciliación. |
| ISP | COMPLETO en los puertos de P38. `Catalogo/IRepositorioCatalogo.cs` separa `IConsultaCatalogo` e `IEscrituraCatalogo`. `IColaEventos` e `IProcesadorOutbox` separan publicación durable de reproceso. `P38_PuertosSegregadosYComposicionDePantallas` comprueba firmas y registro. | `ILecturaEscritorio` agrupa varias consultas de apoyo; revisar si crecen consumidores independientes. |
| DIP | COMPLETO. `ConfirmacionPedidoFacade`, `AdministrarCatalogo` y `ConsultarEscritorio` inyectan puertos. `RegistroPersistencia.AgregarPersistenciaLogiMarket` conecta concretos. P38 valida dependencias y P20 sustituye el puerto de eventos para demostrar rollback. | Mantener el registro DI como punto de acoplamiento a EF y a proveedores concretos. |

## Resultados de negocio y consistencia

- P02–P04 usan bases distintas y comprueban Ana VIP 59.00 (6.00 de descuento), Luis NORMAL 102.00 y Carmen 90.00 (15.00 de descuento). Mayorista es una estrategia por cantidad, no un rol ni una categoría almacenada. P32 verifica prioridad sobre VIP, exclusión y redondeo decimal `AwayFromZero`.
- P01/P18 prueban kits 50.00 y 60.00, anidación, ciclos y expansión final. P27/P34 comprueban P01/P02/P03 y peso total 1.00 kg. P08 conserva datos e historial frente a recotización y edición de un pedido pagado.
- P35 verifica 54.00 sin flete, reintegro único, rechazo al duplicar después de reabrir SQLite y auditoría; las capturas del usuario corroboran la devolución y los saldos 8/5/20 desde WinForms.
- `Pbkdf2HashClave` usa PBKDF2-HMAC-SHA256 (600.000 iteraciones, sal de 16 bytes, derivación de 32 y comparación de tiempo fijo). Pruebas de seed comprueban cuatro hashes distintos y rechazo de credenciales erróneas. `AutorizacionOperaciones.ExigirAsync` y P22 prueban denegación auditable, con cero llamadas de cobro para BODEGA/CONSULTA.
- `ResolutorPasarelas.Obtener` conecta Proxy → Decorator → Adapter o alterno. P07/P20/P36 verifican recompra idempotente, reversión tras FK inválida y dos conexiones compitiendo por una unidad. Restricciones SQLite comprueban unicidad de pedido, clave, referencia, devolución y entrega por suscriptor.
- `ProcesadorOutboxSqlite.ProcesarAsync` trabaja sobre eventos confirmados y el reproceso es ADMIN. `EntregaDurableSqlite.EntregarAsync` registra efectos y marca por suscriptor de forma transaccional. P37 comprueba fallo, reapertura y recuperación sin duplicar cobro.

## Hallazgo corregido

**Indicadores obsoletos al cambiar el estado.** Antes del commit `66cd254`, un despacho autorizado persistía pero `FormularioPrincipalMediator.Destinos` omitía métricas. La prueba con BODEGA llegó a despachar y falló porque la vista de indicadores no recibió actualización. Después pasó y comprobó pendientes de despacho de 1 a 0 y Outbox de 1 a 2. La ruta compartida también cubre notificaciones de cancelación. La refactorización `ccc6cb3` elimina una segunda expansión física al confirmar y conserva las cantidades validadas para valoración y descuento de stock.

## Interfaz y decisión

Las doce vistas y sus pruebas previas se cruzan en [Evidencias](evidencias-auditoria.md). Las capturas del usuario acreditan devolución, inventario, auditoría y foco anterior al arreglo DPI. En el entorno de auditoría el escritorio solo pudo validarse a 96 DPI. La rama queda separada de `develop` hasta revalidar 120 y 144 DPI con la compilación auditada. No se han preparado archivos de entrega.
