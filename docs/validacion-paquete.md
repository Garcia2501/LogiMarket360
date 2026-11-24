# Validación del paquete de entrega

## Diferencia de modelo detectada al extraer el ZIP

La primera prueba del ZIP produjo 118 aprobadas y un fallo en
`InicializacionTests.P01_SeedDesdeArchivoAusenteEsIdempotenteYPersiste`, en
`HasPendingModelChanges()`. La misma prueba pasó en el checkout WinForms de
Windows. Los resultados originales de cada ejecución se conservaron por separado.

Para reproducir con el código previo a la corrección, exportar
la solución y sus proyectos desde `16b7373` con `git archive` y ejecutar
`dotnet test LogiMarket360.sln -c Release --filter FullyQualifiedName~P01_Seed`
en la extracción. El archivo `CatalogoModelo.cs` del ZIP tenía finales LF
y el del checkout tenía CRLF.

`CK_Movimiento_Inventario` se construía con un literal multilínea que usaba
los saltos del archivo fuente. EF Core comparaba esa cadena con el snapshot de
la migración, que conserva CRLF explícitos. Se fijaron los saltos del literal
para que ambas compilaciones produzcan la misma definición de restricción.
La migración existente, el predicado y los datos se conservan. P01 sigue
comprobando la equivalencia entre el modelo actual y su snapshot.
