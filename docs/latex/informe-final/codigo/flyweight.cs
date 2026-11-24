    public RenglonCatalogoVisual CrearRenglon(ProductoVisto producto, int cantidad)
    {
        if (!_metadatos.TryGetValue(producto.Sku, out var metadatos) ||
            metadatos.Nombre != producto.Nombre || metadatos.Categoria != producto.Categoria ||
            metadatos.Descripcion != producto.Descripcion)
        {
            metadatos = CopiarMetadatos(producto);
            _metadatos[producto.Sku] = metadatos;
        }
        return new(metadatos, producto.Stock, cantidad, producto.Precio);
    }

    internal static MetadatosProductoVisual CopiarMetadatos(ProductoVisto p) => new(p.Sku, p.Nombre, p.Categoria, p.Descripcion);
}
