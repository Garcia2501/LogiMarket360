# Informe APE 04 · LogiMarket 360

Informe en español basado en el commit auditado `12458bc1cf073e2575e1d85bda0f9f0517a1fa19`. La sección Resultados obtenidos contiene los bloques técnicos y sus comprobaciones. PDF en formato Letter, clase institucional `article`.

## Compilar

Desde esta carpeta, con Tectonic y su paquete TeX disponible:

```powershell
tectonic main.tex --keep-logs --keep-intermediates
Copy-Item main.pdf APE04_Informe_APELLIDOS_NOMBRES.pdf
```

Tectonic usa XeTeX y ejecuta BibTeX con las referencias de `sample.bib`. Los archivos `main.tex` y `imagenes/` de `../plantilla-original/` se preservan a nivel de bytes. `evidencias/plantilla-sha256.txt` registra las huellas de las entradas inspeccionadas en el ZIP institucional.

- `resultados/` contiene los capítulos técnicos, las matrices y el índice de artefactos.
- `diagramas/` contiene las fuentes TikZ.
- `codigo/` contiene fragmentos de código del commit auditado.
- `capturas/` contiene imágenes auténticas con origen y hash en `evidencias/capturas.md`.
- `wireframes/` contiene los esquemas de referencia de la guía. Sus cifras son ilustrativas.
- `evidencias/` contiene TRX, salidas de compilación, resultados SQLite no operativos y comprobantes de simulación.

Consulta `VERIFICACION-EDITORIAL.md` para el resultado del control PDF y `PENDIENTES.md` para la portada, DPI y estado de integración.

La revisión de diagramas, la paginación comparada y la procedencia de las recapturas constan en [el registro gráfico](evidencias/registro-revision-grafica.md) y [el registro de cierre visual](evidencias/cierre-visual.md).
