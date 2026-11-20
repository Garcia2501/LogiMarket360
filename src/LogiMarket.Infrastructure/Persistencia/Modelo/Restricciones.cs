namespace LogiMarket.Infrastructure.Persistencia.Modelo;

internal static class Restricciones
{
    public static string Decimal(string columna, bool permiteCero = false)
    {
        var operador = permiteCero ? ">=" : ">";
        return $"CASE WHEN json_valid({columna}) THEN json_type({columna}) IN ('integer', 'real') " +
            $"AND CAST({columna} AS NUMERIC) {operador} 0 ELSE 0 END";
    }
}
