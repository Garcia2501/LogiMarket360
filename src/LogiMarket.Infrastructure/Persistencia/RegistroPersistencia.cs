using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pagos;
using LogiMarket.Infrastructure.Pagos;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Precios;
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
        servicios.AddSingleton(ConexionSqlite.CrearOpciones(archivo));
        servicios.AddScoped<LogiMarketDbContext>();
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
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddSingleton<SesionLocal>();
        servicios.AddScoped<AutenticacionLocal>();
        servicios.AddScoped<AutorizacionOperaciones>();
        servicios.AddScoped<IAuditoriaAcceso, AuditoriaAcceso>();
        servicios.AddScoped<AdministrarCatalogo>();
        servicios.AddScoped<AdministrarClientes>();
        servicios.AddSingleton<IPoliticaDescuento, SinDescuento>();
        servicios.AddSingleton<IPoliticaDescuento, Vip10>();
        servicios.AddSingleton<IPoliticaDescuento, Mayorista15>();
        servicios.AddSingleton<CalculadoraPedido>();
        servicios.AddSingleton<FabricaEntrega, FabricaEntregaEstandar>();
        servicios.AddSingleton<FabricaEntrega, FabricaEntregaExpres>();
        servicios.AddSingleton<FamiliasEntrega>();
        servicios.AddScoped<CadenaValidacionPedido>();
        servicios.AddScoped<AdministrarPedidos>();
        servicios.AddSingleton<EstadoSimulacionPago>();
        servicios.AddScoped<PasarelaLegadaSimulada>();
        servicios.AddScoped<PasarelaLegadaAdapter>();
        servicios.AddScoped<PasarelaAlternaSimulada>();
        servicios.AddScoped<IResolutorPasarelas, ResolutorPasarelas>();
        servicios.AddScoped<ITrazasPago, TrazasPagoSqlite>();
        servicios.AddScoped<IPagosRegistrados, PagosRegistrados>();
        servicios.AddScoped<IColaEventos, ColaEventosSqlite>();
        servicios.AddScoped<ConfirmacionPedidoFacade>();
        servicios.AddScoped<IDespachosRegistrados, DespachosRegistrados>();
        servicios.AddScoped<DespacharPedidos>();
        return servicios;
    }
}
