using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Modelo;

internal static class PedidosModelo
{
    public static void Configurar(ModelBuilder model)
    {
        model.Entity<Pedido>(b =>
        {
            b.ToTable("Pedido", t =>
            {
                t.HasCheckConstraint("CK_Pedido_Estado", "Estado BETWEEN 1 AND 6");
                t.HasCheckConstraint("CK_Pedido_Entrega", "Entrega IN (1, 2)");
            });
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.Version).IsConcurrencyToken();
            b.Ignore(e => e.Subtotal);
            b.HasIndex(e => new { e.Estado, e.FechaCreacionUtc });
            b.HasIndex(e => e.FechaCreacionUtc);
            b.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(e => e.Lineas).WithOne().HasForeignKey(e => e.PedidoId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(e => e.Historial).WithOne().HasForeignKey(e => e.PedidoId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(e => e.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(e => e.Historial).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        model.Entity<PedidoDetalle>(b =>
        {
            b.ToTable("PedidoDetalle", t =>
            {
                t.HasCheckConstraint("CK_PedidoDetalle_Cantidad", "typeof(Cantidad) = 'integer' AND Cantidad BETWEEN 1 AND 2147483647");
                t.HasCheckConstraint("CK_PedidoDetalle_Precio", Restricciones.Decimal("PrecioUnitario"));
            });
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Ignore(e => e.Subtotal);
            b.HasMany(e => e.Componentes).WithOne().HasForeignKey(e => e.PedidoDetalleId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(e => e.Componentes).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        model.Entity<PedidoDetalleComponente>(b =>
        {
            b.ToTable("PedidoDetalleComponente", t =>
            {
                t.HasCheckConstraint("CK_Snapshot_Cantidades",
                    "CantidadEnPadre BETWEEN 1 AND 2147483647 AND CantidadPorUnidad BETWEEN CantidadEnPadre AND 2147483647");
                t.HasCheckConstraint("CK_Snapshot_Precio", Restricciones.Decimal("PrecioUnitario"));
                t.HasCheckConstraint("CK_Snapshot_Peso", Restricciones.Decimal("PesoKg", permiteCero: true));
            });
            b.Property(e => e.Id).ValueGeneratedNever();
            b.HasIndex(e => new { e.PedidoDetalleId, e.Ruta }).IsUnique();
        });

        model.Entity<HistorialPedido>(b =>
        {
            b.ToTable("HistorialPedido", t => t.HasCheckConstraint("CK_Historial_Secuencia",
                "Secuencia > 0 AND Nuevo BETWEEN 1 AND 6 AND (Anterior IS NULL OR Anterior BETWEEN 1 AND 6)"));
            b.Property(e => e.Id).ValueGeneratedNever();
            b.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(e => new { e.PedidoId, e.Secuencia }).IsUnique();
        });
    }
}
