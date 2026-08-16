using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Orelhao.Core;
using Orelhao.Services;
using Wpf.Ui.Controls;

namespace Orelhao.Views;

/// <summary>
/// Personalização do tema: base (claro/escuro) + cor de destaque, com preview
/// ao vivo. "Concluir" salva; "Cancelar"/X reverte para o estado anterior.
/// </summary>
public partial class ThemeDialog : FluentWindow
{
    private record Swatch(string Hex, SolidColorBrush Brush);

    private static ShortcutStore Store => App.Store;

    // Estado anterior, para reverter no Cancelar.
    private readonly string _origBase;
    private readonly string _origAccent;

    private string _base;
    private string _accent; // "" = padrão (amarelo)
    private bool _loaded;

    public ThemeDialog()
    {
        InitializeComponent();

        _origBase = _base = Store.Theme is ThemePreference.Light ? ThemePreference.Light : ThemePreference.Dark;
        _origAccent = _accent = Store.AccentColor;

        Swatches.ItemsSource = BuildPalette();
        BaseDark.IsChecked = _base == ThemePreference.Dark;
        BaseLight.IsChecked = _base == ThemePreference.Light;
        HexBox.Text = _accent;
        UpdateCurrentSwatch();

        _loaded = true;
    }

    // Paleta: matiz variando, em três luminosidades, + tons de cinza/branco.
    private static List<Swatch> BuildPalette()
    {
        var list = new List<Swatch>();
        double[] lums = { 0.72, 0.58, 0.44 };
        foreach (var l in lums)
            for (int i = 0; i < 12; i++)
                list.Add(MakeHsl(i / 12.0, 0.85, l));
        // Neutros: preto → cinzas → branco, para quem quiser algo sóbrio.
        foreach (var l in new[] { 0.0, 0.30, 0.50, 0.70, 0.92 })
            list.Add(MakeHsl(0, 0, l));
        return list;
    }

    private static Swatch MakeHsl(double h, double s, double l)
    {
        var c = HslColor(h, s, l);
        var hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        return new Swatch(hex, new SolidColorBrush(c));
    }

    private void OnBaseChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded)
            return;
        _base = BaseLight.IsChecked == true ? ThemePreference.Light : ThemePreference.Dark;
        Preview();
    }

    private void OnSwatchClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string hex)
        {
            _accent = hex;
            HexBox.Text = hex;
            Preview();
        }
    }

    private void OnHexChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loaded)
            return;
        var value = HexBox.Text.Trim();
        // Aplica ao vivo só quando o hex está completo e válido; incompleto/
        // inválido é ignorado (sem erro a cada tecla). Vazio fica com a última
        // cor (ou use "Restaurar padrão" para voltar ao amarelo).
        if (Regex.IsMatch(value, "^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$"))
        {
            _accent = value;
            Preview();
        }
    }

    private void OnRestoreDefault(object sender, RoutedEventArgs e)
    {
        _base = ThemePreference.Dark;
        _accent = "";
        BaseDark.IsChecked = true;
        HexBox.Text = "";
        Preview();
    }

    private void Preview()
    {
        ThemeService.Apply(_base, _accent);
        UpdateCurrentSwatch();
    }

    private void UpdateCurrentSwatch()
        => CurrentSwatch.Background = new SolidColorBrush(ThemeService.Accent);

    private void OnOk(object sender, RoutedEventArgs e)
    {
        // Aplicação final limpa (garante consistência mesmo sem preview) + salva.
        ThemeService.Apply(_base, _accent);
        Store.Theme = _base;
        Store.AccentColor = _accent;
        Store.Save();
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        ThemeService.Apply(_origBase, _origAccent); // reverte o preview
        DialogResult = false;
    }

    // HSL→Color (mesma matemática do ThemeService, para as amostras).
    private static Color HslColor(double h, double s, double l)
    {
        double r, g, b;
        if (s == 0) { r = g = b = l; }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            r = Hue(p, q, h + 1.0 / 3); g = Hue(p, q, h); b = Hue(p, q, h - 1.0 / 3);
        }
        return Color.FromRgb((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
    }

    private static double Hue(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
}
