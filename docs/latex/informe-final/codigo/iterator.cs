public static IEnumerable<ComponenteFisico> Recorrer(IElementoCatalogo elemento, int cantidad = 1)
{
    ArgumentNullException.ThrowIfNull(elemento);
    return Recorrer(elemento.Capturar(), cantidad);
}

internal static IEnumerable<ComponenteFisico> Recorrer(ComponenteCatalogoSnapshot raiz, int cantidad)
{
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
    return Enumerar(raiz, cantidad);
}

private static IEnumerable<ComponenteFisico> Enumerar(ComponenteCatalogoSnapshot raiz, int cantidad)
{
    var pila = new Stack<(ComponenteCatalogoSnapshot Nodo, int Multiplicador)>();
    pila.Push((raiz, cantidad));
