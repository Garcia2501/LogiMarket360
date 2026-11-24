# Revalidación visual al 125 %

Fecha: 2026-11-23. Rama audit/calidad-integral, commit 12458bc. Ventana comercial WinForms con ADMIN y SQLite de demostración aislada. El proceso registró DeviceDpi=120. Se comprobó apertura y distribución en ventana normal y maximizada, sin ejecutar escrituras comerciales.

| Vista | Normal | Maximizada |
| --- | --- | --- |
| Acceso | normal-01-acceso.jpg | max-01-acceso.jpg |
| Panel | normal-02-panel.jpg | max-02-panel.jpg |
| Catálogo y kits | normal-03-catalogo.jpg | max-03-catalogo.jpg |
| Clientes | normal-04-clientes.jpg | max-04-clientes.jpg |
| Carrito y borradores | normal-05-carrito.jpg | max-05-carrito.jpg |
| Pedidos e historial | normal-06-pedidos.jpg | max-06-pedidos.jpg |
| Confirmación y cobro | normal-07-cobro.jpg | max-07-cobro.jpg |
| Despacho y entrega | normal-08-despacho.jpg | max-08-despacho.jpg |
| Devoluciones | normal-09-devoluciones.jpg | max-09-devoluciones.jpg |
| Inventario | normal-10-inventario.jpg | max-10-inventario.jpg |
| Reportes y comprobantes | normal-11-reportes.jpg | max-11-reportes.jpg |
| Auditoría | normal-12-auditoria.jpg | max-12-auditoria.jpg |

- Los rótulos Reportes y comprobantes y Auditoría y demostración son legibles y completos. Las vistas largas se alcanzan mediante desplazamiento. Algunas celdas largas de tablas muestran puntos suspensivos en ventana normal.
- En la versión publicada se comprobó TAB de Buscar cliente al botón Buscar, con borde de foco visible (125-tab-clientes.jpg). También se observó el foco en Categoría durante el ensayo de renderizado. El recorrido de teclado fue representativo.
- Las cuatro cifras de Auditoría aparecen en max-12-auditoria.jpg y al entrar desde Reportes (normal-12-auditoria-reentrada.jpg). En la captura al entrar directamente desde Panel general faltan dos ceros (normal-12-auditoria.jpg). El panel también mostró esa diferencia al volver desde Auditoría.
- Se ensayaron repintado explícito, renderizado de etiqueta y mantener vistas montadas. Ninguno cambió el resultado de captura. Se retiraron los tres ensayos. Falta contrastar con la pantalla física del usuario; no se confirma un defecto de la aplicación ni se aprueba todavía ese salto. No hubo commits ni integración.