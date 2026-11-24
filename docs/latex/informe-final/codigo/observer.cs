    public async Task PublicarAsync(EventoPendiente evento, CanalAviso canal, CancellationToken ct)
    {
        foreach (var observador in observadores)
            await entregas.EntregarAsync(evento, observador, canal, ct);
    }
}
