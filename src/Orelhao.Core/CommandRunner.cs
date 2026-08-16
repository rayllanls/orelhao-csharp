using System.Diagnostics;

namespace Orelhao.Core;

/// <summary>Abre cada comando numa janela de terminal nova.</summary>
public static class CommandRunner
{
    /// <summary>
    /// Lança <paramref name="command"/> numa janela nova, preferindo o Windows
    /// Terminal (wt) e caindo para PowerShell e depois cmd. -NoExit / /k mantêm
    /// a janela aberta depois que o comando roda (sessão SSH, por exemplo).
    /// </summary>
    public static void Run(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        // 1) Windows Terminal, se instalado.
        if (TryStart("wt", new[] { "powershell", "-NoExit", "-Command", command }, newConsole: false))
            return;

        // 2) PowerShell em console próprio.
        if (TryStart("powershell", new[] { "-NoExit", "-Command", command }, newConsole: true))
            return;

        // 3) Último recurso: cmd.
        TryStart("cmd", new[] { "/k", command }, newConsole: true);
    }

    private static bool TryStart(string fileName, IReadOnlyList<string> args, bool newConsole)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            foreach (var a in args)
                psi.ArgumentList.Add(a);
            Process.Start(psi);
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            // Executável não encontrado ou não pôde iniciar: tenta o próximo.
            return false;
        }
    }
}
