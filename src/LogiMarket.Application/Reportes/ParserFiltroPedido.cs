using System.Globalization;
using System.Text;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Reportes;

public sealed class SintaxisFiltroException(int posicion, string detalle)
    : FormatException($"Filtro inválido en la columna {posicion}: {detalle}")
{
    public int Posicion { get; } = posicion;
}

public static class ParserFiltroPedido
{
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
            var filtro = LeerOr(0);
            SaltarEspacios();
            if (_posicion != texto.Length)
                throw Error("se esperaba AND, OR o el final de la expresión.");
            return filtro;
        }

        private FiltroPedidos LeerOr(int profundidad)
        {
            var filtro = LeerAnd(profundidad);
            while (ConsumirPalabra("OR"))
                filtro = new FiltroOr(filtro, LeerAnd(profundidad));
            return filtro;
        }

        private FiltroPedidos LeerAnd(int profundidad)
        {
            var filtro = LeerPrimario(profundidad);
            while (ConsumirPalabra("AND"))
                filtro = new FiltroAnd(filtro, LeerPrimario(profundidad));
            return filtro;
        }

        private FiltroPedidos LeerPrimario(int profundidad)
        {
            if (Consumir("("))
            {
                if (profundidad >= 48)
                    throw Error("hay demasiados paréntesis anidados.");
                var contenido = LeerOr(profundidad + 1);
                Exigir(")");
                return contenido;
            }

            var campo = Palabra().ToUpperInvariant();
            return campo switch
            {
                "ESTADO" => Estado(),
                "TOTAL" => Total(),
                "CLIENTE" => Cliente(),
                _ => throw Error("se esperaba ESTADO, TOTAL, CLIENTE o '('.")
            };
        }

        private FiltroPedidos Estado()
        {
            Exigir("=");
            var valor = Palabra();
            if (!Enum.GetNames<EstadoPedido>().Contains(valor, StringComparer.OrdinalIgnoreCase))
                throw Error("el estado de pedido no existe.");
            return new FiltroEstado(Enum.Parse<EstadoPedido>(valor, ignoreCase: true));
        }

        private FiltroPedidos Total()
        {
            Exigir(">=");
            var valor = Palabra();
            if (!decimal.TryParse(valor, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var monto))
                throw Error("se esperaba un número decimal con punto.");
            return new FiltroTotal(monto);
        }

        private FiltroPedidos Cliente()
        {
            Exigir("=");
            if (!Consumir("\""))
            {
                var nombre = Palabra();
                if (nombre.Length == 0 || EsPalabraOperador(nombre))
                    throw Error("se esperaba un cliente; usa comillas para nombres con espacios.");
                return new FiltroCliente(nombre);
            }

            var partes = new StringBuilder();
            while (_posicion < texto.Length)
            {
                char letra = texto[_posicion++];
                if (letra != '"')
                {
                    partes.Append(letra);
                    continue;
                }
                if (_posicion < texto.Length && texto[_posicion] == '"')
                {
                    _posicion++;
                    partes.Append('"');
                    continue;
                }
                if (partes.Length == 0)
                    throw Error("el nombre de cliente no puede estar vacío.");
                return new FiltroCliente(partes.ToString());
            }
            throw Error("falta cerrar las comillas del nombre de cliente.");
        }

        private bool ConsumirPalabra(string esperada)
        {
            int inicio = _posicion;
            if (string.Equals(Palabra(), esperada, StringComparison.OrdinalIgnoreCase))
                return true;
            _posicion = inicio;
            return false;
        }

        private string Palabra()
        {
            SaltarEspacios();
            int inicio = _posicion;
            while (_posicion < texto.Length && !char.IsWhiteSpace(texto[_posicion])
                && texto[_posicion] is not ('(' or ')' or '=' or '>' or '<' or '"'))
                _posicion++;
            return texto[inicio.._posicion];
        }

        private void Exigir(string esperado)
        {
            if (!Consumir(esperado))
                throw Error($"se esperaba '{esperado}'.");
        }

        private bool Consumir(string valor)
        {
            SaltarEspacios();
            if (!texto.AsSpan(_posicion).StartsWith(valor, StringComparison.Ordinal))
                return false;
            _posicion += valor.Length;
            return true;
        }

        private void SaltarEspacios()
        {
            while (_posicion < texto.Length && char.IsWhiteSpace(texto[_posicion]))
                _posicion++;
        }

        private static bool EsPalabraOperador(string palabra) =>
            palabra.Equals("AND", StringComparison.OrdinalIgnoreCase) || palabra.Equals("OR", StringComparison.OrdinalIgnoreCase);

        private SintaxisFiltroException Error(string detalle) => new(_posicion + 1, detalle);
    }
}
