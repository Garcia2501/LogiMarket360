# Verificación editorial de la versión de respaldo

La revisión posterior se documenta en [Registro de revisión gráfica](evidencias/registro-revision-grafica.md).

**Documento** APE04_Informe_APELLIDOS_NOMBRES.pdf  
**Fecha** 23 de noviembre de 2026  
**Corte de código** 12458bc1cf073e2575e1d85bda0f9f0517a1fa19

## Plantilla y ajustes

- La copia `plantilla-original` conserva exactamente las tres entradas del ZIP institucional. Sus huellas SHA-256 se cotejaron contra el archivo de origen y se registran en `evidencias/plantilla-sha256.txt`.
- La copia de trabajo mantiene la clase `article`, tamaño Letter, los dos logotipos, encabezado, portada, apartados y casillas. Se actualizó el idioma a español y se quitó la identidad académica ajena. Los campos desconocidos tienen un marcador explícito.
- Se delimitó el área del encabezado dentro de la geometría, con márgenes laterales originales de 3 cm, ajuste del margen inferior y folio. Se añadieron tablas multipágina, listados C#, índice enlazado, TikZ, pies y referencias cruzadas, y el archivo bibliográfico faltante. Las fuentes institucionales de imagen se conservaron.
- El detalle técnico está en Resultados obtenidos. Los anexos finales indexan los archivos que acompañan el informe.

## Cotejo del PDF

| Comprobación | Resultado |
| --- | --- |
| Motor y compilación | Tectonic 0.17.0, XeTeX y BibTeX. Salida 0, 81 páginas Letter. |
| Registro final | Cero errores, advertencias de LaTeX, referencias indefinidas, caracteres ausentes y cajas desbordadas. |
| Bibliografía | Ocho referencias numeradas, resueltas con el estilo institucional `ieeetr`. |
| Navegación | 132 anotaciones de enlace en el PDF. Se cotejaron destinos internos y enlaces bibliográficos del archivo. |
| Presentación | Se renderizaron e inspeccionaron las 81 páginas. Se revisaron portada, folios e índice, encabezados, tablas extensas, código, diagramas, capturas, pies, referencias y anexos. Se corrigieron páginas de continuación evitables y se volvió a revisar el resultado afectado. |
| Cobertura | 28 requisitos, 23 patrones, 38 casos, 12 vistas y 13 riesgos. Cada patrón incluye diagrama editable y fragmento del código auditado. |
| Imágenes | 22 capturas auténticas insertadas, 46 en la carpeta con su manifiesto. Se incluyen aparte los 12 wireframes, presentados en seis láminas. |
| Diagramas | 31 fuentes TikZ. Ocho diagramas generales y 23 colaboraciones de patrones. |
| Huella PDF | `6d2b3c51cc4126b8a2222e10f65600f40fef4171b02271a7b20dfd029dcee7cf` |

Se conservan los registros finales de LaTeX, BibTeX y el inventario de páginas en `evidencias/`. Las capturas de Windows Forms y los wireframes se identifican por separado. El cotejo de DPI se documenta en `evidencias/cotejo-dpi.md`.

## Verificación técnica de la solución

Desde una copia limpia de la referencia auditada, .NET SDK 8.0.428 restauró los cinco proyectos, compiló Release con cero advertencias y errores, y ejecutó 119 pruebas de las que aprobaron 119. El cotejo aislado de P23 con cliente ausente aprobó 1 de 1 en una ejecución aparte. Se conservan TRX y salidas. `PENDIENTES.md` registra la verificación física de DPI, el estado de ramas y los campos de portada.

