# Matriz final de cumplimiento

Fuente de escenarios: guía APE 04, pp. 17–18. Esta revisión conserva las entradas y evidencia históricas de la [matriz por fase](matriz-aceptacion.md) y las contrasta con la suite final y [evidencia visual](evidencias-auditoria.md). Los estados expresan el alcance de cada requisito. **COMPLETO** significa que se encontró evidencia de sus condiciones obligatorias, **PARCIAL** indica evidencia incompleta, **INCUMPLIDO** corresponde a una condición contradicha por la ejecución y **NO VERIFICADO** identifica una ejecución pendiente.

## Aceptación P01–P38

| Caso | Precondición y entrada | Esperado y obtenido | Evidencia y método | Estado |
| --- | --- | --- | --- | --- |
| P01 | Base ausente; ejecutar seed dos veces | Esquema vigente, 5 productos, 2 kits, 4 roles y sin duplicados | InicializacionTests.P01; sqlite-arranque-limpio.json | COMPLETO |
| P02 | Ana VIP, teletrabajo ×1 estándar, base nueva | 59.00, stocks 7/4/19, PAGADO y pago único | ConfirmacionTests.P02_P03_P04 | COMPLETO |
| P03 | Luis NORMAL, P04 ×2 y P05 ×2 exprés, base nueva | 102.00, stocks P04/P05 2/28, pago único | ConfirmacionTests.P02_P03_P04 | COMPLETO |
| P04 | Carmen, P03 ×10 estándar, base nueva | 90.00, descuento 15.00, P03=10, pago único | ConfirmacionTests.P02_P03_P04 | COMPLETO |
| P05 | Teletrabajo ×6, stock oficial | P02 insuficiente; sin pago, movimientos ni Outbox | ConfirmacionTests.P05 | COMPLETO |
| P06 | Pedido válido y alterno configurado para rechazar | VALIDADO y saldos intactos; sin pago ni evento aprobado | ConfirmacionTests.P06_P19 | COMPLETO |
| P07 | Reintentar misma clave en el mismo pedido | Mismo pago, una llamada y tres movimientos físicos | ConfirmacionTests.P07_P08 | COMPLETO |
| P08 | Cambiar precio del catálogo y editar/cancelar pedido pagado | Denegación de ambas operaciones y mismo precio, pago e historial | EscritorioTests.P08; capturas fase 4 13–15 | COMPLETO |
| P09 | BODEGA: intentar antes del pago; pagar, despachar y entregar | Rechazo previo; estado, guía y entrega correctos; VENTAS no despacha | LogisticaTests.P09; cierre fase 4, captura 17 | COMPLETO |
| P10 | Cerrar y reabrir sobre misma SQLite | Pedido, pago, stock y traza preservados; comprobación posterior al reembolso | Fase 4 04–06 y SQLite 03/08; usuario-devolucion-reabierta.png | COMPLETO |
| P11 | Procesar evento por EMAIL y SMS en bases separadas | Registro del canal, tipo y contenido característico | OutboxComercialTests.P11_P30 | COMPLETO |
| P12 | Resolver estándar y exprés | 5/12, 3/1 y guías de su propia familia | OperacionesComercialesTests.P12 | COMPLETO |
| P13 | Crear pedido sin líneas, con cliente inexistente y de varios renglones | Dos rechazos y agregado válido (subtotal 67.00) | OperacionesComercialesTests.ABMCatalogoClientesYBorradoresPreservaInstantaneas | COMPLETO |
| P14 | Clonar borrador con kit anidado dos veces, editar una copia | Identidades nuevas y snapshot aislado de plantilla y otra copia | PatronesBorradorTests.P14; fase 4, captura 16 | COMPLETO |
| P15 | Dos ámbitos y dos vistas; duplicar 50 eventos en paralelo | Identidad única; total 50; mismo contador visible en Panel y Auditoría | MetricasSesionTests.P15; OutboxComercialTests.P37; fase 4, capturas 06–07 | COMPLETO |
| P16 | Resolver legado y alterno con el mismo contrato | Ambos aprueban 5.21, repiten resultado y rechazan importes inválidos | ConfirmacionTests.P16 | COMPLETO |
| P17 | Emitir venta y devolución por VistaPrevia y ArchivoTXT | Cuatro combinaciones; contenido de venta 59.00 y devolución 54.00 | ComprobantesTests.BridgeEntregaVentaYDevolucionACadaUnoDeLosDosMedios; TXT y registro visual de fase 3 | COMPLETO |
| P18 | Kits oficiales y ciclos directo e indirecto | 50/60, hojas P01/P02/P03, rechazo de ciclos | InicializacionTests.P01; CatalogoTests; RestriccionesSqliteTests | COMPLETO |
| P19 | Cobro autorizado aprobado, rechazado y argumento inválido | Orden INICIO → PASARELA → FIN o ERROR; correlación y resultados | ConfirmacionTests.P06_P19 y P16 | COMPLETO |
| P20 | Forzar FK inválida en Outbox antes del commit | Pedido VALIDADO; pago, stock, movimientos y evento sin efectos parciales | ConfirmacionTests.P20 | COMPLETO |
| P21 | 1.000 filas con cinco SKU, mismo orden y datos extrínsecos | ReferenceEquals, 5/1.000 objetos y 56.752/104.024 bytes asignados en medición original | FlyweightTests.P21; fase 4, captura 10; protocolo fase 3 | COMPLETO |
| P22 | CONSULTA y BODEGA intentan cobrar | Denegación anterior a log de pago y pasarela; solo auditoría de seguridad | ConfirmacionTests.P22 | COMPLETO |
| P23 | Cliente inactivo y kit sin stock; reactivar cliente | CLIENTE seguido de STOCK; ni proveedor ni persistencia parcial | OperacionesComercialesTests.P23, validación y fachada | COMPLETO |
| P24 | Ana VIP, teletrabajo ×1 estándar por botón e invocador de prueba | ConfirmarPedidoCommand / LEGADO; 60−6+5=59 y auditoría de éxito | OutboxComercialTests.P24; recorrido P24 en evidencia de fase 3 y TXT de venta | COMPLETO |
| P25 | ESTADO = PAGADO AND TOTAL >= 50 | Solo pedidos coincidentes, con pago persistido | FiltroPedidosTests.P25; recorrido de fase 4 | COMPLETO |
| P26 | Sintaxis errónea, paréntesis y AND/OR | Error con columna y precedencia; árbol tipado; consulta segura | FiltroPedidosTests.P26; cierre fase 4, captura 20 | COMPLETO |
| P27 | Iterar teletrabajo y caso con ramas compartidas | Tres SKU en orden determinista y multiplicadores correctos | RecorridosCatalogoTests.P27_P34 e IteratorEsDeterminista | COMPLETO |
| P28 | Confirmar y revisar catálogo, pedidos y métricas | Stock 7/4/19, PAGADO y ventas brutas recalculadas; métricas de suscriptores tras reproceso | MediadorComercialTests.ConfirmacionRefrescaCincoColaboradoresSobreElEstadoConfirmado; recorrido de fase 3 | COMPLETO |
| P29 | Tres ediciones, dos deshacer y un rehacer, luego pagar | Snapshots restaurados, límite cinco y pago no reversible | PatronesBorradorTests.P29; fase 4, captura 17 | COMPLETO |
| P30 | Tres suscriptores para compra confirmada; reintentar y provocar rollback | Aviso, métrica y auditoría por suscriptor; sin duplicados ni evento exitoso de rollback | OutboxComercialTests.P11_P30 y P37; ConfirmacionTests.P20 | COMPLETO |
| P31 | Ciclo completo y cancelar pagado | Transiciones permitidas y rechazo de repetición, retroceso y cancelación pagada | PedidoTests.StateRegistraSoloTransicionesAutorizadasPorElEstado | COMPLETO |
| P32 | Promociones normal, VIP, mayorista y extensión registrada | 6/15/0, un descuento, prioridad y redondeo decimal | OperacionesComercialesTests.P32 y PromocionYUmbralFisico | COMPLETO |
| P33 | DEFECTO día 20 y ARREPENTIMIENTO día 10 en pedidos entregados | Aceptación y rechazo según plazo; rechazo sin reintegro ni reembolso | LogisticaTests.P33 | COMPLETO |
| P34 | Visitar kit teletrabajo | P01/P02/P03 y 1.00 kg; multiplicidad ×3 también cubierta | RecorridosCatalogoTests.P27_P34; fase 4, captura 09 | COMPLETO |
| P35 | Devolver por defecto y repetir tras reabrir | 54.00 sin flete, una devolución, tres reintegros, rechazo de duplicado y auditoría | LogisticaTests.P35; capturas usuario-inventario, usuario-devolucion y usuario-auditoria | COMPLETO |
| P36 | Dos confirmaciones en conexiones distintas, stock inicial ajustado a 1 | Un pago, un evento, una llamada a pasarela y stock final 0 | ConfirmacionTests.P36 | COMPLETO |
| P37 | Fallo de aviso, reiniciar host sobre la misma base, reintentar | Intentos 1→2, pendiente→procesado, sin pago ni aviso duplicado | OutboxComercialTests.P37; fase 4, capturas 02/06 | COMPLETO |
| P38 | Compilar desde artefactos nuevos y sustituir puertos y proveedor | 119 pruebas, 0 errores/advertencias; referencias, ISP, DIP y LSP | ContratosEscritorioTests; ReferenciasTests; ConfirmacionTests.P16; TRX final | COMPLETO |

