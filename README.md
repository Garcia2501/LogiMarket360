# LogiMarket 360

Proyecto académico de la APE 04. Aplicación Windows Forms con SQLite, EF Core 8, el catálogo oficial y operaciones comerciales simuladas.

## Documentación

- [Análisis de la guía y matriz de requisitos](docs/analisis-guia.md)
- [Diseño técnico preliminar](docs/diseno-tecnico.md)
- [Plan maestro y gestión de Git y GitHub](docs/plan-maestro.md)
- [Matriz de patrones GoF](docs/matriz-gof.md)
- [Matriz de aceptación P01–P38 y pruebas automatizadas](docs/matriz-aceptacion.md)
- [Historial de desarrollo](docs/historial-desarrollo.md)

## Compilación y pruebas

Requiere Windows y SDK .NET 8. Para Visual Studio 2022, instala la carga de escritorio de .NET.

```powershell
dotnet restore LogiMarket360.sln
dotnet build LogiMarket360.sln -c Release --no-restore
dotnet test LogiMarket360.sln -c Release --no-build --logger 'trx;LogFileName=logimarket360.trx'
```

Las pruebas xUnit usan archivos SQLite temporales aislados. P01 verifica la migración, los cinco productos, dos kits y cuatro roles, así como el seed repetido.

## Inicialización de la base local

El archivo predeterminado es `%LOCALAPPDATA%\LogiMarket360\logimarket360.db`. Para aislar una demostración, asigna a `LOGIMARKET360_DB` la ruta de un archivo nuevo. La primera ejecución aplica la migración e inserta los registros oficiales faltantes. Los usuarios ficticios son `admin.demo`, `ventas.demo`, `bodega.demo` y `consulta.demo`. Suministra una contraseña por rol para crear las cuentas.

```powershell
$roles = 'ADMIN','VENTAS','BODEGA','CONSULTA'
try {
    foreach ($rol in $roles) {
        $entrada = Read-Host "Clave ficticia para $rol" -AsSecureString
        $valor = [Net.NetworkCredential]::new('', $entrada).Password
        [Environment]::SetEnvironmentVariable("LOGIMARKET360_DEMO_$rol", $valor, 'Process')
    }
    dotnet run --project src/LogiMarket.WinForms -- --solo-inicializar
    if ($LASTEXITCODE -ne 0) { throw 'La inicialización no se completó.' }
} finally {
    foreach ($rol in $roles) {
        [Environment]::SetEnvironmentVariable("LOGIMARKET360_DEMO_$rol", $null, 'Process')
    }
    Remove-Variable valor, entrada -ErrorAction SilentlyContinue
}
```

Las claves se suministran temporalmente al proceso y SQLite conserva sus hashes con sal. Si las cuatro cuentas ya existen, el arranque repetido conserva inventario, precios y composición sin volver a pedir claves. Para restablecer una demostración, apunta `LOGIMARKET360_DB` a otro archivo vacío y repite la inicialización.

Usa `dotnet run --project src/LogiMarket.WinForms` e inicia sesión con una cuenta ficticia configurada localmente.

## Demostración del núcleo comercial

Para una base de demostración con los controles de fallo habilitados, agrega `--demo` al comando de inicialización y de arranque:

```powershell
dotnet run --project src/LogiMarket.WinForms -- --demo
```

El modo de demostración usa `%LOCALAPPDATA%\LogiMarket360\Demostracion\logimarket360-demo.db`. Para inicializar sus cuentas, usa el mismo procedimiento de claves anterior con `--demo --solo-inicializar`. El modo de demostración ignora la ruta operativa configurada en `LOGIMARKET360_DB`.

1. Con VENTAS, registra a Ana como VIP en **Clientes**. En **Carrito y borradores**, selecciona a Ana, KIT_TELETRABAJO y entrega estándar. Crea y valida el borrador.
2. En **Confirmación y cobro**, elige el pedido y una pasarela simulada. Sobre una base oficial nueva, verifica subtotal USD 60.00, descuento USD 6.00, envío USD 5.00 y total USD 59.00.
3. Con BODEGA, selecciona el pedido en **Despacho y entrega**, emite la guía y confirma la entrega.
4. Con ADMIN, valida en **Devoluciones** el motivo DEFECTO. El reembolso previsto es USD 54.00. Revisa el importe antes de confirmar.
5. Abre **Auditoría y demostración** para consultar el estado de Outbox. En modo de demostración puedes inducir el fallo del notificador y reprocesar avisos locales por EMAIL o SMS simulado. **Panel general** muestra los indicadores del negocio.
6. En **Reportes y comprobantes**, filtra por período y expresión. Selecciona el pedido y un tipo de comprobante para verlo o guardarlo en TXT.

Consulta el [recorrido de las 12 vistas](docs/interfaz-winforms.md) y las comprobaciones registradas.

## Migración de esquema

Para comprobar solo el esquema con EF Core 8 y un archivo SQLite nuevo:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.34
dotnet ef database update --project src/LogiMarket.Infrastructure --startup-project src/LogiMarket.Infrastructure -- "$env:TEMP\logimarket-migracion.db"
```

Si la herramienta global ya está instalada en otra versión, usa una instalación de EF Core 8.0.34 en un directorio de herramientas separado. La migración prepara el esquema. El arranque ejecuta también el seed.

## Patrones y consultas en WinForms

- En **Carrito y borradores**, selecciona un borrador y usa **Clonar borrador**, **Cambiar cantidad**, **Deshacer** o **Rehacer**. El historial conserva hasta cinco estados en la sesión de aplicación y queda bloqueado al salir de BORRADOR.
- En **Catálogo**, elige un SKU para consultar peso, desglose y existencias. **Medir 1.000 filas** repite los metadatos del catálogo bajo dos estrategias y muestra los recuentos y bytes asignados.
- En **Reportes y comprobantes**, filtra con `ESTADO = PAGADO AND TOTAL >= 50`. `AND` tiene prioridad sobre `OR`. Usa paréntesis para agrupar y comillas para nombres con espacios, por ejemplo `CLIENTE = "Ana QA"`. `TOTAL` representa el monto efectivamente cobrado.
- Selecciona el pedido en la cuadrícula. Selecciona venta o devolución y elige **Vista previa** o **Guardar TXT**. Los comprobantes son internos.

Consulta el [registro técnico de fase 3](docs/evidencia-fase-3.md) para reproducir las comprobaciones.
