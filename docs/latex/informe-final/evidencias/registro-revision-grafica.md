# Registro de revisión editorial y gráfica

24 de noviembre de 2026. Corte de implementación 12458bc. El informe de respaldo conserva 81 páginas y el revisado tiene 82.

## Diagramas

| Figura y nombre | Hallazgo original | Intervención | Fuente editable | Página anterior → revisada | Estado |
| --- | --- | --- | --- | --- | --- |
| 1. Contexto | La etiqueta entre rol y aplicación invadía las cajas. | Separó actor, aplicación y almacenamiento; encaminó simulación y salida por rutas laterales. | diagramas/contexto.tex | 5 → 5 | Revisado en PDF |
| 2. Casos de uso | Varias puntas convergían en el mismo punto de autorización. | Ordenó cuatro roles por fila, distinguió usos elípticos y agrupó las llegadas en una dependencia al control de acceso. | diagramas/casos.tex | 6 → 5 | Revisado en PDF |
| 3. Clases del catálogo y pedido | Rombos ausentes y relación de producto o kit ambigua. | Separó catálogo de pedido; añadió composición, realización y el alcance de KitComponente. | diagramas/entidades.tex | 9 → 8 | Revisado en PDF |
| 4. Modelo de datos | Arcos largos cruzaban nodos y agrupaciones de claves. | Organizó por dominio; rotuló claves foráneas, unicidad y PK de EntregaEvento. | diagramas/relacional.tex | 9 → 9 | Revisado en PDF |
| 5. Estado del pedido | Las etiquetas rozaban los límites; devolución podía leerse como estado. | Elevó rótulos y separó la operación de devolución de las transiciones. | diagramas/estado.tex | 10 → 9 | Revisado en PDF |
| 6. Dependencias de arquitectura | Dependencias cruzadas y rótulos superpuestos. | Separó capas y Tests, usó rutas de dependencia y limitó el uso de Infrastructure en WinForms a composición. | diagramas/componentes.tex | 10 → 10 | Revisado en PDF |
| 7. Confirmación aprobada | Una interacción mezclaba idempotencia, validación y transacción. | Separó cinco líneas de vida, activaciones, retornos y orden de commit. | diagramas/confirmacion.tex | 13 → 12 | Revisado en PDF |
| 8. Operación rechazada | Falta de stock y rechazo de cobro aparecían como pasos consecutivos. | Delimitó caminos alternativos y salida común sin confirmar; marcó excepciones. | diagramas/rechazo.tex | 14 → 13 | Revisado en PDF |
| 9. Factory Method | Los creadores concretos y los productos quedaban implícitos. | Mostró la jerarquía de creadores y el contrato de los avisos. | diagramas/patron-factory-method.tex | 16 → 15 | Revisado en PDF |
| 10. Abstract Factory | El diagrama aplanaba fábricas e integrantes de cada familia. | Separó las variantes estándar y exprés y los tres objetos producidos. | diagramas/patron-abstract-factory.tex | 17 → 16 | Revisado en PDF |
| 11. Builder | El constructor y el agregado no mostraban operaciones ni línea asociada. | Añadió Agregar/Construir, agregado, línea e instantánea. | diagramas/patron-builder.tex | 18 → 17 | Revisado en PDF |
| 12. Prototype | Parecía que el clon era otro tipo. | Identificó el mismo tipo Pedido, el acto de clonación y líneas copiadas. | diagramas/patron-prototype.tex | 19 → 18 | Revisado en PDF |
| 13. Singleton | Resultaba ambiguo quién registraba la instancia y quién la consultaba. | Mostró registro de DI, observador y consumidor de la misma instancia. | diagramas/patron-singleton.tex | 20 → 19 | Revisado en PDF |
| 14. Adapter | No distinguía el puerto de ambos proveedores ni la operación legada. | Separó interfaz y realizaciones; hizo explícita la conversión a centavos. | diagramas/patron-adapter.tex | 21 → 20 | Revisado en PDF |
| 15. Bridge | Las dos dimensiones figuraban como cajas agregadas. | Separó jerarquías de comprobante y medio de salida en cuatro clases. | diagramas/patron-bridge.tex | 22 → 21 | Revisado en PDF |
| 16. Composite | Rótulos junto a bordes y composición sin símbolo distinguible. | Añadió realización y rombo; distinguió hoja, kit y componente. | diagramas/patron-composite.tex | 23 → 22 | Revisado en PDF |
| 17. Decorator | El contrato compartido y el puerto de auditoría no aparecían. | Identificó IPasarelaPago, trazas y la llamada al ejemplo de adaptador. | diagramas/patron-decorator.tex | 24 → 23 | Revisado en PDF |
| 18. Facade | Los colaboradores estaban comprimidos en una caja. | Separó validación, transacción, pagos, inventario y evento. | diagramas/patron-facade.tex | 25 → 24 | Revisado en PDF |
| 19. Flyweight | La compartición entre renglones no resultaba visible. | Mostró dos referencias a los mismos metadatos y la información extrínseca de cada fila. | diagramas/patron-flyweight.tex | 26 → 25 | Revisado en PDF |
| 20. Proxy | La autorización previa estaba solo en prosa. | Mostró el puerto, la comprobación de Cobrar y la cadena aproximada por selección legada. | diagramas/patron-proxy.tex | 27 → 26 | Revisado en PDF |
| 21. Chain of Responsibility | Se omitían los eslabones y el orden. | Identificó Paso y representó las cuatro validaciones encadenadas. | diagramas/patron-chain-of-responsibility.tex | 28 → 27 | Revisado en PDF |
| 22. Command | El invocador y el receptor se confundían con el objeto comando. | Distinguió comando, contrato, receptor y registro del resultado. | diagramas/patron-command.tex | 29 → 28 | Revisado en PDF |
| 23. Interpreter | El AST y los nodos terminales no se distinguían. | Mostró el contrato Cumple y la separación de nodos simples y binarios. | diagramas/patron-interpreter.tex | 30 → 29 | Revisado en PDF |
| 24. Iterator | El origen congelado y las hojas del recorrido no se veían. | Añadió snapshot, iterador y ComponenteFisico resultante. | diagramas/patron-iterator.tex | 31 → 30 | Revisado en PDF |
| 25. Mediator | Los colaboradores de vista se reducían a un rótulo. | Distribuyó suscripciones por áreas e identificó el método de notificación. | diagramas/patron-mediator.tex | 32 → 31 | Revisado en PDF |
| 26. Memento | Se confundían guardián, originador y captura. | Separó HistorialBorrador, Pedido y el memento anidado. | diagramas/patron-memento.tex | 33 → 32 | Revisado en PDF |
| 27. Observer | El diagrama no separaba los suscriptores concretos. | Mostró avisos, métricas y auditoría como realizaciones del mismo contrato. | diagramas/patron-observer.tex | 34 → 33 | Revisado en PDF |
| 28. State | Las transiciones y clases concretas se confundían. | Separó EstadoDelPedido de los estados concretos y su valor de persistencia. | diagramas/patron-state.tex | 35 → 34 | Revisado en PDF |
| 29. Strategy | No se veía la sustitución de políticas. | Representó la interfaz y las tres estrategias con sus tasas documentadas. | diagramas/patron-strategy.tex | 36 → 35 | Revisado en PDF |
| 30. Template Method | Los motivos y el algoritmo de retorno se comprimían. | Separó las políticas y dejó explícito el esqueleto del método plantilla. | diagramas/patron-template-method.tex | 37 → 36 | Revisado en PDF |
| 31. Visitor | Faltaban el despacho por tipo y las realizaciones concretas. | Separó los dos elementos y la jerarquía de visitantes para peso y desglose. | diagramas/patron-visitor.tex | 38 → 37 | Revisado en PDF |

