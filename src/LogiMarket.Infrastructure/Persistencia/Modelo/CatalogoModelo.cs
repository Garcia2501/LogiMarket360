using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Inventario;
using LogiMarket.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Modelo;

internal static class CatalogoModelo
{
    public static void Configurar(ModelBuilder model)
    {
        model.Entity<Usuario>(b =>
        {
            b.ToTable("Usuario", t => t.HasCheckConstraint("CK_Usuario_Rol", "Rol IN (1, 2, 3, 4)"));
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.Nombre).UseCollation("NOCASE");
            b.HasIndex(e => e.Nombre).IsUnique();
        });

        model.Entity<Cliente>(b =>
        {
            b.ToTable("Cliente", t => t.HasCheckConstraint("CK_Cliente_Categoria", "Categoria IN (1, 2)"));
            b.Property(e => e.Id).ValueGeneratedNever();
            b.HasIndex(e => e.Identificacion).IsUnique();
            b.HasIndex(e => e.Nombre);
        });

        model.Entity<ProductoSimple>(b =>
        {
            b.ToTable("Producto", t =>
            {
                t.HasCheckConstraint("CK_Producto_Precio", Restricciones.Decimal("Precio"));
                t.HasCheckConstraint("CK_Producto_Peso", Restricciones.Decimal("PesoKg", permiteCero: true));
                t.HasCheckConstraint("CK_Producto_Stock", "typeof(Stock) = 'integer' AND Stock BETWEEN 0 AND 2147483647");
                t.HasCheckConstraint("CK_Producto_Sku", "length(trim(Sku)) > 0");
            });
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.Sku).UseCollation("NOCASE");
            b.Property(e => e.Version).IsConcurrencyToken();
            b.HasIndex(e => e.Sku).IsUnique();
            b.HasIndex(e => e.Nombre);
            b.HasIndex(e => e.Categoria);
        });

        model.Entity<KitCompuesto>(b =>
        {
            b.ToTable("Kit", t => t.HasCheckConstraint("CK_Kit_Sku", "length(trim(Sku)) > 0"));
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.Sku).UseCollation("NOCASE");
            b.Property(e => e.Version).IsConcurrencyToken();
            b.HasIndex(e => e.Sku).IsUnique();
            b.HasIndex(e => e.Nombre);
            b.HasMany(e => e.Componentes).WithOne().HasForeignKey(e => e.KitId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(e => e.Componentes).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        model.Entity<KitComponente>(b =>
        {
            b.ToTable("KitComponente", t =>
            {
                t.HasCheckConstraint("CK_KitComponente_Cantidad", "typeof(Cantidad) = 'integer' AND Cantidad BETWEEN 1 AND 2147483647");
                t.HasCheckConstraint("CK_KitComponente_Tipo", "(ProductoId IS NULL) <> (KitHijoId IS NULL)");
                t.HasCheckConstraint("CK_KitComponente_CicloDirecto", "KitHijoId IS NULL OR KitId <> KitHijoId");
            });
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Ignore(e => e.Elemento);
            b.HasOne(e => e.Producto).WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.KitHijo).WithMany().HasForeignKey(e => e.KitHijoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(e => new { e.KitId, e.ProductoId }).IsUnique();
            b.HasIndex(e => new { e.KitId, e.KitHijoId }).IsUnique();
        });

        model.Entity<MovimientoInventario>(b =>
        {
            b.ToTable("MovimientoInventario", t =>
            {
                t.HasCheckConstraint("CK_Movimiento_Inventario",
                    """
                    typeof(Cantidad) = 'integer' AND Cantidad BETWEEN -2147483648 AND 2147483647
                    AND Cantidad <> 0 AND StockResultante BETWEEN 0 AND 2147483647
                    AND (Tipo = 1 OR (Tipo = 2 AND Cantidad < 0) OR (Tipo = 3 AND Cantidad > 0))
                    """);
            });
            b.Property(e => e.Id).ValueGeneratedNever();
            b.HasOne<ProductoSimple>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(e => new { e.ProductoId, e.FechaUtc });
            b.HasIndex(e => new { e.ProductoId, e.Tipo, e.Referencia }).IsUnique();
        });
    }
}
