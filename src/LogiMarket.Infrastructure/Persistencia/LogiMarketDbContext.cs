using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Inventario;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia.Modelo;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia;

public sealed class LogiMarketDbContext(DbContextOptions<LogiMarketDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ProductoSimple> Productos => Set<ProductoSimple>();
    public DbSet<KitCompuesto> Kits => Set<KitCompuesto>();
    public DbSet<KitComponente> KitComponentes => Set<KitComponente>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoDetalle> PedidoDetalles => Set<PedidoDetalle>();
    public DbSet<PedidoDetalleComponente> PedidoDetalleComponentes => Set<PedidoDetalleComponente>();
    public DbSet<HistorialPedido> HistorialPedidos => Set<HistorialPedido>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();
    public DbSet<PagoRegistro> Pagos => Set<PagoRegistro>();
    public DbSet<DespachoRegistro> Despachos => Set<DespachoRegistro>();
    public DbSet<DevolucionRegistro> Devoluciones => Set<DevolucionRegistro>();
    public DbSet<EventoOutbox> EventosOutbox => Set<EventoOutbox>();
    public DbSet<AuditoriaRegistro> Auditorias => Set<AuditoriaRegistro>();
    public DbSet<EntregaEvento> EntregasEvento => Set<EntregaEvento>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        configuration.Properties<Sku>().HaveConversion<SkuConverter>();
        configuration.Properties<DateTime>().HaveConversion<FechaUtcConverter>();
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        CatalogoModelo.Configurar(model);
        PedidosModelo.Configurar(model);
        OperacionesModelo.Configurar(model);
    }
}
