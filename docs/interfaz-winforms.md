# Interfaz Windows Forms y verificación de fase 4

Se revisaron los 12 wireframes originales de las pp. 11–16. Al inicio existían el acceso y la operación central desde un panel compartido. La navegación, las pantallas dedicadas, parte de la edición, las búsquedas, las consultas de inventario y las herramientas de demostración se completaron en esta rama.

| Vista | Operación disponible | Comprobación de esta fase |
| --- | --- | --- |
| Acceso | Usuario, contraseña enmascarada, mensajes junto al campo, apertura y cierre de sesión. | El cierre volvió al acceso con foco en Usuario. Inicio y permisos probados en Application. |
| Panel general | Ventas, pedidos, stock bajo, eventos pendientes y pedidos recientes. | El pedido conservó el estado y el importe al reabrir. La métrica coincidió con Auditoría tras reprocesar. |
| Catálogo y kits | Búsqueda, alta, edición, desactivación, composición anidada y medición Flyweight. | Se guardó el precio de P01 y se observó KIT_TELETRABAJO con P01/P02/P03 y 1.00 kg. La medición usó 1.000 filas. |
| Clientes | Búsqueda por identificación o nombre, alta y edición de ficha. | La tabla recuperó los tres clientes de prueba. La gestión tiene pruebas de integración. |
| Carrito y borradores | Crear, editar, clonar, deshacer, rehacer, validar y consultar instantáneas. | El borrador clonado recibió un GUID nuevo. En la copia, la cantidad pasó de 1 a 2 y se deshizo y rehízo. El precio histórico de Ana seguía en USD 60.00. |
| Pedidos e historial | Búsqueda y filtro por estado, historial, detalle de pago y cancelación por estado. | El histórico mantuvo BORRADOR, VALIDADO y PAGADO. El botón de cancelación se inhabilitó para PAGADO. |
| Confirmación y cobro | Selector de proveedor, desglose, diálogo previo y confirmación simulada. | Se pagó el pedido de Ana. Tras corregir el formato y al reabrir se mostró USD 59.00. |
| Despacho y entrega | Guía, seguimiento, registro de despacho y entrega autorizados. | Se emitió guía ESTÁNDAR, plazo de tres días, y se registró ENTREGADO. |
| Devoluciones | Elegir motivo, validar política, ver cálculo y confirmar devolución simulada. | La validación de DEFECTO mostró USD 54.00 sin flete. La confirmación visual quedó pendiente. La suite cubre reembolso, stock y duplicados. |
| Inventario y movimientos | Existencias, estados de stock, filtro por SKU, movimientos y ajuste autorizado. | Tras el reinicio se observaron 7, 4 y 19 para P01/P02/P03 y los tres movimientos de venta. |
| Reportes y comprobantes | Reporte por período, filtro Interpreter, comprobante de venta o devolución, vista previa y TXT. | `ESTADO = PAGADO AND TOTAL >= 50` devolvió solo a Ana. Su vista previa y el TXT exportado mostraron USD 59.00. Las cuatro combinaciones de Bridge conservan la evidencia de fase 3. |
| Auditoría, métricas y demostración | Filtro de auditoría, indicadores de sesión, diagnósticos Outbox, simulación de fallos y reintentos. | Se observó el fallo del aviso y su recuperación en el segundo intento tras reiniciar. Hubo una sola invocación de pago para Ana. |

Se usa WinForms nativo con Segoe UI, tema claro, botones con rótulo, cuadrículas con selección de fila y precios a dos decimales en USD, contenedores adaptables, foco nativo, estados junto a cada operación y confirmación previa de acciones sensibles. Las vistas recibieron datos de SQLite desde servicios de Application. La navegación y los comandos comparten el pedido actual mediante su identificador. Los cambios comerciales notifican a Mediator y los permisos se verifican nuevamente en Application.

## Cómo repetir las comprobaciones

1. Ejecuta la inicialización en el modo `--demo --solo-inicializar` y abre WinForms con `--demo`. El procedimiento para configurar las cuatro cuentas está en el [README](../README.md).
2. Crea el pedido de Ana como VIP, KIT_TELETRABAJO ×1 con entrega estándar. Valida el borrador. Con ADMIN, activa **Fallar avisos de Outbox**, aplica el escenario y cobra el pedido.
3. Reprocesa pendientes y comprueba una falla con `IOException`. Cierra la aplicación completa. Reabre con `--demo` sobre el mismo archivo y comprueba pedido, pago de USD 59.00, existencias 7/4/19 y auditoría de cobro.
4. Reprocesa y comprueba una recuperación, dos intentos acumulados y ningún error pendiente. Verifica que la referencia del pago sea la misma y que exista una sola invocación a pasarela. Observa **Ventas de sesión** en **Panel general** y en **Auditoría y demostración**.
5. Edita el precio de P01 y vuelve a consultar el pedido pagado. Su instantánea y el historial se conservan. La cancelación y el reemplazo de línea quedan deshabilitados. Application rechaza ambas operaciones en la prueba P08.
6. En Reportes, aplica el filtro anterior, abre **Vista previa** y luego **Guardar TXT**. En Catálogo busca KIT_TELETRABAJO y compara el desglose físico con existencias; ejecuta también **Medir 1.000 filas**.

