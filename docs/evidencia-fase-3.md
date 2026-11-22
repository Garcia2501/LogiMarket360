# Evidencia técnica de fase 3

## Ejecución automatizada

El 22 de noviembre de 2026, la base de código `02fe769` fue restaurada, compilada en Release y probada con .NET 8. La suite registró 108 aprobadas (81 bajo Integracion, que incluye casos unitarios del parser y de colaboradores, 21 bajo Dominio y 6 de Arquitectura), 0 fallidas y 0 omitidas. El nombre del TRX local es `phase3-final.trx`. La compilación terminó con 0 errores y 0 advertencias. EF Core 8 aplicó la migración inicial a un archivo SQLite inexistente y no encontró cambios pendientes de modelo.

Para reproducir:

```powershell
dotnet restore LogiMarket360.sln
dotnet build LogiMarket360.sln -c Release --no-restore
dotnet test LogiMarket360.sln -c Release --no-build --logger 'trx;LogFileName=phase3-final.trx'
```

## Medición Flyweight P21

`FlyweightTests.P21_MilRenglonesCompartenCincoInstanciasYMidenAsignaciones` carga desde SQLite los cinco productos oficiales como CONSULTA. Construye las mismas 1.000 filas por modalidad, en el mismo orden, con la misma oferta de SKU y los mismos datos extrínsecos. La variante independiente crea un objeto de metadatos por fila. La variante compartida incluye el costo de crear y llenar su diccionario. Tras calentar ambas variantes, se tomaron cinco medidas por modalidad y se comparó la mediana de `GC.GetAllocatedBytesForCurrentThread`. La lectura de SQLite, los controles WinForms y el recuento de identidades se excluyeron del intervalo medido. El ensayo mide bytes asignados durante la proyección, no memoria residente ni retenida.

| Modalidad | Filas | Metadatos distintos por referencia | Mediana de bytes asignados |
| --- | ---: | ---: | ---: |
| Compartida | 1.000 | 5 | 56.752 |
| Independiente | 1.000 | 1.000 | 104.024 |

La prueba verificó identidad con `Assert.Same`. La demostración WinForms mostró los mismos recuentos y bytes en el recorrido visual. La diferencia observada fue de 47.272 bytes, aproximadamente 45,4 % en este escenario. El resultado depende de la proporción de SKU repetidos.

## Ejecución WinForms P17 P24 P28

Se abrió la clase WinForms de producción desde un lanzador temporal. El lanzador preparó una base aparte, inició una sesión ficticia por el servicio de autenticación y creó el pedido validado de Ana VIP con KIT_TELETRABAJO ×1. Después se operaron los controles del panel real:

1. Confirmar cobro mostró USD 59.00 y la tabla de pedidos cambió a PAGADO. Catálogo e inventario mostraron P01/P02/P03 en 7/4/19. El resumen indicó carrito sin borrador editable y un pedido con cobro.
2. Vista previa de venta abrió una ventana que mostró el desglose y total. Después de despachar, entregar y devolver por DEFECTO, la vista previa de devolución mostró USD 54.00 y la exclusión del flete. El inventario volvió a 8/5/20.
3. Se exportaron ambos tipos con Guardar TXT. Los archivos se leyeron y cotejaron con lo mostrado. La base ficticia pasó `integrity_check` y `foreign_key_check`. Su auditoría registró `ConfirmarPedidoCommand`, `DespacharPedidoCommand` y `ProcesarDevolucionCommand` como aprobados.
4. En el catálogo se abrió la demostración de 1.000 renglones y se observó la proyección con cantidades extrínsecas y su resultado de memoria.

Los artefactos de comprobación se conservaron fuera de Git. Los archivos exportados proceden del escenario ficticio.

| Archivo | SHA-256 |
| --- | --- |
| `comprobante-venta.txt` | `3918D69566A9472F2D7CEF30B7569AA85B5EACD5B9645D75CC4E995796E0307C` |
| `comprobante-devolucion.txt` | `28B14677F5DC949CDA53F18A87CC8C73FE12B0BE4A02BF6144B3D19F68210882` |

La sesión se preparó por servicio para el fixture. Este recorrido no sustituye la verificación visual de acceso, la métrica compartida entre dos vistas, la inducción de error Outbox desde la interfaz ni el ensayo de reinicio completo de Windows Forms.
