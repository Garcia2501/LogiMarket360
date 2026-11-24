public sealed class CreadorEmail : CreadorAviso
{
    public override CanalAviso Canal => CanalAviso.EMAIL;
    public override IAvisoSimulado Crear(IRegistroAvisosSimulados registro) => new EmailSimulado(registro);

    private sealed class EmailSimulado(IRegistroAvisosSimulados registro) : IAvisoSimulado
    {
        public void Enviar(EventoPendiente evento) => registro.Registrar(new(evento.Id, CanalAviso.EMAIL,
            evento.Datos.Tipo, $"Asunto: {evento.Datos.Tipo}. Pedido {evento.Datos.PedidoId:D}; referencia {evento.Datos.Referencia}.",
            evento.Datos.FechaUtc));
    }
}
