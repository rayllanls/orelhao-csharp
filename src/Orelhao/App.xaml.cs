using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Orelhao.Services;

namespace Orelhao;

public partial class App : Application
{
    /// <summary>Estado compartilhado por todas as páginas (fonte única de verdade).</summary>
    public static ShortcutStore Store { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Nenhuma exceção deve fechar o app sem deixar rastro. Registra e mostra.
        DispatcherUnhandledException += OnUnhandled;

        // AppUserModelID próprio: o Windows não agrupa o app sob o ícone do
        // host na barra de tarefas e usa o ícone/janela corretos.
        try { SetCurrentProcessExplicitAppUserModelID("Rayllan.Orelhao.1"); }
        catch { /* não é crítico se falhar */ }

        Store.Load();
        ThemeService.Apply(Store.Theme, Store.AccentColor);

        var window = new MainWindow();
        window.Show();

        if (Store.RecoveryMessage is { } msg)
            MessageBox.Show(msg, "Banco de dados", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "orelhao_error.log"),
                e.Exception.ToString());
        }
        catch { /* nada a fazer se nem logar der */ }

        MessageBox.Show(
            "Ocorreu um erro inesperado, mas o app continua aberto.\n\n" + e.Exception.Message,
            "Orelhão", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true; // não derruba o app
    }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern void SetCurrentProcessExplicitAppUserModelID(
        [MarshalAs(UnmanagedType.LPWStr)] string appID);
}
