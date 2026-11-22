using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Reportes;

public sealed record PedidoReportado(Guid Id, EstadoPedido Estado, string Cliente, decimal? TotalCobrado);

public interface ILecturaReportePedidos
{
    Task<IReadOnlyList<PedidoReportado>> ListarAsync(CancellationToken ct);
}

public sealed class ConsultarReportePedidos(AutorizacionOperaciones acceso, ILecturaReportePedidos datos)
{
    public async Task<IReadOnlyList<PedidoReportado>> EjecutarAsync(string? expresion, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        var filtro = string.IsNullOrWhiteSpace(expresion) ? null : ParserFiltroPedido.Parsear(expresion);
        var pedidos = await datos.ListarAsync(ct);
        return filtro is null ? pedidos : pedidos.Where(filtro.Cumple).ToArray();
    }
}
