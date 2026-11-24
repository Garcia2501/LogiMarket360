# Evidencias de auditoría

Fecha: 2026-11-23. Los archivos de ejecución y capturas se conservan en `outputs/evidencias-auditoria` del espacio de trabajo. El repositorio versiona el índice y las pruebas fuente. Las capturas aportadas de escalado alto muestran la versión anterior al arreglo.

## Ejecuciones nuevas

| Evidencia | Resultado |
| --- | --- |
| `auditoria-base.trx` | 118 aprobadas, 0 fallidas, 0 omitidas en la base `a4e428b`. |
| `mediator-antes.trx`, `mediator-despues.trx` | BODEGA despachó con la SQLite temporal y se observó la falta de actualización. Después de la corrección pasaron 3 pruebas del mediador. |
| `expansion-antes.trx`, `expansion-despues.trx` | 19 aprobadas en cada ejecución focalizada de confirmación y valoración. |
| `auditoria-final.trx` | 119 aprobadas, 0 fallidas, 0 omitidas; restore y Release con 0 errores/advertencias desde directorio de artefactos nuevo. |
| `sqlite-arranque-limpio.json` | Dos invocaciones independientes de `--solo-inicializar` con archivo temporal. Migración inicial, catálogo, roles, `integrity_check` y `foreign_key_check`. |
| `audit-100-panel.jpg`, `audit-100-auditoria.jpg`, `audit-100-maximizada.jpg`, `audit-100-foco.jpg` | WinForms de producción desde lanzador de prueba, SQLite de demostración y sesión ADMIN obtenida con el servicio real. 96 DPI, rótulos completos, cuatro métricas visibles, foco en Categoría y ventana normal y maximizada. |
| `usuario-inventario-devuelto.png`, `usuario-devolucion-reabierta.png`, `usuario-auditoria-devolucion.png` | Evidencia aportada por el usuario en la demostración. Reintegro único P01/P02/P03, devolución 54.00 después del reinicio, botón deshabilitado y comando aprobado. |
| `usuario-dpi-anterior-carrito.png`, `usuario-dpi-anterior-auditoria.png` | Foco y recortes con escalado alto antes del commit DPI. El usuario confirmó TAB en 125 % y 150 % de la versión anterior. |

## Trazabilidad de las 12 vistas de la guía

Las referencias de fase 4 corresponden a `outputs/evidencias-fase-4` y las de cierre anterior a `outputs/evidencias-cierre-fase-4`. El informe de [interfaz](interfaz-winforms.md) documenta las comprobaciones históricas. La auditoría contrastó esas fuentes y abrió Panel y Auditoría en la compilación actual.

| Pantalla | Comprobación realizada o evidencia previa revisada |
| --- | --- |
| Acceso | El usuario informó entrada correcta con cuatro roles. Cierre 03 y `SeguridadComercialTests` corroboran acceso centrado y sesiones autorizadas. |
| Panel general | Ejecución actual `audit-100-panel.jpg`; ventas brutas 59.00 y pedido persistido. |
| Catálogo y kits | Fase 4 09/10/13 y cierre 14. Desglose anidado, precio y restricción de edición de VENTAS. |
| Clientes | Cierre 13/15. Entrada real, fila VIP, permisos y foco en búsqueda. |
| Carrito y borradores | Fase 4 15–17 y cierre 09. Instantánea, edición bloqueada en pagado, clonar, deshacer y rehacer. |
| Pedidos e historial | Fase 4 14 y P08. Historial, saldo pagado y cancelación inhabilitada. |
| Confirmación y cobro | Recorrido de [fase 3](evidencia-fase-3.md), cierre 21 y P22/P24. Cobro real simulado en recorrido anterior y control de BODEGA. |
| Despacho y entrega | Fase 4 18 y cierre 17. Guía, estado y permisos. Regresión nueva de actualización después del despacho. |
| Devoluciones | Captura del usuario tras reinicio, cálculo 54.00, unicidad y bloqueo visual. |
| Inventario y movimientos | Captura del usuario posterior a devolución, saldos 8/5/20 y tres movimientos +1. |
| Reportes y comprobantes | Fase 4 11/12, cierre 19/20 y ambos TXT de fase 3. Bridge, error del Interpreter y consulta para CONSULTA. |
| Auditoría y demostración | Capturas de auditoría actual y dato aportado del comando aprobado. Controles de demostración y recuperación P37 en fase 4 02/06. |

## Pendiente para liberar la rama

Probar la compilación de `audit/calidad-integral` en Windows con 120 y 144 DPI reales. Reiniciar el proceso tras ajustar cada escala. Abrir Auditoría con ADMIN en `--demo` y comprobar menú, cuatro cifras, desplazamiento y foco TAB, primero con tamaño normal y luego maximizado. Comparar con los defectos reproducidos en las capturas originales. Este procedimiento no requiere operaciones adicionales sobre el pedido de Ana.
