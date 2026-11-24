public static FiltroPedidos Parsear(string texto)
{
    ArgumentNullException.ThrowIfNull(texto);
    if (texto.Length > 4096)
        throw new SintaxisFiltroException(4097, "supera el máximo de 4096 caracteres.");
    return new Lector(texto).Analizar();
}

private sealed class Lector(string texto)
{
    private int _posicion;

    public FiltroPedidos Analizar()
    {
