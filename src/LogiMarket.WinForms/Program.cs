using LogiMarket.Application.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        bool soloInicializar = args is ["--solo-inicializar"];
        try
        {
            using var servicios = CrearHost();
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

    private static ServiceProvider CrearHost()
    {
        var archivo = Environment.GetEnvironmentVariable("LOGIMARKET360_DB")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LogiMarket360", "logimarket360.db");
        archivo = Path.GetFullPath(archivo);
        Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);

        return new ServiceCollection().AgregarPersistenciaLogiMarket(archivo)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task PrepararPersistenciaAsync(IServiceProvider servicios)
    {
        await using var scope = servicios.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatosIniciales>().InicializarAsync(rol =>
            Environment.GetEnvironmentVariable($"LOGIMARKET360_DEMO_{rol}") ?? "");
    }
}
