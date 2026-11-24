    public async Task<RespuestaPasarela> CobrarAsync(decimal importe, string clave, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Cobrar, ct);
        return await siguiente.CobrarAsync(importe, clave, ct);
    }
}