## Evidencias y alcance

WinForms se ejecutó en Windows con una base SQLite aislada, usando un host de validación que prepara una sesión de ADMIN a través de Application y abre la ventana comercial de producción. El reinicio cerró y volvió a abrir el proceso sobre el mismo archivo. Las capturas, el TXT, las consultas SQLite de solo lectura y el TRX se conservaron fuera de Git.

- P08. Capturas 13–15. El precio de P01 subió de USD 20.00 a USD 21.00; original pagado, instantánea e historial conservaron sus valores.
- P10 y P37. Capturas 02 y 04–06, y registros SQLite 03 y 08. El evento pasó de un intento fallido a dos intentos y estado procesado. Sin duplicados de pago ni de efectos por suscriptor.
- P15. Capturas 06–07 y `MetricasSesionTests`. Ambas vistas mostraron el valor 1 en la misma sesión. El observador deduplicó 50 eventos publicados dos veces en paralelo.
- P38. `phase4-final.trx`, `ContratosEscritorioTests`, `ReferenciasTests` y `ConfirmacionTests.P16_AmbosProveedoresCompartenContrato`. Release sin advertencias ni errores. 114 pruebas aprobadas, ninguna fallida u omitida. EF Core aplicó `20261120214119_EsquemaInicial` en un archivo nuevo y confirmó que el modelo coincide con la migración.
- Capturas adicionales 09–12 y 16–21. Composición, medición Flyweight, comprobante, edición de borrador, entrega, devolución calculada, maximizado y cierre de sesión. La medición Flyweight comprobó 5 frente a 1.000 objetos y 56.752 frente a 104.024 bytes asignados en esa ejecución.

Para la revisión visual final quedan la entrada manual de credenciales, la navegación TAB completa, el comportamiento con otros escalados DPI y el recorrido con VENTAS, BODEGA y CONSULTA. Se observó el foco inicial y se verificaron el tamaño normal y maximizado. La revisión automática de la herramienta bloqueó la acción de confirmar el reembolso simulado sin autorización específica, por lo que el recorrido visual llegó hasta la validación del importe. Los casos de integración conservan su evidencia de devolución.

## Validación de cierre · 2026-11-22

Se abrió el ejecutable con `--demo` y se verificó el formulario de acceso con una SQLite separada. Un entorno temporal abrió la misma ventana de producción con sesiones autenticadas por `AutenticacionLocal` para contrastar los controles de cada rol. El inicio con el teclado en el formulario de producción queda como verificación manual.

| Vista | Evidencia acumulada y comprobación de cierre |
| --- | --- |
| Acceso | Se reprodujo el formulario desplazado a la derecha y se comprobó que queda centrado a 900 × 630. Se observó foco en Usuario y regreso al acceso al cerrar sesiones de prueba. Entrada manual por rol pendiente. |
| Panel general | Apertura a 900 × 630 y maximizada. Ana, ENTREGADO y USD 59.00 se leen en la tabla. Las capturas 06–07 previas acreditan Singleton tras procesar el evento. |
| Catálogo y kits | Con VENTAS se observaron cinco productos, precio de P01 USD 20.00, edición inhabilitada y consulta de composición habilitada. Se conserva el árbol anidado, peso y medición Flyweight previos. |
| Clientes | Ana VIP cargada. Nuevo y Editar habilitados para VENTAS. Se observó el salto de Tab desde Buscar cliente al botón Buscar. |
| Carrito y borradores | Los controles inferiores seguían alcanzables por desplazamiento a tamaño mínimo. La evidencia previa conserva Prototype y Memento con cantidades 1→2→1→2. |
| Pedidos e historial | P08 se reejecutó contra SQLite. La evidencia visual anterior conserva el importe, la instantánea y el estado original tras editar el catálogo. |
| Confirmación y cobro | BODEGA vio el subtotal 60.00, descuento 6.00, envío 5.00 y total 59.00 USD, con confirmación inhabilitada. Las denegaciones de Application y la exclusión de efectos se reejecutaron en la suite. |
| Despacho y entrega | Con BODEGA se consultaron la guía ESTÁNDAR, el plazo de tres días y la entrega real. El escenario preparó el pedido con una sesión real de VENTAS y registró despacho y entrega con BODEGA por los servicios de Application. |
| Devoluciones | Se validó visualmente DEFECTO y el importe de USD 54.00 sin flete sobre Ana entregada. La suite confirmó reintegro, auditoría y unicidad después de reabrir SQLite. Pendiente confirmar la devolución desde la ventana y releerla después de un reinicio. La consulta SQLite 22 conserva el pedido entregado y ninguna devolución. |
| Inventario y movimientos | CONSULTA vio 7/4/19 y los tres movimientos de la venta, con Registrar ajuste inhabilitado. |
| Reportes y comprobantes | CONSULTA vio el pedido y tuvo acceso a vista previa y TXT. El filtro incompleto `ESTADO =` mostró el error en la columna 9 y el indicador junto al campo. Se conserva la vista previa y exportación verificadas anteriormente. |
| Auditoría, métricas y demostración | Acceso reservado a ADMIN. Se comprobó que la barra vertical permite alcanzar Aplicar escenario y Reprocesar pendientes. La recuperación y la deduplicación de Outbox se reejecutaron en la suite. |

