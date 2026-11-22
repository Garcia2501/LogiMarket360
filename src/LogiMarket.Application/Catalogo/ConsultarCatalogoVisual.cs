namespace LogiMarket.Application.Catalogo;

public sealed record MedicionFlyweight(int Renglones, int ObjetosCompartidos, int ObjetosIndependientes,
    long BytesCompartidos, long BytesIndependientes, int Repeticiones);

public sealed record DemostracionFlyweight(IReadOnlyList<RenglonCatalogoVisual> Renglones, MedicionFlyweight Medicion);

public sealed class ConsultarCatalogoVisual(AdministrarCatalogo catalogo)
{
    private const int Repeticiones = 5;

    public async Task<IReadOnlyList<RenglonCatalogoVisual>> ListarAsync(CancellationToken ct = default)
    {
        var productos = await catalogo.BuscarAsync(ct: ct);
        var fabrica = new CatalogoVisualFlyweightFactory();
        return productos.Select(producto => fabrica.CrearRenglon(producto, 1)).ToArray();
    }

    public async Task<DemostracionFlyweight> DemostrarAsync(int renglones = 1000, CancellationToken ct = default)
    {
        if (renglones is < 1000 or > 50_000)
            throw new ArgumentOutOfRangeException(nameof(renglones), "La muestra debe tener entre 1000 y 50000 renglones.");
        var productos = await catalogo.BuscarAsync(ct: ct);
        if (productos.Count == 0)
            throw new InvalidOperationException("El catálogo no tiene productos.");
        return Comparar(productos, renglones);
    }

    private static DemostracionFlyweight Comparar(IReadOnlyList<ProductoVisto> productos, int renglones)
    {
        Proyectar(productos, renglones, compartir: true);
        Proyectar(productos, renglones, compartir: false);
        var bytesCompartidos = new long[Repeticiones];
        var bytesIndependientes = new long[Repeticiones];
        RenglonCatalogoVisual[] compartidos = [], independientes = [];
        for (int intento = 0; intento < Repeticiones; intento++)
        {
            (compartidos, bytesCompartidos[intento]) = Medir(productos, renglones, compartir: true);
            (independientes, bytesIndependientes[intento]) = Medir(productos, renglones, compartir: false);
        }

        Array.Sort(bytesCompartidos);
        Array.Sort(bytesIndependientes);
        return new(compartidos, new MedicionFlyweight(renglones, ContarMetadatos(compartidos),
            ContarMetadatos(independientes), bytesCompartidos[Repeticiones / 2], bytesIndependientes[Repeticiones / 2], Repeticiones));
    }

    private static int ContarMetadatos(RenglonCatalogoVisual[] filas) =>
        filas.Select(f => f.Metadatos).Distinct(ReferenceEqualityComparer.Instance).Count();

    private static (RenglonCatalogoVisual[], long) Medir(IReadOnlyList<ProductoVisto> productos, int renglones, bool compartir)
    {
        long inicio = GC.GetAllocatedBytesForCurrentThread();
        var filas = Proyectar(productos, renglones, compartir);
        return (filas, GC.GetAllocatedBytesForCurrentThread() - inicio);
    }

    private static RenglonCatalogoVisual[] Proyectar(IReadOnlyList<ProductoVisto> productos, int renglones, bool compartir)
    {
        var fabrica = compartir ? new CatalogoVisualFlyweightFactory() : null;
        var filas = new RenglonCatalogoVisual[renglones];
        for (int i = 0; i < filas.Length; i++)
        {
            var producto = productos[i % productos.Count];
            int cantidad = 1 + i % 3;
            filas[i] = fabrica?.CrearRenglon(producto, cantidad) ??
                new(CatalogoVisualFlyweightFactory.CopiarMetadatos(producto), producto.Stock, cantidad, producto.Precio);
        }
        return filas;
    }
}
