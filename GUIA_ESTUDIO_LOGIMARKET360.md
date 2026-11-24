# Guía de estudio y sustentación de LogiMarket 360

Esta guía acompaña al código auditado en `12458bc`. Las rutas se interpretan desde la raíz de la solución. Busca los nombres de clases o métodos desde Visual Studio con **Ctrl+T**. Los casos de aceptación, sus evidencias históricas y los resultados automatizados se detallan en el informe y la [matriz P01–P38](docs/matriz-aceptacion.md).

## 1. Arranque y capas

1. `src/LogiMarket.WinForms/Program.cs`, `Main`, lee `--demo` y `--solo-inicializar`. `CrearHost` decide la ruta SQLite y construye el contenedor. `PrepararPersistenciaAsync` pide al servicio `DatosIniciales.InicializarAsync` que migre y siembre los datos. Solo después abre `VentanaComercial`.
2. `src/LogiMarket.Infrastructure/Persistencia/RegistroPersistencia.cs`, `AgregarPersistenciaLogiMarket`, registra el contexto por ámbito, repositorios, pasarelas, observadores y casos de uso. Registra la sesión y las métricas con vida compartida dentro del proceso.
3. La vista de acceso usa `AutenticacionLocal.IniciarAsync` en `src/LogiMarket.Application/Seguridad/SesionLocal.cs`. El hash se verifica con `Pbkdf2HashClave` en Infrastructure. La sesión suministra la identidad y `AutorizacionOperaciones.ExigirAsync` aplica el permiso de la operación y audita denegaciones.
4. ADMIN puede operar y usar las herramientas autorizadas de demostración. VENTAS mantiene clientes y prepara, valida, cancela cuando corresponde y cobra pedidos. BODEGA consulta y despacha o entrega. CONSULTA consulta. La interfaz refleja la política y Application vuelve a comprobarla.

| Proyecto | Responsabilidad y punto de entrada |
| --- | --- |
| Domain | Agregados e invariantes de producto, kit, cliente y pedido. Empieza por `Catalogo/KitCompuesto.cs`, `Pedidos/Pedido.cs` y `Pedidos/EstadoDelPedido.cs`. |
| Application | Casos de uso, puertos, permisos y coordinación. Depende de Domain. Empieza por `Pedidos/AdministrarPedidos.cs` y `Pagos/ConfirmacionPedidoFacade.cs`. |
| Infrastructure | EF Core 8, SQLite, migración, seed, repositorios, Outbox, hash y adaptadores. Implementa puertos de Application y trabaja con Domain. |
| WinForms | Presentación y composición. `VentanaComercial` crea vistas y resuelve servicios en ámbitos. Infrastructure se conecta en el arranque. |
| Tests | xUnit. Prueba el dominio, la separación de dependencias y los casos de uso con archivos SQLite nuevos, usando `BaseSqlite` y `EntornoComercial`. |

## 2. Sigue un pedido de extremo a extremo

- En `VentanaComercial.Pedidos.cs` carga el carrito. `AdministrarPedidos` construye el borrador con `PedidoBuilder` y agrega líneas a partir de `IElementoCatalogo.Capturar`. Las líneas guardan precio y componentes históricos en `PedidoDetalle` y `PedidoDetalleComponente`. Cambiar luego el catálogo no reescribe la venta.
- La validación recorre `CadenaValidacionPedido.RevisarAsync`, con los pasos `ClienteActivo`, `LineasPositivas`, `StockSuficiente` y `EstadoValido`. El agregado usa `EstadoDelPedido` para autorizar transiciones. En el flujo normal se observa BORRADOR → VALIDADO → PAGADO → DESPACHADO → ENTREGADO.
- El botón de cobro usa `OperacionesPedido` e `InvocadorComandos.EjecutarAsync` con `ConfirmarPedidoCommand`. `ConfirmacionPedidoFacade.ConfirmarAsync` revisa el permiso, abre la unidad de trabajo y consulta la clave. Para una operación nueva, revalida el pedido y valora con `ValorarPedido.Calcular`.
- `CalculadoraPedido` ordena promociones y toma la primera elegible. A partir de diez unidades físicas gana `Mayorista15`, incluso para VIP. Después se aplican la política VIP o el 0 % según corresponda. Usa `decimal` y redondeo `AwayFromZero`. `FamiliasEntrega` agrega tarifa, plazo y prefijo coherentes.
- `PagoAutorizadoProxy` comprueba el permiso antes de que `AuditoriaPagoDecorator` registre el inicio. `PasarelaLegadaAdapter` convierte el importe a centavos para el legado. `PasarelaAlternaSimulada` ofrece el mismo contrato. Tras la aprobación, la fachada descuenta el stock, guarda movimientos, pone PAGADO, registra pago y encola el evento. El commit confirma el conjunto.
- Cifras para defender en pizarrón. Ana VIP, kit de USD 60.00, descuento de USD 6.00 y flete de USD 5.00, cobra **USD 59.00**. Luis, P04×2 y P05×2 con exprés, cobra **USD 102.00**. Carmen, P03×10 y estándar, cobra **USD 90.00** con descuento de USD 15.00.

