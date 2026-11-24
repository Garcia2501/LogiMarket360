public sealed class PasarelaLegadaAdapter(PasarelaLegadaSimulada legado) : IPasarelaPago
{
    public Task<RespuestaPasarela> CobrarAsync(decimal importe, string clave, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var centavos = ReferenciaSimulada.Centavos(importe);
        return Task.FromResult(ReferenciaSimulada.Respuesta(legado.CobrarCentavos(centavos, clave)));
    }
}

public sealed class PasarelaAlternaSimulada(EstadoSimulacionPago simulacion, EstadoDemostracion demo) : IPasarelaPago
