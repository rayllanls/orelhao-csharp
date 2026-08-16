using System.Diagnostics;
using System.Windows.Controls;
using Orelhao.Core;

namespace Orelhao.Views;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = $"Versão {Constants.AppVersion}";
    }

    private void OnGithubClick(object sender, System.Windows.RoutedEventArgs e)
        => OpenUrl("https://github.com/rayllanls/orelhao");

    private void OnAuthorClick(object sender, System.Windows.RoutedEventArgs e)
        => OpenUrl("https://github.com/rayllanls");

    private static void OpenUrl(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