## 3. SQLite, idempotencia y concurrencia

- Lee `LogiMarketDbContext`, `Migraciones/20261120214119_EsquemaInicial.cs` y `Modelo/OperacionesModelo.cs` en `src/LogiMarket.Infrastructure/Persistencia/`. Allí se comprueban relaciones y restricciones. `DatosIniciales` siembra solo lo que falta. Reiniciar conserva ajustes y movimientos.
- `UnidadDeTrabajoSqlite.EnTransaccionAsync` inicia la transacción, ejecuta el caso, escribe con `GuardarAsync` y confirma. Si hay excepción, limpia el rastreador y al salir se dispone de la transacción sin commit. El proveedor traduce conflictos optimistas al contrato interno. La prueba P20 fuerza un fallo y comprueba la ausencia de persistencia parcial.
- La fachada busca primero por clave de idempotencia normalizada. Si pertenece al mismo pedido, retorna el pago registrado. Si pertenece a otro, rechaza. Un segundo intento del mismo pedido con otra clave también se detiene ante un pago previo. SQLite protege la unicidad de la clave, la referencia y el pedido pagado. Mira `PagosRegistrados`, `OperacionesModelo` y `ConfirmacionTests`.
- El último stock se comprueba dentro de la misma transacción. `CantidadesFisicas` agrupa componentes de kits por SKU y la fachada consume exactamente los productos y cantidades revisados por la cadena. `ProductoSimple.MoverInventario` impide un resultado negativo. La concurrencia se verifica con dos confirmaciones sobre una unidad y dos ámbitos SQLite en P36.
- Las trazas diagnósticas del pago y las denegaciones pendientes tienen su propio registro. Por eso puede haber una traza de rechazo aunque no se haya guardado pago, movimiento ni evento de éxito.

## 4. Despacho, devolución y Outbox

- `Application/Entregas/DespacharPedidos.cs` aplica permisos y transición de despacho/entrega. La modalidad determina tarifa, plazo y etiqueta. Sigue la guía que se genera y el registro persistido.
- `Application/Devoluciones/ProcesarDevolucion.cs` contiene `AdministrarDevoluciones.PrevisualizarAsync` y `DevolverAsync`. El template verifica pedido entregado y devolución única, consulta pago y entrega, aplica motivo y plazo y registra reembolso simulado, reintegro y evento. DEFECTO admite hasta 30 días. ARREPENTIMIENTO admite hasta siete días y exige producto sin uso. Ana recibe **USD 54.00 = 59.00 − 5.00** y se repone una unidad de P01, P02 y P03. `LogisticaTests` comprueba la negativa, la persistencia y el reintento.
- `ColaEventosSqlite` guarda el evento en la misma transacción del caso. `ProcesadorOutboxSqlite.ProcesarAsync` toma hasta 100 pendientes ordenados, suma intentos y publica. Si un observador falla, conserva el evento pendiente con el último error. `EntregaDurableSqlite.EntregarAsync` deduplica efectos durables por evento y observador. `MetricasSesion` mantiene las métricas deduplicadas por evento en memoria. Un reintento entrega avisos locales, registra auditoría y actualiza métricas sin invocar otra vez el cobro.
- Distingue el punto de durabilidad de la sesión visual. Los registros SQLite reaparecen al reiniciar. Las tarjetas de sesión pueden empezar de cero porque se crea una nueva instancia del host. P15 compara dos vistas del mismo proceso.

## 5. Los 23 patrones que puedes señalar en el código

