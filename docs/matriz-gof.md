# Matriz de los 23 patrones GoF

Las filas describen el diseño previsto. La sección final registra las implementaciones y comprobaciones realizadas. Después de integrar cada patrón registrar ruta y método reales, commit, recorrido, resultado de prueba y evidencia del colaborador. Los ejemplos de evidencia son instrucciones de captura.

D = Domain; A = Application; I = Infrastructure; W = WinForms. Las rutas propuestas son relativas al proyecto indicado.

## Creacionales

| Patrón y fuente | Problema y participación real | Participantes y punto previsto | Verificación y evidencia | Beneficio y costo |
| --- | --- | --- | --- | --- |
| Factory Method · p. 5 | Los eventos de pedido pagado, despacho y devolución deben enviar avisos por canal elegido fuera de UI. Cada creador redefine la fábrica. | I/Notificaciones/CreadorAviso.cs `CrearNotificador`, creadores Email/Sms, notificadores bajo un mismo contrato. | P11, A11. Alternar canal y observar emisor concreto, registro y mensaje simulado. | Cambia canal sin acoplar UI. Mantiene dos creadores. |
| Abstract Factory · p. 6 | Precio, plazo y etiqueta/guía deben pertenecer a una familia de entrega compatible. | D/Entregas/IFabricaEntrega.cs `CrearTarifa`, `CrearPlazo`, `CrearGuia`; fábricas Estándar/Exprés. | P12, A12. 5/3 y 12/1, guía de la misma familia. | Evita combinaciones incoherentes. Añadir familia implica implementar sus tres productos. |
| Builder · p. 6 | Construir un agregado completo con cliente, líneas simples/anidadas, entrega y snapshots. | D/Pedidos/PedidoBuilder.cs `Construir`, agregado Pedido. | P13, A13. Multilínea válida, vacío/sin cliente rechazados y sin agregado parcial. | Centraliza construcción válida. Requiere sincronizar invariantes del builder y agregado. |
| Prototype · p. 6 | Reutilizar un borrador como plantilla sin alias de colecciones. | D/Pedidos/PedidoBorrador.cs `ClonarComoBorrador`, líneas y componentes. | P14, A14. Identidad nueva, copia profunda, edición no afecta plantilla ni copia anterior. | Acelera pedidos repetidos. Copia y mantenimiento proporcional al grafo. |
| Singleton · p. 6 | Dos vistas necesitan métricas de sesión compartidas y seguras. | A/Metricas/MetricasSesion.cs `RegistrarEvento` e `Instantanea`, registro AddSingleton en W. | P15, A15. Misma identidad y acumulación concurrente deduplicada; dos vistas coherentes. | Unifica contadores. Vida de sesión y concurrencia requieren diseño explícito. |

## Estructurales

| Patrón y fuente | Problema y participación real | Participantes y punto previsto | Verificación y evidencia | Beneficio y costo |
| --- | --- | --- | --- | --- |
| Adapter · p. 6 | La pasarela legada incompatible cobra centavos. Preservar su código y permitir segundo proveedor real simulado. | I/Pagos/PasarelaLegadaAdapter.cs `CobrarAsync(decimal, clave)` hacia `CobrarCentavos(long, string)`, proveedor alternativo y puerto IPasarelaPago en A. | P16, A16/A39. Ambos ejecutan cobro y satisfacen contrato; comprobar conversión y resultados/error. | Aísla legado. Obliga a normalizar moneda, claves y errores. |
| Bridge · p. 6 | Dos comprobantes necesitan dos medios intercambiables sin duplicar combinaciones. | A/Documentos/Comprobante.cs `Emitir`, ComprobanteVenta/Devolucion; ISalidaDocumento, VistaPrevia y ArchivoTXT, con render visual W y escritura I. | P17, A17. Cuatro salidas, TXT real y vista visible. | Evolucionan contenido y medio por separado. Exige contrato común de presentación. |
| Composite · p. 6 | Tratar productos y kits anidados uniformemente y descontar SKU físicos. | D/Catalogo/IElementoCatalogo.cs, ProductoSimple, KitCompuesto `CalcularPrecio` y `AgregarComponente`. | P18, A18. Precios 50/60, cantidades finales, ciclos directos/indirectos rechazados. | Unifica operaciones recursivas. Requiere proteger el grafo. |
| Decorator · p. 6 | Añadir trazas de cobro correlacionadas a cualquier proveedor. | I/Pagos/AuditoriaPagoDecorator.cs `CobrarAsync`, IPasarelaPago y puerto de trazas. | P19/P22, A19/A22. INICIO → PASARELA → FIN/ERROR en autorizado, sin nuevo LOG en denegación. | Agrega auditoría sin alterar resultado. El orden de composición y manejo de trazas importan. |
| Facade · p. 6 | Confirmar desde un solo punto coordinando permisos, validaciones, valoración, pago, persistencia, stock y Outbox. | A/Pedidos/ConfirmacionPedidoFacade.cs `ConfirmarAsync`, puertos y políticas. | P02/P05–P07/P20, A20. Éxito, rechazo, duplicado y rollback ante fallo de colaborador. | Reduce acoplamiento del invocador. Evitar concentrar reglas o SQL en la fachada. |
| Flyweight · p. 7 | La vista de catálogo repite nombre, categoría y descripción en 1.000 o más filas. | A/Catalogo/CatalogoVisualFlyweightFactory.cs `ObtenerMetadatos`, dato intrínseco inmutable, stock/cantidad/precio externos. | P21, A21. ReferenceEquals, instancias y memoria con/ sin compartición y mismas filas/condiciones. | Comparte metadatos. Requiere política de actualización al cambiar el catálogo. |
| Proxy · p. 7 | Proteger el puerto de cobro aun fuera de formularios. | A/Pagos/PagoAutorizadoProxy.cs `CobrarAsync`, política VENTAS/ADMIN, después Decorator y Adapter/proveedor. | P22, A22. CONSULTA/BODEGA sin llamada ni LOG de cobro. VENTAS/ADMIN sí delegan. | Control antes de efectos. Requiere política consistente con la fachada. |

