using LogiMarket.Application.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        bool soloInicializar = args.Contains("--solo-inicializar", StringComparer.Ordinal);
        bool demo = args.Contains("--demo", StringComparer.Ordinal);
        try
        {
            using var servicios = CrearHost(demo);
            PrepararPersistenciaAsync(servicios).GetAwaiter().GetResult();
            if (!soloInicializar)
            {
                ApplicationConfiguration.Initialize();
                System.Windows.Forms.Application.Run(new VentanaComercial(servicios, servicios.GetRequiredService<SesionLocal>()));
            }
            return 0;
        }
        catch (Exception error)
        {
            if (soloInicializar)
                Console.Error.WriteLine(error.Message);
            else
                MessageBox.Show(error.Message, "No se pudo abrir LogiMarket 360",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static ServiceProvider CrearHost(bool demo)
    {
        var baseLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LogiMarket360");
        var operativa = Path.GetFullPath(Environment.GetEnvironmentVariable("LOGIMARKET360_DB")
            ?? Path.Combine(baseLocal, "logimarket360.db"));
        var demostracion = Path.Combine(baseLocal, "Demostracion", "logimarket360-demo.db");
        if (string.Equals(operativa, demostracion, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La ruta de demostración está reservada para el modo --demo.");
        var archivo = demo ? demostracion : operativa;
        Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);

        return new ServiceCollection().AgregarPersistenciaLogiMarket(archivo, demo)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task PrepararPersistenciaAsync(IServiceProvider servicios)
    {
        await using var scope = servicios.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatosIniciales>().InicializarAsync(rol =>
            Environment.GetEnvironmentVariable($"LOGIMARKET360_DEMO_{rol}") ?? "");
    }
}