| Patrón | Participantes reales | Qué mostrar |
| --- | --- | --- |
| Factory Method | `Eventos/AvisosSimulados.cs`: `CreadorAviso.Crear`, `CreadorEmail`, `CreadorSms` | El observador selecciona un creador y este entrega el aviso concreto. P11. |
| Abstract Factory | `Entregas/FamiliasEntrega.cs`: `FabricaEntrega.Crear`, `FabricaEntregaEstandar`, `FabricaEntregaExpres` | Tarifa, plazo y etiqueta salen de la misma familia. P12. |
| Builder | `Pedidos/PedidoBuilder.cs`: `PedidoBuilder`, `AdministrarPedidos` | Construye el agregado e impide un resultado incompleto en el flujo probado. P13. |
| Prototype | `Pedido.ClonarComoBorrador`, `PedidoDetalle.CopiarPara`, `AdministrarPedidos.ClonarBorradorAsync` | Una nueva identidad y líneas/componentes independientes. P14. |
| Singleton | `Eventos/MetricasSesion.cs`, registro `AddSingleton` en `RegistroPersistencia` | Panel y Auditoría leen el mismo contador. P15. |
| Adapter | `Infrastructure/Pagos/PasarelasSimuladas.cs`: `PasarelaLegadaAdapter`, `IPasarelaPago` | El legado recibe centavos y Application trabaja con importe decimal. P16. |
| Bridge | `Comprobante.EmitirAsync`, `ComprobanteVenta`, `ComprobanteDevolucion`, `ISalidaDocumento`, `VistaPrevia`, `ArchivoTXT` | Dos comprobantes combinan con dos medios. P17. |
| Composite | `Domain/Catalogo`: `IElementoCatalogo`, `ProductoSimple`, `KitCompuesto`, `KitComponente` | KIT_TELETRABAJO contiene KIT_OFICINA y P03. P18. |
| Decorator | `Pagos/AuditoriaPagoDecorator.cs`, `ITrazasPago`, `IPasarelaPago` | Envuelve la llamada y conserva su resultado. P19. |
| Facade | `Pagos/ConfirmacionPedidoFacade.cs` | Coordina autorización, validación, pago, movimientos y Outbox desde un punto. P20. |
| Flyweight | `CatalogoVisualFlyweightFactory.CrearRenglon`, `ConsultarCatalogoVisual.DemostrarAsync` | Los metadatos se comparten. Existencias, precio y cantidad pertenecen a la fila. P21. |
| Proxy | `Pagos/PagoAutorizadoProxy.cs` y `AutorizacionOperaciones` | Deja la protección antes de Decorator y del proveedor. P22. |
| Chain of Responsibility | `CadenaValidacionPedido` y sus cuatro pasos | La primera validación fallida detiene la cadena. P23. |
| Command | `ComandosPedido.cs`, `InvocadorComandos`, `OperacionesPedido` | La UI y la prueba usan el mismo comando y quedan auditadas. P24. |
| Interpreter | `ParserFiltroPedido.Parsear`, `FiltroPedidos.Cumple`, `ConsultarReportePedidos` | Un árbol tipado aplica AND, OR y agrupación a las proyecciones. P25/P26. |
| Iterator | `IteradorComponentesFisicos.Recorrer`, `ComponenteCatalogoSnapshot.Expandir` | Hojas físicas y multiplicidades de kits anidados. P27. |
| Mediator | `FormularioPrincipalMediator.NotificarAsync`, `VentanaComercial.ConfigurarConsultas` | Un cambio refresca las vistas suscritas según destino. P28. |
| Memento | `Pedido.CapturarBorrador`, `RestaurarBorrador`, `HistorialBorrador` | Deshacer y rehacer cinco estados como máximo dentro del borrador. P29. |
| Observer | `PublicadorEventos`, `ObservadorAvisos` y observadores de auditoría y métricas | Tres reacciones al mismo evento después del commit. P30/P37. |
| State | `Pedido`, `EstadoDelPedido`, `EstadoPedido` | La operación permitida depende del estado persistido. P31. |
| Strategy | `CalculadoraPedido`, `IPoliticaDescuento`, `SinDescuento`, `Vip10`, `Mayorista15` | Políticas excluyentes y extensión por registro. P32. |
| Template Method | `ProcesarDevolucion`, `DevolucionPorDefecto`, `DevolucionPorArrepentimiento` | La secuencia de devolución se conserva y la elegibilidad varía. P33. |
| Visitor | `IElementoCatalogo.Aceptar`, `VisitanteDesgloseSku`, `VisitantePesoLogistico` | Los visitantes recorren el árbol y obtienen desglose y 1.00 kg. P34. |

