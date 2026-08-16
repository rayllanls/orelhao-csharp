using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Orelhao.Core;
using Orelhao.Services;

namespace Orelhao.Views;

public partial class SettingsPage : Page
{
    private static ShortcutStore Store => App.Store;

    public SettingsPage()
    {
        InitializeComponent();

        DnsList.ItemsSource = Store.DnsServers;
        DefaultIdBox.Text = Store.SshDefaults.Id;
        DefaultJumpBox.Text = Store.SshDefaults.JumpProxy;

        UpdateThemeSummary();
    }

    private void UpdateThemeSummary()
    {
        ThemeSwatch.Background = new SolidColorBrush(ThemeService.Accent);
        var baseName = Store.Theme == ThemePreference.Light ? "Claro" : "Escuro";
        var color = string.IsNullOrEmpty(Store.AccentColor) ? "amarelo padrão" : $"cor {Store.AccentColor}";
        ThemeSummary.Text = $"{baseName} · {color}";
    }

    private void OnCustomize(object sender, RoutedEventArgs e)
    {
        new ThemeDialog { Owner = Window.GetWindow(this) }.ShowDialog();
        UpdateThemeSummary(); // o diálogo já aplicou (ou reverteu, se cancelado)
    }

    private void OnRestoreTheme(object sender, RoutedEventArgs e)
    {
        Store.Theme = ThemePreference.Dark;
        Store.AccentColor = "";
        ThemeService.Apply(Store.Theme, Store.AccentColor);
        Store.Save();
        UpdateThemeSummary();
    }

    private void OnSshDefaultsChanged(object sender, RoutedEventArgs e)
    {
        Store.SshDefaults.Id = DefaultIdBox.Text.Trim();
        Store.SshDefaults.JumpProxy = DefaultJumpBox.Text.Trim();
        Store.Save();
    }

    private void OnAddDns(object sender, RoutedEventArgs e)
    {
        var value = NewDnsBox.Text.Trim();
        if (value.Length == 0)
            return;
        if (!NetworkInfo.TryParseIp(value, out _))
        {
            MessageBox.Show("Informe um endereço IP válido.", "DNS",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!Store.DnsServers.Contains(value))
            Store.DnsServers.Add(value);
        NewDnsBox.Text = "";
        Store.Save();
    }

    private void OnRemoveDns(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string dns)
        {
            Store.DnsServers.Remove(dns);
            Store.Save();
        }
    }
}
