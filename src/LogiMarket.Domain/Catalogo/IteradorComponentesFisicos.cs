namespace LogiMarket.Domain.Catalogo;

public sealed record ComponenteFisico(Sku Sku, int Cantidad);

// patron iterator: recorre las hojas en profundidad sin exponer la pila ni el arbol
public static class IteradorComponentesFisicos
{
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

        while (pila.TryPop(out var entrada))
        {
            int unidades = checked(entrada.Nodo.Cantidad * entrada.Multiplicador);
            if (entrada.Nodo.Componentes.Count == 0)
            {
                yield return new ComponenteFisico(entrada.Nodo.Sku, unidades);
                continue;
            }

            foreach (var hijo in entrada.Nodo.Componentes.OrderByDescending(c => c.Sku.Valor, StringComparer.Ordinal))
                pila.Push((hijo, unidades));
        }
    }
}
