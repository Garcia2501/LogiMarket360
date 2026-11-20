using System.Security.Cryptography;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.Infrastructure.Seguridad;

public sealed class Pbkdf2HashClave : IHashClave
{
    private const string Formato = "pbkdf2-sha256-v1";
    private const int Iteraciones = 600_000;
    private const int LongitudSal = 16;
    private const int LongitudHash = 32;

    public string Crear(string clave)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);
        var sal = RandomNumberGenerator.GetBytes(LongitudSal);
        var hash = Derivar(clave, sal);
        return $"{Formato}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string clave, string hash)
    {
        if (string.IsNullOrWhiteSpace(clave) || string.IsNullOrWhiteSpace(hash))
            return false;

        var bloques = hash.Split('$');
        if (bloques.Length != 3 || bloques[0] != Formato)
            return false;

        Span<byte> sal = stackalloc byte[LongitudSal];
        Span<byte> esperado = stackalloc byte[LongitudHash];
        if (!Convert.TryFromBase64String(bloques[1], sal, out int bytesSal) || bytesSal != LongitudSal ||
            !Convert.TryFromBase64String(bloques[2], esperado, out int bytesHash) || bytesHash != LongitudHash)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(esperado, Derivar(clave, sal));
    }

    private static byte[] Derivar(string clave, ReadOnlySpan<byte> sal) =>
        Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, LongitudHash);
}
