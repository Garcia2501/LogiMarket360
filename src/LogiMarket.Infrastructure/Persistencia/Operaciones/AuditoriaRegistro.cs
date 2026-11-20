namespace LogiMarket.Infrastructure.Persistencia.Operaciones;

public sealed class AuditoriaRegistro
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? UsuarioId { get; init; }
    public required string Categoria { get; init; }
    public required string Accion { get; init; }
    public required string Resultado { get; init; }
    public required string Correlacion { get; init; }
    public string? Referencia { get; init; }
    public DateTime FechaUtc { get; init; }
}
