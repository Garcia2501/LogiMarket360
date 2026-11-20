using LogiMarket.Domain.Seguridad;

namespace LogiMarket.Application.Seguridad;

public interface IConsultaUsuarios
{
    Task<Usuario?> BuscarAsync(string nombre, CancellationToken cancellationToken = default);
}

public interface IHashClave
{
    string Crear(string clave);
    bool Verificar(string clave, string hash);
}
