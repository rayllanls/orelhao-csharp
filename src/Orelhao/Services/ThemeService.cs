using System.Windows;
using System.Windows.Media;
using Orelhao.Core;
using Wpf.Ui.Appearance;

namespace Orelhao.Services;

/// <summary>
/// Aplica o tema: base (claro/escuro) + cor de destaque. A cor pinta só os
/// **ícones e os elementos de destaque** (botões, números, foco); os textos e
/// títulos seguem a base (brancos no escuro, pretos no claro) para não poluir.
/// Sem cor personalizada, o destaque é o amarelo da marca.
/// </summary>
public static class ThemeService
{
    private static readonly Color Yellow = FromHex("#FDBF2D");
    private static readonly Color OnLight = FromHex("#10141B"); // texto sobre acento claro

    public static Color Accent { get; private set; } = Yellow;

    // Última base aplicada. Reaplicar a base (ApplicationThemeManager.Apply)
    // à toa reseta o acento do WPF-UI e deixa os botões Primary com uma cor e
    // os ícones com outra — por isso só reaplica quando a base muda de verdade.
    private static ApplicationTheme? _appliedTheme;

    /// <param name="baseTheme">Base: System/Light/Dark (de ThemePreference).</param>
    /// <param name="accentHex">Cor personalizada ("#RRGGBB"); vazio = amarelo padrão.</param>
    public static void Apply(string baseTheme, string? accentHex)
    {
        var theme = baseTheme switch
        {
            ThemePreference.Light => ApplicationTheme.Light,
            ThemePreference.Dark => ApplicationTheme.Dark,
            _ => SystemThemeIsLight() ? ApplicationTheme.Light : ApplicationTheme.Dark,
        };

        var accent = TryParse(accentHex, out var custom) ? custom : Yellow;
        Accent = accent;

        // Só troca a base quando muda (senão o acento do WPF-UI sai de sincronia).
        if (_appliedTheme != theme)
        {
            ApplicationThemeManager.Apply(theme, updateAccent: false);
            _appliedTheme = theme;
        }
        ApplicationAccentColorManager.Apply(accent, theme);

        // Sobrescreve também os brushes de acento do WPF-UI (os que os botões
        // Primary e o foco usam). O ApplicationAccentColorManager sozinho não
        // atualiza esses de forma confiável em runtime — daí os botões ficarem
        // com uma cor e os ícones com outra. Aqui garantimos que tudo bate.
        var onAccent = OnAccent(accent);
        var res = Application.Current.Resources;
        res["OrelhaoAccentColor"] = accent;
        res["OrelhaoAccentBrush"] = new SolidColorBrush(accent);
        res["OrelhaoOnAccentColor"] = onAccent;
        res["OrelhaoOnAccentBrush"] = new SolidColorBrush(onAccent);

        res["AccentFillColorDefaultBrush"] = new SolidColorBrush(accent);
        res["AccentFillColorSecondaryBrush"] = new SolidColorBrush(Scale(accent, 0.90)); // hover
        res["AccentFillColorTertiaryBrush"] = new SolidColorBrush(Scale(accent, 0.82));  // pressed
        res["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(onAccent);
        res["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(onAccent);
        res["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(onAccent);
    }

    // Texto/ícone que fica legível sobre o acento: escuro em acento claro,
    // branco em acento escuro (luminância perceptual).
    private static Color OnAccent(Color c)
    {
        var luma = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
        return luma > 140 ? OnLight : Colors.White;
    }

    // Escurece o acento (hover/pressed dos botões).
    private static Color Scale(Color c, double f)
        => Color.FromRgb((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f));

    private static bool SystemThemeIsLight()
        => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Light;

    private static bool TryParse(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
            return false;
        try { color = (Color)ColorConverter.ConvertFromString(hex)!; return true; }
        catch { return false; }
    }

    private static Color FromHex(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;
}
