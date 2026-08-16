using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Orelhao.Views;

/// <summary>Caixa de texto simples e estilizada (nome de grupo, renomear etc.).</summary>
public partial class InputDialog : FluentWindow
{
    public string Value => InputBox.Text.Trim();

    private InputDialog(string title, string prompt, string initial)
    {
        InitializeComponent();
        Title = title;
        TitleBar.Title = title;
        PromptText.Text = prompt;
        InputBox.Text = initial;
        Loaded += (_, _) => { InputBox.Focus(); InputBox.SelectAll(); };
    }

    /// <summary>Mostra o diálogo e devolve o texto, ou null se cancelado/vazio.</summary>
    public static string? Ask(Window owner, string title, string prompt, string initial = "")
    {
        var dlg = new InputDialog(title, prompt, initial) { Owner = owner };
        if (dlg.ShowDialog() == true && dlg.Value.Length > 0)
            return dlg.Value;
        return null;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnOk(sender, e);
        else if (e.Key == Key.Escape) OnCancel(sender, e);
    }

    private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
