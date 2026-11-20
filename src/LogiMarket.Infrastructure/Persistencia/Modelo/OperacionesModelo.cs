using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Modelo;

internal static class OperacionesModelo
{
    public static void Configurar(ModelBuilder model)
    {
        model.Entity<PagoRegistro>(b =>
        {
            b.ToTable("Pago", t =>
            {
                t.HasCheckConstraint("CK_Pago_Subtotal", Restricciones.Decimal("Subtotal"));
                t.HasCheckConstraint("CK_Pago_Descuento", Restricciones.Decimal("Descuento", permiteCero: true));
                t.HasCheckConstraint("CK_Pago_Tarifa", Restricciones.Decimal("Tarifa", permiteCero: true));
                t.HasCheckConstraint("CK_Pago_Importe", Restricciones.Decimal("Importe"));
                t.HasCheckConstraint("CK_Pago_Referencias",
                    "length(trim(Referencia)) > 0 AND length(trim(ClaveIdempotencia)) > 0");
            });
            b.HasOne<Pedido>().WithOne().HasForeignKey<PagoRegistro>(e => e.PedidoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(e => e.Referencia).IsUnique();
            b.HasIndex(e => e.ClaveIdempotencia).IsUnique();
        });

        model.Entity<DespachoRegistro>(b =>
        {
            b.ToTable("Despacho", t => t.HasCheckConstraint("CK_Despacho_PlazoYFecha",
                "PlazoDias > 0 AND (FechaEntregaUtc IS NULL OR FechaEntregaUtc >= FechaDespachoUtc)"));
            b.HasOne<Pedido>().WithOne().HasForeignKey<DespachoRegistro>(e => e.PedidoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(e => e.Guia).IsUnique();
        });

        model.Entity<DevolucionRegistro>(b =>
        {
            b.ToTable("Devolucion", t =>
            {
                t.HasCheckConstraint("CK_Devolucion_Motivo", "Motivo IN ('DEFECTO', 'ARREPENTIMIENTO')");
                t.HasCheckConstraint("CK_Devolucion_Importe", Restricciones.Decimal("ImporteReembolso", permiteCero: true));
                t.HasCheckConstraint("CK_Devolucion_Referencia", "length(trim(ReferenciaReembolso)) > 0");
            });
            b.HasOne<Pedido>().WithOne().HasForeignKey<DevolucionRegistro>(e => e.PedidoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(e => e.ReferenciaReembolso).IsUnique();
        });

        model.Entity<EventoOutbox>(b =>
        {
            b.ToTable("EventoOutbox", t => t.HasCheckConstraint("CK_Outbox_Intentos", "Intentos >= 0"));
            b.HasOne<Pedido>().WithMany().HasForeignKey(e => e.PedidoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(e => new { e.ProcesadoUtc, e.FechaUtc });
        });

        model.Entity<AuditoriaRegistro>(b =>
        {
            b.ToTable("Auditoria");
            b.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(e => e.FechaUtc);
            b.HasIndex(e => e.Correlacion);
        });

        model.Entity<EntregaEvento>(b =>
        {
            b.ToTable("EntregaEvento");
            b.HasKey(e => new { e.EventoId, e.Suscriptor });
            b.HasOne<EventoOutbox>().WithMany().HasForeignKey(e => e.EventoId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
