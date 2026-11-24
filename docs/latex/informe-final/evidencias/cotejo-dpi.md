# Cotejo de capturas DPI al 125 %

La bitácora original se conserva en resultados-125.md. En los JPEG archivados de Auditoría normal, maximizada y de reentrada están los cuatro valores 0, 0, 0 y 3. En el JPEG de Panel normal también se distinguen Pedidos activos y Por despachar.

Dos previsualizaciones mostraron cifras ausentes, como anotó la bitácora. Para contrastarlas se decodificaron directamente los JPEG. En las imágenes de Auditoría normal y de reentrada, ambas de 970 × 663 píxeles, el intervalo vertical [232, 259) y ventanas de 18 píxeles centradas cerca de x=273, 413, 570 y 744 contienen 138, 138, 145 y 109 píxeles por debajo de luminancia 100, respectivamente. Los píxeles forman tres glifos cero y el glifo tres. La extracción del objeto JPEG embebido en el PDF conserva la huella del archivo de reentrada. Los nombres y hashes de los archivos están en capturas.md.

El cotejo sostiene que las cifras están en el material archivado. El origen de la diferencia con la previsualización no se ha establecido, y se mantiene pendiente la validación del recorrido en el monitor físico. La compilación corregida a 144 DPI no se ejecutó. RNF03 sigue PARCIAL.
