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

Por decisión del estudiante se conservan estas siete capturas auténticas en sus archivos originales. El cursor y el halo son indicadores de captura y no modifican los estados, importes, datos o resultados funcionales registrados.

| Archivo original | Figura | Evidencia que conserva |
| --- | --- | --- |
| `pedidos.jpg` | 37 | Pedido histórico de Ana en estado PAGADO y seguimiento del pedido. |
| `dpi125-foco.jpg` | 44 | Foco de teclado en Buscar a 120 DPI. |
| `dpi125-reentrada.jpg` | 45 | Cifras de Auditoría al volver a la vista en ventana normal, a 120 DPI. |
| `dpi125-max.jpg` | 46 | Auditoría maximizada con cuatro valores visibles en el JPEG, a 120 DPI. |
| `outbox-fallo.jpg` | 50 | Primer intento del evento histórico, pendiente y con error. |
| `outbox-recuperado.jpg` | 51 | Segundo intento exitoso del mismo evento. |
| `metricas-panel.jpg` | 52 | Contadores del Panel general durante la misma sesión del recorrido histórico. |

**RNF03 permanece PARCIAL.** Las tres figuras DPI conservan evidencia histórica a 120 DPI. El cotejo físico de las transiciones a 125 % sigue pendiente, y la versión corregida no está verificada al 150 % en ambos tamaños.

**RNF10 permanece PARCIAL.** Los 13 riesgos registrados incluyen refactorizaciones, correcciones, mitigaciones y propuestas con evidencia de distinto alcance.
