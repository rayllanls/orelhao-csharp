using System.IO;

namespace Orelhao.Services;

/// <summary>
/// Onde ficam os dados: sempre ao lado do executável. <see cref="Environment.ProcessPath"/>
/// aponta para o .exe de verdade (mesmo empacotado single-file), então o
/// shortcuts.json nasce na pasta do programa — não numa temporária que o
/// Windows apaga ao fechar.
/// </summary>
public static class AppPaths
{
    public static string DataDir
    {
        get
        {
            var exe = Environment.ProcessPath;
            var dir = exe is null ? null : Path.GetDirectoryName(exe);
            return dir ?? AppContext.BaseDirectory;
        }
    }

    public static string DataFile => Path.Combine(DataDir, "shortcuts.json");
}
