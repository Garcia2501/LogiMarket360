using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Inventario;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Persistencia;
using LogiMarket.Application.Seguridad;
using LogiMarket.Infrastructure.Persistencia.Repositorios;
using LogiMarket.Infrastructure.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.Infrastructure.Persistencia;

public static class RegistroPersistencia
{
    public static IServiceCollection AgregarPersistenciaLogiMarket(this IServiceCollection servicios, string archivo)
    {
        servicios.AddScoped(_ => new LogiMarketDbContext(ConexionSqlite.CrearOpciones(archivo)));
        servicios.AddScoped<RepositorioCatalogo>();
        servicios.AddScoped<IConsultaCatalogo>(s => s.GetRequiredService<RepositorioCatalogo>());
        servicios.AddScoped<IEscrituraCatalogo>(s => s.GetRequiredService<RepositorioCatalogo>());
        servicios.AddScoped<IRepositorioClientes, RepositorioClientes>();
        servicios.AddScoped<IRepositorioPedidos, RepositorioPedidos>();
        servicios.AddScoped<IConsultaUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<IRegistroInventario, RegistroInventario>();
        servicios.AddScoped<UnidadDeTrabajoSqlite>();
        servicios.AddScoped<IUnidadDeTrabajo>(s => s.GetRequiredService<UnidadDeTrabajoSqlite>());
        servicios.AddScoped<DatosIniciales>();
        servicios.AddSingleton<IHashClave, Pbkdf2HashClave>();
        return servicios;
    }
}