## 6. SOLID con ejemplos durante la defensa

- **SRP.** `ConfirmacionPedidoFacade` coordina, `ValorarPedido.Calcular` encapsula valoración y `UnidadDeTrabajoSqlite` maneja la transacción. Los escenarios oficiales vuelven a probar el resultado después de extraer cálculos repetidos y consolidar cantidades físicas.
- **OCP.** `CalculadoraPedido` recibe políticas de descuento por el constructor. La prueba de P32 registra una estrategia adicional y conserva la calculadora. Explica prioridad y elegibilidad, pues ambas influyen en la selección.
- **LSP.** `IPasarelaPago` permite sustituir legado por alterno y usar el mismo caso de uso. P16 verifica contrato y conversión. P38 vuelve a contrastar con pruebas de dependencias y sustitución.
- **ISP.** `Catalogo/IRepositorioCatalogo.cs` separa `IConsultaCatalogo` y `IEscrituraCatalogo`. La cadena requiere consultas de existencias. La escritura se inyecta en `AdministrarCatalogo` para operaciones de mantenimiento.
- **DIP.** `ConfirmacionPedidoFacade` recibe `IUnidadDeTrabajo`, repositorios y puertos de pago. `RegistroPersistencia` aporta los concretos SQLite. `ReferenciasTests` inspecciona los proyectos y P20 sustituye un colaborador para probar fallo/rollback.

## 7. Los 38 casos y cómo demostrarlos

Crea una base por escenario que requiera el inventario inicial. Conserva la misma base para reinicio o reintento. Los IDs son de aceptación, mientras que una prueba xUnit puede cubrir varios casos o entradas. Ejecuta `dotnet test -c Release` y usa `--filter FullyQualifiedName~Pnn` cuando esa etiqueta esté en el método. Para los demás, filtra por la clase de la tabla. Los casos visuales conservan su evidencia en el informe.