## Capturas y maquetación

- Siete recapturas nativas a 96 DPI, byte por byte sin retoque. Los archivos originales siguen en capturas. Véanse [recapturas](recapturas-revision.md) y [el inventario de imágenes](inventario-visual.md).
- Doce figuras históricas mantienen puntero o halo. El registro identifica el escenario y por qué sigue pendiente cada recaptura. Se preservaron las cifras, controles y mensajes.
- Tres figuras originales ya estaban libres de superposición y se conservaron.
- Se ampliaron inventario y precio a una figura por página y se separaron las dos ventanas de Auditoría a 120 DPI. Se preservó su proporción. Los listados quiebran en espacios. Se revisaron tablas, escala, orden, pies, índice y referencias.
- No se halló evidencia posterior suficiente para cerrar la comprobación física pendiente a 120 DPI ni la ejecución corregida a 144 DPI. Las nuevas recapturas fueron a 96 DPI.

## Compilación y cotejo

- Tectonic/XeTeX y BibTeX, salida 0. Ningún error, advertencia, referencia indefinida ni caja desbordada.
- 31 diagramas TikZ vectoriales y editables, 22 figuras de ejecución, 12 wireframes agrupados en seis láminas y ocho referencias. Se mantuvieron los 23 patrones, 38 casos, 28 requisitos y los registros de resultados de pruebas.
- Fuentes y logotipos de la plantilla histórica conservados sin cambios. El PDF anterior se mantiene como respaldo.
- SHA-256 del PDF revisado: 995d295fefecec9f73fa1163265d1da2518e96cdf310d2deb482e898939d6642.
