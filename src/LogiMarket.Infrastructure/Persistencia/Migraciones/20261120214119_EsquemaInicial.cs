using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMarket.Infrastructure.Persistencia.Migraciones
{
    public partial class EsquemaInicial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Identificacion = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    Contacto = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.Id);
                    table.CheckConstraint("CK_Cliente_Categoria", "Categoria IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "Kit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kit", x => x.Id);
                    table.CheckConstraint("CK_Kit_Sku", "length(trim(Sku)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Producto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", nullable: false),
                    PesoKg = table.Column<decimal>(type: "TEXT", nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.Id);
                    table.CheckConstraint("CK_Producto_Peso", "CASE WHEN json_valid(PesoKg) THEN json_type(PesoKg) IN ('integer', 'real') AND CAST(PesoKg AS NUMERIC) >= 0 ELSE 0 END");
                    table.CheckConstraint("CK_Producto_Precio", "CASE WHEN json_valid(Precio) THEN json_type(Precio) IN ('integer', 'real') AND CAST(Precio AS NUMERIC) > 0 ELSE 0 END");
                    table.CheckConstraint("CK_Producto_Sku", "length(trim(Sku)) > 0");
                    table.CheckConstraint("CK_Producto_Stock", "typeof(Stock) = 'integer' AND Stock BETWEEN 0 AND 2147483647");
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    HashClave = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.CheckConstraint("CK_Usuario_Rol", "Rol IN (1, 2, 3, 4)");
                });

            migrationBuilder.CreateTable(
                name: "Pedido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Entrega = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedido", x => x.Id);
                    table.CheckConstraint("CK_Pedido_Entrega", "Entrega IN (1, 2)");
                    table.CheckConstraint("CK_Pedido_Estado", "Estado BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_Pedido_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitComponente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    KitHijoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitComponente", x => x.Id);
                    table.CheckConstraint("CK_KitComponente_Cantidad", "typeof(Cantidad) = 'integer' AND Cantidad BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("CK_KitComponente_CicloDirecto", "KitHijoId IS NULL OR KitId <> KitHijoId");
                    table.CheckConstraint("CK_KitComponente_Tipo", "(ProductoId IS NULL) <> (KitHijoId IS NULL)");
                    table.ForeignKey(
                        name: "FK_KitComponente_Kit_KitHijoId",
                        column: x => x.KitHijoId,
                        principalTable: "Kit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitComponente_Kit_KitId",
                        column: x => x.KitId,
                        principalTable: "Kit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitComponente_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientoInventario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", nullable: false),
                    StockResultante = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientoInventario", x => x.Id);
                    table.CheckConstraint("CK_Movimiento_Inventario", "typeof(Cantidad) = 'integer' AND Cantidad BETWEEN -2147483648 AND 2147483647\r\nAND Cantidad <> 0 AND StockResultante BETWEEN 0 AND 2147483647\r\nAND (Tipo = 1 OR (Tipo = 2 AND Cantidad < 0) OR (Tipo = 3 AND Cantidad > 0))");
                    table.ForeignKey(
                        name: "FK_MovimientoInventario_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Categoria = table.Column<string>(type: "TEXT", nullable: false),
                    Accion = table.Column<string>(type: "TEXT", nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", nullable: false),
                    Correlacion = table.Column<string>(type: "TEXT", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", nullable: true),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auditoria_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Despacho",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Guia = table.Column<string>(type: "TEXT", nullable: false),
                    PlazoDias = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaDespachoUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaEntregaUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despacho", x => x.Id);
                    table.CheckConstraint("CK_Despacho_PlazoYFecha", "PlazoDias > 0 AND (FechaEntregaUtc IS NULL OR FechaEntregaUtc >= FechaDespachoUtc)");
                    table.ForeignKey(
                        name: "FK_Despacho_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Devolucion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", nullable: false),
                    ProductoSinUso = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReintegroInventario = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReferenciaReembolso = table.Column<string>(type: "TEXT", nullable: false),
                    ImporteReembolso = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devolucion", x => x.Id);
                    table.CheckConstraint("CK_Devolucion_Importe", "CASE WHEN json_valid(ImporteReembolso) THEN json_type(ImporteReembolso) IN ('integer', 'real') AND CAST(ImporteReembolso AS NUMERIC) >= 0 ELSE 0 END");
                    table.CheckConstraint("CK_Devolucion_Motivo", "Motivo IN ('DEFECTO', 'ARREPENTIMIENTO')");
                    table.CheckConstraint("CK_Devolucion_Referencia", "length(trim(ReferenciaReembolso)) > 0");
                    table.ForeignKey(
                        name: "FK_Devolucion_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventoOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Contenido = table.Column<string>(type: "TEXT", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcesadoUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Intentos = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventoOutbox", x => x.Id);
                    table.CheckConstraint("CK_Outbox_Intentos", "Intentos >= 0");
                    table.ForeignKey(
                        name: "FK_EventoOutbox_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPedido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Secuencia = table.Column<int>(type: "INTEGER", nullable: false),
                    Anterior = table.Column<int>(type: "INTEGER", nullable: true),
                    Nuevo = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPedido", x => x.Id);
                    table.CheckConstraint("CK_Historial_Secuencia", "Secuencia > 0 AND Nuevo BETWEEN 1 AND 6 AND (Anterior IS NULL OR Anterior BETWEEN 1 AND 6)");
                    table.ForeignKey(
                        name: "FK_HistorialPedido_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialPedido_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", nullable: false),
                    ClaveIdempotencia = table.Column<string>(type: "TEXT", nullable: false),
                    Proveedor = table.Column<string>(type: "TEXT", nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Descuento = table.Column<decimal>(type: "TEXT", nullable: false),
                    Tarifa = table.Column<decimal>(type: "TEXT", nullable: false),
                    Importe = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => x.Id);
                    table.CheckConstraint("CK_Pago_Descuento", "CASE WHEN json_valid(Descuento) THEN json_type(Descuento) IN ('integer', 'real') AND CAST(Descuento AS NUMERIC) >= 0 ELSE 0 END");
                    table.CheckConstraint("CK_Pago_Importe", "CASE WHEN json_valid(Importe) THEN json_type(Importe) IN ('integer', 'real') AND CAST(Importe AS NUMERIC) > 0 ELSE 0 END");
                    table.CheckConstraint("CK_Pago_Referencias", "length(trim(Referencia)) > 0 AND length(trim(ClaveIdempotencia)) > 0");
                    table.CheckConstraint("CK_Pago_Subtotal", "CASE WHEN json_valid(Subtotal) THEN json_type(Subtotal) IN ('integer', 'real') AND CAST(Subtotal AS NUMERIC) > 0 ELSE 0 END");
                    table.CheckConstraint("CK_Pago_Tarifa", "CASE WHEN json_valid(Tarifa) THEN json_type(Tarifa) IN ('integer', 'real') AND CAST(Tarifa AS NUMERIC) >= 0 ELSE 0 END");
                    table.ForeignKey(
                        name: "FK_Pago_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoDetalle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoDetalle", x => x.Id);
                    table.CheckConstraint("CK_PedidoDetalle_Cantidad", "typeof(Cantidad) = 'integer' AND Cantidad BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("CK_PedidoDetalle_Precio", "CASE WHEN json_valid(PrecioUnitario) THEN json_type(PrecioUnitario) IN ('integer', 'real') AND CAST(PrecioUnitario AS NUMERIC) > 0 ELSE 0 END");
                    table.ForeignKey(
                        name: "FK_PedidoDetalle_Pedido_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregaEvento",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Suscriptor = table.Column<string>(type: "TEXT", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregaEvento", x => new { x.EventoId, x.Suscriptor });
                    table.ForeignKey(
                        name: "FK_EntregaEvento_EventoOutbox_EventoId",
                        column: x => x.EventoId,
                        principalTable: "EventoOutbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoDetalleComponente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoDetalleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ruta = table.Column<string>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    CantidadEnPadre = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadPorUnidad = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "TEXT", nullable: false),
                    PesoKg = table.Column<decimal>(type: "TEXT", nullable: false),
                    EsProducto = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoDetalleComponente", x => x.Id);
                    table.CheckConstraint("CK_Snapshot_Cantidades", "CantidadEnPadre BETWEEN 1 AND 2147483647 AND CantidadPorUnidad BETWEEN CantidadEnPadre AND 2147483647");
                    table.CheckConstraint("CK_Snapshot_Peso", "CASE WHEN json_valid(PesoKg) THEN json_type(PesoKg) IN ('integer', 'real') AND CAST(PesoKg AS NUMERIC) >= 0 ELSE 0 END");
                    table.CheckConstraint("CK_Snapshot_Precio", "CASE WHEN json_valid(PrecioUnitario) THEN json_type(PrecioUnitario) IN ('integer', 'real') AND CAST(PrecioUnitario AS NUMERIC) > 0 ELSE 0 END");
                    table.ForeignKey(
                        name: "FK_PedidoDetalleComponente_PedidoDetalle_PedidoDetalleId",
                        column: x => x.PedidoDetalleId,
                        principalTable: "PedidoDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Correlacion",
                table: "Auditoria",
                column: "Correlacion");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_FechaUtc",
                table: "Auditoria",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_UsuarioId",
                table: "Auditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Identificacion",
                table: "Cliente",
                column: "Identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Nombre",
                table: "Cliente",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Despacho_Guia",
                table: "Despacho",
                column: "Guia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Despacho_PedidoId",
                table: "Despacho",
                column: "PedidoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devolucion_PedidoId",
                table: "Devolucion",
                column: "PedidoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devolucion_ReferenciaReembolso",
                table: "Devolucion",
                column: "ReferenciaReembolso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventoOutbox_PedidoId",
                table: "EventoOutbox",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventoOutbox_ProcesadoUtc_FechaUtc",
                table: "EventoOutbox",
                columns: new[] { "ProcesadoUtc", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPedido_PedidoId_Secuencia",
                table: "HistorialPedido",
                columns: new[] { "PedidoId", "Secuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPedido_UsuarioId",
                table: "HistorialPedido",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Kit_Nombre",
                table: "Kit",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Kit_Sku",
                table: "Kit",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitComponente_KitHijoId",
                table: "KitComponente",
                column: "KitHijoId");

            migrationBuilder.CreateIndex(
                name: "IX_KitComponente_KitId_KitHijoId",
                table: "KitComponente",
                columns: new[] { "KitId", "KitHijoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitComponente_KitId_ProductoId",
                table: "KitComponente",
                columns: new[] { "KitId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitComponente_ProductoId",
                table: "KitComponente",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_ProductoId_FechaUtc",
                table: "MovimientoInventario",
                columns: new[] { "ProductoId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_ProductoId_Tipo_Referencia",
                table: "MovimientoInventario",
                columns: new[] { "ProductoId", "Tipo", "Referencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_ClaveIdempotencia",
                table: "Pago",
                column: "ClaveIdempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_PedidoId",
                table: "Pago",
                column: "PedidoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_Referencia",
                table: "Pago",
                column: "Referencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_ClienteId",
                table: "Pedido",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_Estado_FechaCreacionUtc",
                table: "Pedido",
                columns: new[] { "Estado", "FechaCreacionUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_FechaCreacionUtc",
                table: "Pedido",
                column: "FechaCreacionUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalle_PedidoId",
                table: "PedidoDetalle",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalleComponente_PedidoDetalleId_Ruta",
                table: "PedidoDetalleComponente",
                columns: new[] { "PedidoDetalleId", "Ruta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Categoria",
                table: "Producto",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Nombre",
                table: "Producto",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Sku",
                table: "Producto",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Nombre",
                table: "Usuario",
                column: "Nombre",
                unique: true);

            AgregarProteccionesDeCatalogo(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Producto_Sku_DeKit_Alta");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Producto_Sku_DeKit_Cambio");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Kit_Sku_DeProducto_Alta");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Kit_Sku_DeProducto_Cambio");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_KitComponente_Ciclo_Alta");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_KitComponente_Ciclo_Cambio");
            migrationBuilder.DropTable(
                name: "Auditoria");

            migrationBuilder.DropTable(
                name: "Despacho");

            migrationBuilder.DropTable(
                name: "Devolucion");

            migrationBuilder.DropTable(
                name: "EntregaEvento");

            migrationBuilder.DropTable(
                name: "HistorialPedido");

            migrationBuilder.DropTable(
                name: "KitComponente");

            migrationBuilder.DropTable(
                name: "MovimientoInventario");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "PedidoDetalleComponente");

            migrationBuilder.DropTable(
                name: "EventoOutbox");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Kit");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "PedidoDetalle");

            migrationBuilder.DropTable(
                name: "Pedido");

            migrationBuilder.DropTable(
                name: "Cliente");
        }
        private static void AgregarProteccionesDeCatalogo(MigrationBuilder migracion)
        {
            CrearProteccionSku(migracion, "Producto", "Kit", "Alta", "INSERT");
            CrearProteccionSku(migracion, "Producto", "Kit", "Cambio", "UPDATE OF Sku");
            CrearProteccionSku(migracion, "Kit", "Producto", "Alta", "INSERT");
            CrearProteccionSku(migracion, "Kit", "Producto", "Cambio", "UPDATE OF Sku");
            CrearProteccionCiclo(migracion, "Alta", "INSERT", "");
            CrearProteccionCiclo(migracion, "Cambio", "UPDATE OF KitId, KitHijoId", "AND enlace.Id <> OLD.Id");
        }

        private static void CrearProteccionSku(MigrationBuilder migracion, string tabla, string otra,
            string nombre, string operacion)
        {
            migracion.Sql($"""
                CREATE TRIGGER TR_{tabla}_Sku_De{otra}_{nombre}
                BEFORE {operacion} ON {tabla}
                WHEN EXISTS (SELECT 1 FROM {otra} WHERE Sku = NEW.Sku COLLATE NOCASE)
                BEGIN
                    SELECT RAISE(ABORT, 'El SKU ya existe en el catalogo');
                END;
                """);
        }

        private static void CrearProteccionCiclo(MigrationBuilder migracion, string nombre,
            string operacion, string excluirEnlaceAnterior)
        {
            migracion.Sql($"""
                CREATE TRIGGER TR_KitComponente_Ciclo_{nombre}
                BEFORE {operacion} ON KitComponente
                WHEN NEW.KitHijoId IS NOT NULL
                BEGIN
                    SELECT RAISE(ABORT, 'La composicion del kit contiene un ciclo')
                    WHERE EXISTS (
                        WITH RECURSIVE descendientes(Id) AS (
                            SELECT NEW.KitHijoId
                            UNION
                            SELECT enlace.KitHijoId
                            FROM KitComponente AS enlace
                            JOIN descendientes ON enlace.KitId = descendientes.Id
                            WHERE enlace.KitHijoId IS NOT NULL {excluirEnlaceAnterior}
                        )
                        SELECT 1 FROM descendientes WHERE Id = NEW.KitId
                    );
                END;
                """);
        }

    }
}
