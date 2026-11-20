# LogiMarket 360

Proyecto académico de la APE 04. La fase 1 establece el dominio, SQLite con EF Core 8 y el arranque con datos iniciales.

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
dotnet build LogiMarket360.sln --no-restore
dotnet test LogiMarket360.sln --no-build --logger 'trx;LogFileName=fase-1.trx'
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

1. Con VENTAS, registra a Ana como VIP, escribe `KIT_TELETRABAJO:1`, selecciona entrega estándar, crea el borrador y valídalo. Conserva el GUID que muestra el formulario.
2. Confirma con cualquiera de las dos pasarelas simuladas. El resultado esperado es USD 59.00 sobre la base recién preparada. Para reintentar el mismo cobro conserva tanto el pedido como la clave de confirmación.
3. Cierra sesión. Con BODEGA, pega el GUID y registra despacho y entrega. El seguimiento muestra la guía y el estado.
4. Con ADMIN, devuelve el pedido por DEFECTO dentro de 30 días. El resultado esperado es USD 54.00, sin flete. Desde la misma pantalla puedes procesar eventos pendientes y elegir aviso EMAIL o SMS simulado.
5. Reinicia contra el mismo archivo. El seguimiento recupera los datos persistidos. Las pruebas automatizadas también comprueban recuentos de stock, pago, auditoría y recuperación del procesador.

El panel actual incluye acceso y recorrido básico de operaciones. Los servicios de Application tienen edición, búsqueda y gestión adicionales. Las vistas completas de catálogo, borradores, inventario, auditoría y reportes se incorporarán según el plan.

## Migración de esquema

Para comprobar solo el esquema con EF Core 8 y un archivo SQLite nuevo:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.34
dotnet ef database update --project src/LogiMarket.Infrastructure --startup-project src/LogiMarket.Infrastructure -- "$env:TEMP\logimarket-migracion.db"
```

Si la herramienta global ya está instalada en otra versión, usa una instalación de EF Core 8.0.34 en un directorio de herramientas separado. La migración prepara el esquema. El arranque ejecuta también el seed.
