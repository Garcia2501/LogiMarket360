public async Task<RespuestaPasarela> CobrarAsync(decimal importe, string clave, CancellationToken ct = default)
{
    trazas.Anotar(proveedor, clave, "INICIO", "SOLICITADO");
    try
    {
        trazas.Anotar(proveedor, clave, "PASARELA", "INVOCADA");
        var resultado = await siguiente.CobrarAsync(importe, clave, ct);
        trazas.Anotar(proveedor, clave, "FIN", resultado.Aprobado ? "APROBADO" : "RECHAZADO");
        return resultado;
    }
    catch (Exception error)
    {
        trazas.Anotar(proveedor, clave, "ERROR", error.GetType().Name);
        throw;
