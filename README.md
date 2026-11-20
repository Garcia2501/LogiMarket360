# LogiMarket 360

Esqueleto académico de la APE 04. Contiene cinco proyectos de Clean Architecture. WinForms abre una ventana inicial. La persistencia, los flujos y las pruebas funcionales se implementarán por fases.

## Documentación

- [Análisis de la guía y matriz de requisitos](docs/analisis-guia.md)
- [Diseño técnico preliminar](docs/diseno-tecnico.md)
- [Plan maestro y gestión de Git y GitHub](docs/plan-maestro.md)
- [Matriz de patrones GoF](docs/matriz-gof.md)
- [Matriz de aceptación P01–P38 y pruebas automatizadas](docs/matriz-aceptacion.md)
- [Historial de desarrollo](docs/historial-desarrollo.md)

## Compilación del esqueleto

En Windows, con el SDK .NET 8 (8.0.400 o una banda posterior dentro de .NET 8) y, si se usa Visual Studio, con la carga de desarrollo de escritorio de .NET:

```powershell
dotnet restore LogiMarket360.sln
dotnet build LogiMarket360.sln --no-restore
dotnet run --project src/LogiMarket.WinForms
```

El proyecto de pruebas usa xUnit. El plan incluye incorporar al README final las instrucciones verificadas para migrar, inicializar y restablecer la base de prueba, iniciar sesión con cuentas de demostración y ejecutar la suite.
