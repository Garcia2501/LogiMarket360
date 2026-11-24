    public ValoracionPedido Calcular(BasePromocion datos, decimal tarifa)
    {
        var politica = _politicas.First(p => p.Aplica(datos));
        var descuento = Redondear(datos.Subtotal * politica.Tasa);
        return new(datos.Subtotal, descuento, tarifa, Redondear(datos.Subtotal - descuento + tarifa),
            politica.Nombre, datos.UnidadesFisicas);
    }

    public static decimal Redondear(decimal monto) => decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
}