| ID | Qué preparar, hacer y comprobar | Lugar para defender |
| --- | --- | --- |
| P01 | Arranca dos veces con archivo inexistente al inicio. Comprueba una migración, cinco productos, dos kits y cuatro roles sin duplicar. | `InicializacionTests` |
| P02 | Ana VIP, KIT_TELETRABAJO×1, estándar. Verifica 59.00, PAGADO, pago único y P01/P02/P03 en 7/4/19. | `ConfirmacionTests` |
| P03 | Luis NORMAL, P04×2 y P05×2, exprés. Verifica 102.00 y stocks 2/28. | `ConfirmacionTests` |
| P04 | Carmen, P03×10, estándar. Verifica 90.00, descuento 15.00 y stock 10. | `ConfirmacionTests` |
| P05 | Intenta confirmar KIT_TELETRABAJO×6. Comprueba rechazo por P02 y cero efectos de éxito. | `ConfirmacionTests` |
| P06 | Activa rechazo simulado en un pedido válido. Comprueba VALIDADO, inventario intacto y ausencia de pago y evento de éxito. | `ConfirmacionTests` |
| P07 | Repite misma clave en el mismo pedido. Compara referencia, cantidad de pagos, proveedor invocado y movimientos. | `ConfirmacionTests` |
| P08 | Cambia P01 de USD 20.00 a 21.00 después del pago. Prueba cancelar y editar el pedido y coteja su instantánea e historial. | `ConfirmacionTests`, Catálogo y Carrito |
| P09 | BODEGA despacha y entrega un pagado. Intenta despachar otro pendiente de pago y comprueba rechazo. | `LogisticaTests` |
| P10 | Cierra el host y reábrelo con el mismo SQLite. Compara pedido, stock, pago y auditoría. | `PersistenciaTests`, interfaz |
| P11 | Procesa eventos separados con EMAIL y SMS. Verifica el aviso local elegido por cada creador. | `OutboxComercialTests` |
| P12 | Cotiza estándar 5.00/3 días y exprés 12.00/1 día. Compara el prefijo de guía con su familia. | `OperacionesComercialesTests` |
| P13 | Prueba construir sin datos obligatorios y después construye uno completo con líneas. | `OperacionesComercialesTests` |
| P14 | Clona un borrador con kit y cambia una cantidad en la copia. Comprueba ID nuevo e independencia. | `PatronesBorradorTests`, Carrito |
| P15 | En la misma sesión, procesa un evento y compara las métricas de Panel y Auditoría. Ejecuta la prueba concurrente. | `MetricasSesionTests` |
| P16 | Usa dos compras independientes con proveedor legado y alterno. Compara el contrato de pago y la conversión. | `ConfirmacionTests` |
| P17 | Genera venta y devolución en Vista previa y TXT. Abre ambos TXT y compara totales y referencias. | `ComprobantesTests`, Reportes |
| P18 | Consulta ambos kits, verifica 50.00 y 60.00 y prueba rechazo de un ciclo. | `CatalogoTests` |
| P19 | Compara INICIO / PASARELA / FIN y la ruta fallida de trazas de cobro. | `ConfirmacionTests`, Auditoría |
| P20 | Inyecta fallo de persistencia en la suite. Verifica rollback conjunto y diferencia entre trazas y efectos comerciales. | `ConfirmacionTests` |
| P21 | Pulsa Medir 1.000 filas. Comprueba 5 frente a 1.000 identidades y observa los bytes medidos. | `FlyweightTests`, Catálogo |
| P22 | Intenta cobrar como BODEGA y CONSULTA usando la prueba de Application. Verifica rechazo antes de la pasarela y de INICIO. | `SeguridadComercialTests`, `RevocacionCobroTests` |
| P23 | Prepara dos invalidaciones. Corrige la primera y comprueba que aparece la siguiente sin cobro. | Buscar `P23_` en Tests |
| P24 | Confirma desde el botón y ejecuta la prueba del invocador. Compara comando, resultado y traza. | `OutboxComercialTests` |
| P25 | Filtra con `ESTADO = PAGADO AND TOTAL >= 50` sobre pedidos variados. | `FiltroPedidosTests`, Reportes |
| P26 | Prueba `ESTADO =` y dos expresiones cuya agrupación cambie el resultado. | `FiltroPedidosTests`, Reportes |
| P27 | Recorre KIT_TELETRABAJO y coteja P01, P02 y P03 por unidad. | `RecorridosCatalogoTests` |
| P28 | Confirma y revisa pedidos, existencias y métricas. Distingue el refresco de vistas del procesamiento del evento. | `MediadorComercialTests` |
| P29 | Realiza tres ediciones, deshaz dos, rehace una y comprueba que el pedido pagado queda protegido. | `PatronesBorradorTests` |
| P30 | Procesa PedidoPagado y comprueba aviso, auditoría y métrica una vez cada uno. | `OutboxComercialTests` |
| P31 | Recorre estados permitidos y prueba rechazar PAGADO → CANCELADO. | `PedidoTests` |
| P32 | Compara descuentos VIP, mayorista y normal. Añade una política desde la prueba y verifica la prioridad. | `OperacionesComercialesTests` |
| P33 | Con el reloj de la prueba, devuelve por DEFECTO al día 20 y por ARREPENTIMIENTO al día 10. Compara efectos. | `LogisticaTests.P33_PoliticasDePlazoYCondicion` |
| P34 | Abre Ver composición para KIT_TELETRABAJO y coteja desglose y peso 1.00 kg. | `RecorridosCatalogoTests`, Catálogo |
| P35 | Devuelve a Ana desde ENTREGADO y repite. Comprueba USD 54.00, reintegro único y persistencia. | `LogisticaTests.P35_AnaRecibe54YUnSoloReintegro` |
| P36 | Usa dos ventas concurrentes sobre el último ejemplar y compara el estado final. | `ConfirmacionTests` |
| P37 | Fuerza fallo de notificación, reabre y reprocesa con el fallo desactivado. Cuenta los intentos y comprueba ausencia de cobro repetido. | `OutboxComercialTests`, Auditoría |
| P38 | Compila desde extracción limpia; ejecuta sustitución de proveedor, contratos segregados, pruebas de referencias y suite completa. | `ReferenciasTests`, `ContratosEscritorioTests` y P16/P38 |

## 8. Preguntas probables