Los rechazos se comprobaron directamente con `AutorizacionOperaciones.ExigirAsync` usando las cuatro cuentas en SQLite y verificando la auditoría de cada denegación. Las pruebas de servicios también observaron el rechazo de cobro de BODEGA y CONSULTA y el rechazo de devolución de BODEGA. El flujo de P35 conserva pago de USD 59.00, tarifa de USD 5.00, devolución de USD 54.00, tres movimientos de reintegro, stock 8/5/20, una devolución y un evento de devolución al reabrir el archivo temporal.

### Incidencias y evidencia local

- Acceso desplazado. Abrir el programa sin sesión y comparar el centro de la tarjeta con el área completa. Se liberó el ancho de la navegación mientras el usuario está fuera. Capturas 01 y 03.
- Controles superpuestos y total recortado. Reducir a 900 × 630 y abrir Panel, Carrito y Auditoría. La distribución ahora conserva el alto necesario con desplazamiento vertical y la tabla protege el ancho de las columnas de estado, fecha e importe. Capturas 04–09 y 11–12.
- Texto de acción principal deshabilitada con poco contraste. Con BODEGA, abrir Confirmación y cobro. El fondo de la acción inactiva se aclaró y mantuvo la acción inhabilitada. Capturas 16 y 21.
- Teclado y roles. Capturas 13–20. Se verificó el foco visible y una secuencia de Tab en Clientes. El entorno tuvo 96 DPI y se comprobaron el tamaño mínimo y el maximizado.
- El TRX final `cierre-fase-4-final.trx` registra 118 aprobadas, 0 fallidas y 0 omitidas. `cierre-fase-4-casos.trx` registra 22 pruebas focalizadas aprobadas, incluidas autorización, puertos, referencias y logística. Las ejecuciones visuales anteriores conservan el reinicio y Outbox de P10 y P37.

### Pasos manuales pendientes de cierre

1. Abrir el ejecutable con `--demo`. Introducir una clave incorrecta y comprobar el mensaje. Iniciar y cerrar sesión desde el formulario con ADMIN, VENTAS, BODEGA y CONSULTA. En cada cuenta recorrer los controles habilitados e inhabilitados indicados en la tabla.
2. Con ADMIN, elegir **Ana QA Cierre** en **Devoluciones**, motivo **DEFECTO**. Comprobar USD 54.00 y confirmar la devolución simulada. En Inventario verificar 8/5/20 con tres movimientos de reintegro. En Auditoría comprobar el comando y en Reportes el comprobante. Volver a Devoluciones y comprobar que ya está registrada y no puede repetirse. Cerrar el proceso, abrir el mismo modo y contrastar referencia, auditoría, devolución y existencias.
3. Con el escalado de Windows a 125 % y 150 %, volver a abrir y recorrer con Tab y Mayús+Tab el acceso, la barra y los controles de cada vista. Alternar tamaño mínimo y maximizado y revisar los encabezados, la legibilidad y la capacidad de llegar a las acciones mediante desplazamiento.

La herramienta de automatización de Windows no introduce credenciales en el formulario. La confirmación visual del reembolso simulado requiere una acción del usuario o autorización específica en el momento del clic. El estado de integración permanece pendiente de las verificaciones visuales bloqueantes.
