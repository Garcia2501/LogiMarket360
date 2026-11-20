namespace LogiMarket.Application.Eventos;

public sealed record EventoComercial(Guid PedidoId, string Tipo, string Referencia, decimal? Importe, DateTime FechaUtc)
{
    public const string PedidoPagado = "PedidoPagado";
    public const string DespachoRegistrado = "DespachoRegistrado";
    public const string DevolucionAceptada = "DevolucionAceptada";
}

public interface IColaEventos
{
    void Agregar(EventoComercial evento);
}