## Patrones GoF

Las rutas comienzan tras `src/LogiMarket.` y el proyecto indicado. La tabla registra la colaboración observada en código y la comprobación del apartado anterior.

| Patrón | Problema | Participantes y métodos | Flujo real | Evidencia | Beneficio y costo | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| Factory Method | Producir avisos por canal | Application/Eventos/AvisosSimulados.cs `CreadorEmail.Crear`, `CreadorSms.Crear` y `ObservadorAvisos.RecibirAsync` | Outbox selecciona emisor mediante creadores DI | P11 | Canal intercambiable; dos creadores | COMPLETO |
| Abstract Factory | Mantener coherencia de tarifa, plazo y guía | Application/Entregas/FamiliasEntrega.cs `FabricaEntrega.Crear`, variantes, `FamiliasEntrega.Obtener` | Valorar y despachar reciben componentes de la misma familia | P12 | Evita mezclar variantes; tres productos por familia | COMPLETO |
| Builder | Componer pedidos completos | Application/Pedidos/PedidoBuilder.cs `Construir`, `AdministrarPedidos.CrearAsync` y `Pedido` | Construye líneas y snapshots para altas válidas | P13 | Composición explícita; coordina invariantes | COMPLETO |
| Prototype | Duplicar plantilla de borrador | Domain/Pedidos/Pedido.Borrador.cs `ClonarComoBorrador`; Application/Pedidos/AdministrarPedidos.cs `ClonarBorradorAsync` | Pedido y líneas se copian con identidad nueva | P14 | Reutilización; copia proporcional al grafo | COMPLETO |
| Singleton | Compartir métricas de sesión | Application/Eventos/MetricasSesion.cs `Registrar`, `Contar`; Infrastructure/Persistencia/RegistroPersistencia.cs | DI comparte una instancia entre Outbox y consultas de ambas vistas | P15 | Coherencia por proceso; memoria por evento | COMPLETO |
| Adapter | Normalizar pasarela en centavos | Infrastructure/Pagos/PasarelasSimuladas.cs `PasarelaLegadaAdapter.CobrarAsync`, `ResolutorPasarelas.Obtener` | Adapta llamadas de confirmación y admite alterno | P16 | Sustitución; valida precisión al convertir | COMPLETO |
| Bridge | Separar formato interno del medio | Application/Documentos/Comprobante.cs `EmitirAsync`; `EmitirComprobantes.EjecutarAsync`; Infrastructure/Documentos/ArchivoTXT.cs | Dos tipos de comprobante × vista y archivo | P17 | Cambios independientes; contrato de documento común | COMPLETO |
| Composite | Uniformar hojas y kits | Domain/Catalogo/IElementoCatalogo.cs, `ProductoSimple`, `KitCompuesto.CalcularPrecio`, `AgregarComponente` | Seed compone kits y fachada descuenta hojas | P18 | Composición anidada; necesita controlar ciclos | COMPLETO |
| Decorator | Auditar cobro sin reemplazar proveedor | Application/Pagos/AuditoriaPagoDecorator.cs `CobrarAsync`, `ITrazasPago` | Envuelve al proveedor y correlaciona etapas | P19 | Auditoría transversal; persistencia posterior al commit | COMPLETO |
| Facade | Unificar confirmación transaccional | Application/Pagos/ConfirmacionPedidoFacade.cs `ConfirmarAsync` y `ConfirmarEnTransaccionAsync` | Valida y valora, registra pago, stock y Outbox | P02–P07, P20 | Punto de entrada estable; varios colaboradores | COMPLETO |
| Flyweight | Reutilizar metadatos en catálogo reiterado | Application/Catalogo/CatalogoVisualFlyweightFactory.cs `CrearRenglon`, `ConsultarCatalogoVisual.DemostrarAsync` | Comparte metadatos e independiza precio, cantidad y saldo | P21 | Menos asignaciones con repetición; diccionario por proyección | COMPLETO |
| Proxy | Proteger acceso al proveedor | Application/Pagos/PagoAutorizadoProxy.cs `CobrarAsync`; Infrastructure/Pagos/PasarelasSimuladas.cs `Obtener` | Exige autorización antes del Decorator y Adapter | P22 | Bloquea efectos; requiere orden correcto de DI | COMPLETO |
| Chain of Responsibility | Reportar primer error aplicable | Application/Pedidos/CadenaValidacionPedido.cs `RevisarAsync`, `Paso.EjecutarAsync` | Cliente → líneas → stock → estado; permiso en fachada | P23 | Reglas aisladas; orden semántico | COMPLETO |
| Command | Reusar acciones desde UI y automatización | Application/Comandos/ComandosPedido.cs, `OperacionesPedido.ConfirmarAsync`, `InvocadorComandos.EjecutarAsync` | Confirmar, cancelar, despachar y devolver quedan trazables | P24 | Separa invocador de receptor; auditoría posterior | COMPLETO |
| Interpreter | Interpretar filtros sin construir SQL | Application/Reportes/ParserFiltroPedido.cs `Parsear`, `FiltroPedidos.Cumple`, `ConsultarReportePedidos.EjecutarAsync` | Parser produce árbol y reporte lo aplica | P25–P26 | Precedencia verificable; lectura materializada | COMPLETO |
| Iterator | Recorrer hojas físicas del kit | Domain/Catalogo/IteradorComponentesFisicos.cs `Recorrer`; `AdministrarCatalogo.ConsultarExistenciasAsync` | Enumera hojas con multiplicidad en consulta real | P27 | Oculta estado de recorrido; pila e instantánea | COMPLETO |
| Mediator | Coordinar vistas tras cambios de pedido | Application/Navegacion/FormularioPrincipalMediator.cs `NotificarAsync`/`Destinos`; WinForms/VentanaComercial.cs `NotificarAsync` | Panel, auditoría, inventario, carrito y pedidos se refrescan mediante suscripciones | P28 y regresión de despacho | Sin llamadas entre vistas; mantener destinos al añadir eventos | COMPLETO |
| Memento | Restaurar cambios de un borrador | Domain/Pedidos/Pedido.Borrador.cs `CapturarBorrador`/`RestaurarBorrador`, Application/Pedidos/HistorialBorrador.cs | Historial acotado que deshace y rehace con control de versión | P29 | Reversión aislada; memoria por pedido activo | COMPLETO |
| Observer | Distribuir eventos de negocio confirmados | Application/Eventos/PublicadorEventos.cs `PublicarAsync`; Infrastructure/Persistencia/Eventos/EntregaDurableSqlite.cs `EntregarAsync` | Outbox despacha a auditoría, avisos y métricas | P30/P37 | Suscriptores ampliables; marcas idempotentes | COMPLETO |
| State | Restringir acciones por estado | Domain/Pedidos/EstadoDelPedido.cs `Para`, Domain/Pedidos/Pedido.cs | Agregado delega acciones en comportamiento del estado | P31 | Centraliza transiciones; mapa persistencia/comportamiento | COMPLETO |
| Strategy | Elegir descuento por prioridad | Application/Precios/CalculadoraPedido.cs `Calcular`, `IPoliticaDescuento` | Valorar elige mayorista, VIP o normal sin acumular | P32 | Extensión por DI; mantener prioridades inequívocas | COMPLETO |
| Template Method | Unificar devolución y variar reglas | Application/Devoluciones/ProcesarDevolucion.cs `EjecutarAsync` y variantes | Misma secuencia para elegibilidad, reembolso, stock y evento | P33/P35 | Mantiene orden; limitar pasos variables | COMPLETO |
| Visitor | Obtener detalle y peso por tipo de nodo | Domain/Catalogo/VisitantesCatalogo.cs, `ProductoSimple.Aceptar`, `KitCompuesto.Aceptar`; `ConsultarLogisticaAsync` | El catálogo aplica visitantes de desglose y peso | P34 | Reportes extensibles; nuevo nodo afecta el contrato | COMPLETO |

## Requisitos transversales de entrega

| Requisito | Estado | Evidencia o condición restante |
| --- | --- | --- |
| RF01–RF12, RF18 | COMPLETO | Acceso por rol, catálogo, clientes, pedido, cobro, despacho, devolución, movimientos y persistencia. P01–P10 y P33–P37. |
| RF13–RF17 | COMPLETO | Reportes, comprobantes, canal simulado, actualización de panel y recuperación Outbox. P11, P15, P17, P21, P25–P28 y P37. |
| RNF01–RNF02 | COMPLETO | Integración SQLite y referencias verificadas; ver auditoría de arquitectura. |
| RNF03 | PARCIAL | Las doce vistas cuentan con evidencia de fase. Tras el arreglo, el 100 % está comprobado. La presentación corregida al 125 % y 150 % queda NO VERIFICADA. |
| RNF04–RNF09 | COMPLETO | Flyweight, hash/permisos, trazabilidad, suite automatizada, recuperación y stack objetivo. |
| RNF10 | PARCIAL | Riesgos y correcciones documentados. Algunas correcciones históricas tienen evidencia visual anterior sin TRX diferencial conservado. Los riesgos residuales tienen propuestas separadas. |
