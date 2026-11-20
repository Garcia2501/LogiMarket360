using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.Tests.Integracion;

internal sealed class EntornoComercial(Action<IServiceCollection>? configurar = null) : IAsyncDisposable
{
    private readonly BaseSqlite _base = new();
    private ServiceProvider _servicios = null!;

    public async Task InicializarAsync()
    {
        _servicios = ConstruirServicios();
        await using var scope = Abrir();
        await scope.ServiceProvider.GetRequiredService<DatosIniciales>().InicializarAsync(Clave);
    }

    public async Task ReiniciarAsync()
    {
        await _servicios.DisposeAsync();
        _servicios = ConstruirServicios();
    }

    private ServiceProvider ConstruirServicios()
    {
        var registro = new ServiceCollection().AgregarPersistenciaLogiMarket(_base.Archivo);
        configurar?.Invoke(registro);
        return registro.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public string Clave(Rol rol) => _base.ClavesDemo[rol];
    public AsyncServiceScope Abrir() => _servicios.CreateAsyncScope();
    public LogiMarketDbContext AbrirBase() => _base.Abrir();

    public async Task EntrarAsync(Rol rol)
    {
        await using var scope = Abrir();
        var acceso = scope.ServiceProvider.GetRequiredService<AutenticacionLocal>();
        if (!await acceso.IniciarAsync($"{rol.ToString().ToLowerInvariant()}.demo", Clave(rol)))
            throw new InvalidOperationException("No se pudo iniciar la sesión de prueba.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_servicios is not null)
            await _servicios.DisposeAsync();
        _base.Dispose();
    }
}