1. **¿Por qué no descontar al crear el borrador?** El borrador permite cambiar o clonar líneas. La operación que hace el descuento físico es la confirmación aprobada dentro de la unidad de trabajo.
2. **¿Qué sucede si la misma clave se repite con otro pedido?** La fachada la rechaza. Un reintento del mismo pedido retorna el pago guardado. SQLite protege la unicidad aunque dos solicitudes compitan.
3. **¿Por qué el envío queda fuera del reembolso?** Se devuelve el importe de los artículos después del descuento. El cálculo toma el pago histórico y resta su tarifa.
4. **¿Qué distingue Chain, State y Strategy?** Chain decide el orden y primer fallo de validaciones. State controla el comportamiento permitido según etapa del pedido. Strategy escoge la política intercambiable de promoción.
5. **¿Qué diferencia Composite, Iterator y Visitor en el mismo kit?** Composite modela la estructura. Iterator recorre las hojas y multiplica cantidades. Visitor ejecuta operaciones separadas, como peso y desglose, sobre los tipos visitados.
6. **¿Qué queda al fallar un notificador?** El evento queda pendiente, registra el intento y puede reprocesarse. Los efectos durables ya entregados se reconocen por evento y observador.
7. **¿Qué evidencia existe para los diez riesgos?** El registro identifica trece. R01 y R10 son extracciones estructurales rastreables. Hay correcciones y mitigaciones adicionales y tres riesgos abiertos. RNF10 sigue parcial por el alcance de la evidencia diferencial histórica.
8. **¿Está verificado el 150 %?** Las pruebas de negocio y la evidencia visual son distintas. El informe especifica el cotejo a 120 DPI y el pendiente físico. La corrección a 144 DPI requiere la comprobación correspondiente.

## 9. Demostración sugerida de 8–10 minutos

1. Arranca una SQLite de práctica con el procedimiento del [README](README.md). Aísla la ruta con `LOGIMARKET360_DB` para el recorrido funcional sin modo demo. Para las herramientas de fallo, `--demo` usa la ubicación fija de demostración del usuario Windows. Usa una cuenta Windows de prueba si esa ubicación ya contiene un escenario que deba preservarse.
2. Entra con ADMIN, abre Catálogo, muestra KIT_TELETRABAJO, peso, SKU y promesa de precio. Da una pasada rápida por Medir 1.000 filas.
3. Con VENTAS, registra Ana VIP, crea el borrador estándar y agrega un KIT_TELETRABAJO. Muestra clonar o deshacer/rehacer brevemente y valida el pedido original.
4. Confirma el cobro y muestra el total USD 59.00. Cambia de vista y compara el pedido y el inventario. Muestra el resultado de la prueba de idempotencia para acreditar la repetición exacta de la clave.
5. Con BODEGA, despacha y entrega. Con ADMIN, valida DEFECTO, confirma USD 54.00 y verifica el reintegro. Abre el comprobante y exporta TXT a una carpeta temporal.
6. En una base de demostración, fuerza un fallo del aviso, verifica el evento pendiente y reprocesa con el fallo desactivado. Compara métricas en la misma sesión. Reserva una captura y el resultado automatizado de P37 para el caso en que el tiempo sea corto.
7. Presenta la prueba de concurrencia P36, los resultados del ZIP limpio y los pendientes RNF03/RNF10 con su alcance exacto.

## 10. Orden de lectura para prepararte

1. `src/LogiMarket.WinForms/Program.cs` y `src/LogiMarket.Infrastructure/Persistencia/RegistroPersistencia.cs`. Explica de dónde salen los servicios.
2. `src/LogiMarket.Domain/Pedidos/Pedido.cs`, `Pedido.Borrador.cs`, `PedidoDetalle.cs` y `EstadoDelPedido.cs`. Distingue estado, transición y fotografía de venta.
3. `src/LogiMarket.Application/Pedidos/AdministrarPedidos.cs`, `CadenaValidacionPedido.cs` y `Pagos/ConfirmacionPedidoFacade.cs`. Traza los colaboradores en orden temporal.
4. `src/LogiMarket.Infrastructure/Persistencia/UnidadDeTrabajoSqlite.cs` y `Modelo/OperacionesModelo.cs`. Identifica el límite de la transacción y las restricciones.
5. `src/LogiMarket.Application/Devoluciones/ProcesarDevolucion.cs`, `src/LogiMarket.Infrastructure/Persistencia/Eventos/ProcesadorOutboxSqlite.cs` y `EntregaDurableSqlite.cs`. Prepara una explicación de fallos y recuperación.
6. `tests/LogiMarket.Tests/Integracion/ConfirmacionTests.cs`, `LogisticaTests.cs` y `OutboxComercialTests.cs`. Memoriza el escenario y las aserciones relevantes, no los números de línea.