## Comportamentales

| Patrón y fuente | Problema y participación real | Participantes y punto previsto | Verificación y evidencia | Beneficio y costo |
| --- | --- | --- | --- | --- |
| Chain of Responsibility · p. 7 | Validar cliente activo → líneas/cantidades → stock → autorización/condiciones en orden. | A/Pedidos/Validacion/ValidadorPedido.cs `Validar`, cuatro eslabones y rechazo estructurado. Autorización de entrada independiente. | P23, A23. Primer error corta cadena; corregir revela el siguiente sin cobrar. | Cada regla evoluciona sola. El orden debe ser visible, probado y compatible con invariantes. |
| Command · p. 7 | Compartir acciones UI y pruebas, registrar ejecución. | A/Comandos/InvocadorComandos.cs `EjecutarAsync`, ConfirmarPedidoCommand, CancelarPedidoCommand, DespacharPedidoCommand, ProcesarDevolucionCommand y sus receptores. | P24, A24. Mismo objeto/handler desde botón y prueba, entrada y resultado trazables. | Desacopla vista y acción. Suma tipos y registro de handlers. |
| Interpreter · p. 7 | Consultar mediante gramática ESTADO = valor, TOTAL >= número, CLIENTE = texto con AND/OR y paréntesis. | A/Reportes/ParserFiltroPedido.cs `Parsear`, AST tipado y aplicación segura en consulta. | P25–P26, A25–A26. Resultados filtrados, precedencia, paréntesis y error legible sin SQL concatenado. | Permite ampliar filtros de forma controlada. Hay que mantener gramática y mensajes. |
| Iterator · p. 7 | Exponer recorrido físico determinista sin entregar colecciones internas. | D/Catalogo/IteradorComponentesFisicos.cs `Recorrer`, navega KitCompuesto y propaga multiplicidades. | P27, A27. P01/P02/P03, cantidades anidadas y terminación. | Encapsula navegación. Gestiona pila/recursión y orden. |
| Mediator · p. 7 | Catálogo, carrito, pedidos y métricas deben actualizarse sin invocarse entre sí. | W/Navegacion/FormularioPrincipalMediator.cs `NotificarOperacion`, coordinación aislable y puntos de vista suscritos. | P28, A28. Prueba de coordinación y captura de actualización real tras cobro. | Reduce dependencia entre vistas. Evitar concentrar negocio en el coordinador. |
| Memento · p. 7 | Recuperar ediciones de borrador con cinco estados completos como máximo y rehacer. | D/Pedidos/PedidoBorrador.cs `Capturar`/`Restaurar`, A/Pedidos/HistorialBorrador.cs cuidador. | P29, A29. Tres ediciones, dos deshacer y un rehacer; copia anidada, límite y bloqueo al confirmar. | Restauración encapsulada. Copias acotadas y reglas de invalidación. |
| Observer · p. 8 | Notificar, auditar y medir eventos de negocio durables después del commit. | A/Eventos/PublicadorEventos.cs `PublicarAsync`, observadores de aviso/métrica/auditoría; I/Outbox/ProcesadorOutbox.cs. | P30/P37, A30/A37. Tres suscriptores, tres tipos de evento, sin éxito en rollback ni duplicados tras reintento. | Extiende reacciones sin tocar transacción. Entrega y deduplicación deben manejar fallos parciales. |
| State · p. 8 | El estado define qué operaciones admite Pedido. | D/Pedidos/Estados/EstadoPedido.cs y objetos Borrador, Validado, Pagado, Despachado, Entregado y Cancelado. | P08/P09/P31, A08/A31. Ciclo, cancelación permitida, rechazo de retrocesos/repeticiones. | Distribuye reglas por estado. Persiste estado y rehidrata comportamiento. |
| Strategy · p. 8 | Añadir y priorizar promociones excluyentes sin editar calculadora central. | D/Precios/CalculadoraPedido.cs `Calcular`, IPoliticaDescuento, SinDescuento, Vip10, Mayorista15. | P32, A32. 6/15/0, prioridad mayorista aun VIP y extensión por registro. | Abre extensión sin alterar cálculo. Políticas deben declarar prioridad y elegibilidad. |
| Template Method · p. 8 | Reusar secuencia de devolución y variar elegibilidad/inspección. | A/Devoluciones/ProcesarDevolucion.cs `EjecutarAsync` fija elegibilidad → condición → reembolso → registro devolución/stock → evento; variantes Defecto y Arrepentimiento. | P33/P35, A33/A35. Día 20 acepta defecto y día 10 rechaza arrepentimiento sin efectos. | Protege el orden. Mantener acotados los pasos sobrescribibles. |
| Visitor · p. 8 | Obtener desglose físico y peso sin añadir cada reporte a productos/kits. | D/Catalogo/Visitantes, `Aceptar` en ProductoSimple/KitCompuesto y visitantes DesgloseSku/PesoLogistico. | P34, A34. Tres SKU y 1.00 kg en visita anidada. | Se agregan reportes sin modificar cálculos del catálogo. Cambiar los tipos visitables afecta visitantes. |

