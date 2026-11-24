# Cierre visual del informe

Las recapturas se hicieron en Windows con el código auditado, roles ficticios, una copia SQLite aislada y 96 DPI. Para Catálogo se repuso el precio de la etapa inicial en la copia con `AdministrarCatalogo.ActualizarAsync`. Las existencias, el pago histórico y la base de origen se preservaron. Se estacionó el cursor fuera de la ventana. Los nuevos JPEG son tomas nativas sin retoque. El respaldo de 82 páginas conserva íntegramente el corte anterior.

| Original | Página anterior | Reemplazo | Datos cotejados | SHA-256 de la recaptura |
| --- | --- | --- | --- | --- |
| `catalogo.jpg` | 40 | `catalogo-cierre.jpg` | ADMIN, Catálogo y kits. P01 a USD 20.00, stocks 7/4/19 y árbol KIT_TELETRABAJO con 1.00 kg. | `ee7de779c9e79c660b4344b89130c5a5737b9ed19d5c88d51eff5e199507394d` |
| `reportes.jpg` | 48 | `reportes-cierre.jpg` | Ventana nativa del comprobante. Coinciden ID de pedido, referencia de pago, kit, subtotal 60.00, descuento 6.00, flete 5.00 y total 59.00. La nueva toma encuadra el diálogo, cuya selección de texto se desactivó con el teclado. | `5cd77b57ad9bf9e691d586b55068dd2667299c483d275c9e2dea759585e9c2e3` |
| `flyweight.jpg` | 70 | `flyweight-cierre.jpg` | 1.000 filas, 5/1.000 identidades y 56.752/104.024 bytes asignados. Coinciden con la evidencia histórica. | `27151879ca91ba078ad0722059c4c94794ac753baa6bb560ae1c214d79411e73` |
| `interpreter-error.jpg` | 73 | `interpreter-error-cierre.jpg` | CONSULTA; rango histórico íntegro; mensaje en columna 9, mismo pedido ENTREGADO, USD 59.00 y cero devoluciones. | `1fa69c25fdc513fbead750355e0995e73e597f16bdf5c693c3c28298cac57503` |
| `carrito.jpg` | 42 | `carrito-cierre.jpg` | Mismo borrador Borrador QA, identidad histórica, P04 ×2 y total USD 92.00. Se cambió la cantidad y se ejecutó Deshacer y Rehacer en la misma copia aislada. La toma prioriza las líneas y los importes. | `4fa3343f2b9dd1d0b1e4d9eb188fcea58b0fe209a4666d9afdce6b3d5e56d960` |

## Originales conservados con cursor o halo

| Archivo | Condición que hace falta recuperar | Procedimiento de recaptura |
| --- | --- | --- |
| `pedidos.jpg` | Pedido histórico de Ana antes del despacho, cuando aún estaba PAGADO. | Recuperar una copia del punto previo al despacho y reproducir P08 en Carrito y Pedidos. Conserva la captura archivada si solo existe el SQLite posterior, con ENTREGADO. |
| `dpi125-foco.jpg` | Recorrido de foco a 120 DPI. | Seleccionar escala del 125 % en el monitor físico, reabrir la compilación corregida, navegar con TAB de Buscar cliente a Buscar y capturar el borde de foco. |
| `dpi125-reentrada.jpg` | Reentrada a Auditoría normal con la secuencia y cifras originales. | A 120 DPI abrir Panel → Auditoría y Reportes → Auditoría en ventana normal. Contrastar en pantalla las cuatro cifras; cotejar 0/0/0/3 antes de sustituir la figura. |
| `dpi125-max.jpg` | Auditoría maximizada a 120 DPI. | Repetir en ventana maximizada de la misma sesión y cotejar 0/0/0/3. |
| `outbox-fallo.jpg` | Primer intento con error en el evento histórico. | Usar una copia anterior al primer procesamiento. Con ADMIN en modo demostración, inducir el fallo, reprocesar una vez y verificar Intentos=1, fecha de procesamiento vacía y error. |
| `outbox-recuperado.jpg` | Segundo intento exitoso del mismo evento y contadores de esa sesión. | Continuar el escenario del primer intento. Desactivar el fallo, reprocesar, verificar Intentos=2 y fecha de procesamiento, y comprobar que cobro y movimientos se conservan únicos. |
| `metricas-panel.jpg` | Contador de una venta en la misma sesión del recorrido histórico. | Procesar el evento de P15 en la sesión que se va a capturar. Comparar Panel y Auditoría antes de cerrar la aplicación; cotejar los indicadores de la época. |

Para cada captura coloca el puntero fuera de la ventana y conserva el original cuando alguna cifra, referencia, mensaje o condición esté ausente. Una ejecución nueva con otros identificadores puede servir como comprobación adicional del procedimiento.

**RNF03 permanece PARCIAL.** Las tres figuras DPI conservan evidencia histórica a 120 DPI. Sigue pendiente el cotejo físico de las transiciones a 125 % y el recorrido de la interfaz corregida a 150 % en ambos tamaños. **RNF10 permanece PARCIAL.** Los 13 riesgos registrados incluyen refactorizaciones, correcciones, mitigaciones y propuestas con evidencia de distinto alcance.