Anotar en el código real cerca del punto de colaboración con comentarios como `// patron strategy: selecciona la politica de descuento`. Los identificadores C# mantienen sus convenciones.


## Patrones verificados en fase 1

| Patrón | Colaboración real y archivos | Evidencia |
| --- | --- | --- |
| Composite | `Catalogo/IElementoCatalogo.cs`, `ProductoSimple`, `KitCompuesto` y `KitComponente`. `Capturar` conserva el árbol, `Expandir` acumula SKU físicos y `PedidoDetalle` guarda la composición histórica. El seed construye los kits y el repositorio carga la composición desde SQLite. | P18, `CatalogoTests`, `InicializacionTests`, `RestriccionesSqliteTests` y `PersistenciaTests`. |
| State | `Pedidos/Pedido.cs` delega acciones a `EstadoDelPedido`. El agregado registra cambios de estado y rechaza edición/cancelación indebidas. Los estados terminales comparten un comportamiento cerrado. | P31 del agregado, `PedidoTests` y persistencia del historial en `PersistenciaTests`. |

El avance posterior se registra a continuación.

## Patrones integrados en fase 2

| Patrón | Colaborador en operación | Verificación |
| --- | --- | --- |
| Strategy y Abstract Factory | `CalculadoraPedido`, `ValorarPedido`, `FabricaEntrega`; cotización, cobro y despacho. | P12, P32 y cifras oficiales P02–P04. |
| Builder y Chain of Responsibility | `PedidoBuilder`, `CadenaValidacionPedido`; borrador, validación y confirmación. | P13 y P23. |
| Proxy, Decorator y Adapter | `PagoAutorizadoProxy`, `AuditoriaPagoDecorator`, `PasarelaLegadaAdapter`; pago autorizado y proveedor alterno. | P16, P19, P22; regresión de cierre de sesión antes de invocar. |
| Facade | `ConfirmacionPedidoFacade`; coordina la transacción sin SQL ni cálculo de descuentos. | P02–P07, P20 y P36. |
| Command | `InvocadorComandos`, `OperacionesPedido`, cuatro comandos; ejecución desde panel y pruebas. | P24 parcial, auditoría de éxito/rechazo verificada por integración. |
| Template Method | `ProcesarDevolucion` y variantes por defecto o arrepentimiento. | P33, P35. |
| Observer y Factory Method | `PublicadorEventos` y tres observadores; `CreadorEmail` o `CreadorSms` generan avisos locales. | P11 y P30; recuperación técnica de P37. |
| Singleton | `MetricasSesion` compartida por DI con deduplicación por evento. | Identidad y recuento probados. P15 parcial hasta demostrar dos vistas. |

Estos ocho patrones se integraron en la fase 3. Sus comprobaciones se detallan a continuación.

## Patrones integrados en fase 3

| Patrón | Problema y flujo real | Clases y métodos | Resultado comprobado | Beneficio y costo |
| --- | --- | --- | --- | --- |
| Prototype | Reusar la instantánea de un borrador desde el panel aunque el kit vigente se haya editado. | `AdministrarPedidos.ClonarBorradorAsync`, `Pedido.ClonarComoBorrador`, `PedidoDetalle.CopiarPara`. | P14. Dos copias persistidas con identidad nueva. Editar una no modificó otra ni la plantilla. Prueba `PatronesBorradorTests`. | Reutiliza pedidos. Crear la copia cuesta una asignación por línea y componente. |
| Bridge | Emitir dos comprobantes por dos salidas con formato común. | `Comprobante.EmitirAsync`, `ComprobanteVenta`, `ComprobanteDevolucion`, `VistaPrevia`, `ArchivoTXT`, `EmitirComprobantes.EjecutarAsync`. | P17. Cuatro combinaciones en `ComprobantesTests` y en la ventana real. Vista y TXT de venta 59.00, devolución 54.00. | Evolución independiente de formato y medio. Requiere un contrato compartido para el documento. |
| Flyweight | Reducir instancias intrínsecas cuando una proyección repite SKU. | `CatalogoVisualFlyweightFactory.CrearRenglon`, `ConsultarCatalogoVisual.DemostrarAsync`. | P21. 1.000 renglones, 5 identidades compartidas frente a 1.000. Se midieron 56.752 frente a 104.024 bytes asignados. `FlyweightTests` y panel. | Evita objetos repetidos. Usa un diccionario por lote y no promete ahorro cuando todos los SKU son distintos. |
| Interpreter | Contestar filtros de pedidos sin concatenar SQL. | `ParserFiltroPedido.Parsear`, `FiltroPedidos.Cumple`, `ConsultarReportePedidos.EjecutarAsync`, `LecturaReportePedidos`. | P25 y P26. Filtro oficial devuelve solo el pago de 59.00. Precedencia, paréntesis y errores verificados en `FiltroPedidosTests`. | Semántica testeable y extensible. Evalúa una proyección materializada en esta versión. |
| Iterator | Consultar hojas físicas de manera determinista para expandir existencias. | `IteradorComponentesFisicos.Recorrer`, `ComponenteCatalogoSnapshot.Expandir`, `AdministrarCatalogo.ConsultarExistenciasAsync`. | P27. P01/P02/P03 en orden, correctas multiplicidades, ramas compartidas y control de desbordamiento. `RecorridosCatalogoTests`. | Oculta estado de recorrido. Materializa una instantánea y usa una pila adicional. |
| Mediator | Refrescar vistas después de la acción sin llamadas entre formularios. | `FormularioPrincipalMediator.NotificarAsync`, `VentanaComercial.ConfigurarConsultas`, `RefrescarAsync`. | P28. Cinco colaboradores por integración. En WinForms se observó PAGADO, stock 7/4/19, carrito cerrado y métrica de un pedido cobrado. `MediadorComercialTests`. | Reduce acoplamiento. Deben mantenerse las reglas de suscripción y controlar fallos en una vista. |
| Memento | Restaurar líneas anidadas y modalidad sin revertir pagos. | `Pedido.CapturarBorrador`, `RestaurarBorrador`, `HistorialBorrador`, `AdministrarPedidos.DeshacerAsync` y `RehacerAsync`. | P29. Tres ediciones, dos deshacer, un rehacer, límite de cinco, descarte de rama y bloqueo tras el pago. `PatronesBorradorTests`. | Recupera estados de forma encapsulada. Usa memoria temporal y sustituye identidades internas de detalle al restaurar. |
| Visitor | Mantener el detalle logístico fuera de las entidades de catálogo. | `IElementoCatalogo.Aceptar`, `VisitanteDesgloseSku`, `VisitantePesoLogistico`, `AdministrarCatalogo.ConsultarLogisticaAsync`. | P34. Se obtuvieron P01/P02/P03 y 1.00 kg para una unidad del kit, y 3.00 kg para tres. `RecorridosCatalogoTests`. | Permite agregar reportes sin editar el cálculo del catálogo. Un nuevo tipo visitable exige ampliar el contrato de visitantes. |

## Estado acumulado

Los 23 patrones tienen colaboradores funcionales y prueba técnica. Command completó en esta fase la comprobación visual del botón de confirmación junto con la prueba de invocación y auditoría existente. Singleton aún requiere la verificación de métricas desde dos vistas distintas de P15. La revisión de P38 y las demás comprobaciones parciales siguen en la matriz de aceptación.
