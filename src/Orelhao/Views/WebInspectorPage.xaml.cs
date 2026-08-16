using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Orelhao.Core;

namespace Orelhao.Views;

public partial class WebInspectorPage : Page
{
    private record Row(string Label, string Value);

    public WebInspectorPage()
    {
        InitializeComponent();
        Loaded += (_, _) => TargetBox.Focus();
    }

    private void OnKeyDownTarget(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnInspect(sender, e);
    }

    private List<KeyValuePair<string, string>> _headers = new();
    private string _headersTarget = "";

    private async void OnInspect(object sender, RoutedEventArgs e)
    {
        var target = TargetBox.Text.Trim();
        if (target.Length == 0) return;

        SetLoading(true);
        ErrorBar.IsOpen = false;
        CertCard.Visibility = Visibility.Collapsed;
        HeadersCard.Visibility = Visibility.Collapsed;

        // Roda as duas inspeções em paralelo; cada uma pode falhar sozinha.
        var certTask = SafeCertAsync(target);
        var headersTask = SafeHeadersAsync(target);
        await Task.WhenAll(certTask, headersTask);

        var errors = new List<string>();
        if (certTask.Result is { } cert) ShowCert(cert); else errors.Add($"Certificado: {certTask.Exception?.InnerException?.Message ?? _certError}");
        if (headersTask.Result is { } headers) ShowHeaders(headers); else errors.Add($"Cabeçalhos: {_headersError}");

        if (errors.Count > 0 && (certTask.Result is null || headersTask.Result is null))
            ShowError(string.Join("\n", errors));

        SetLoading(false);
    }

    private string? _certError;
    private string? _headersError;

    private async Task<CertInfo?> SafeCertAsync(string target)
    {
        try { return await WebInspector.CertificateAsync(target); }
        catch (Exception ex) { _certError = ex.Message; return null; }
    }

    private async Task<HttpHeaderInfo?> SafeHeadersAsync(string target)
    {
        try { return await WebInspector.HeadersAsync(target); }
        catch (Exception ex) { _headersError = ex.Message; return null; }
    }

    private void ShowCert(CertInfo cert)
    {
        var rows = new List<Row>
        {
            new("Assunto (CN)", ShortName(cert.Subject)),
            new("Emissor", ShortName(cert.Issuer)),
            new("Válido de", cert.NotBefore.ToString("dd/MM/yyyy")),
            new("Válido até", cert.NotAfter.ToString("dd/MM/yyyy")),
            new("Dias restantes", cert.DaysRemaining.ToString()),
        };
        if (cert.SubjectAltNames.Count > 0)
            rows.Add(new Row("Nomes (SAN)", string.Join("\n", cert.SubjectAltNames)));
        CertList.ItemsSource = rows;

        // Selo de situação: expirado / expira em breve / confiável / não confiável.
        string text; Color color;
        if (cert.DaysRemaining < 0) { text = "EXPIRADO"; color = FromHex("#E5484D"); }
        else if (cert.DaysRemaining <= 15) { text = $"EXPIRA EM {cert.DaysRemaining}d"; color = FromHex("#F5A524"); }
        else if (!cert.Trusted) { text = "NÃO CONFIÁVEL"; color = FromHex("#F5A524"); }
        else { text = "OK"; color = FromHex("#30A46C"); }
        CertBadgeText.Text = text;
        CertBadge.Background = new SolidColorBrush(color);
        CertBadgeText.Foreground = Brushes.White;

        CertCard.Visibility = Visibility.Visible;
    }

    private void ShowHeaders(HttpHeaderInfo info)
    {
        _headersTarget = info.FinalUrl;
        HeadersTitle.Text = $"Cabeçalhos HTTP · {info.StatusCode} {info.StatusText}";
        _headers = info.Headers.ToList();
        HeadersList.ItemsSource = info.Headers.Select(h => new Row(h.Key, h.Value)).ToList();
        HeadersCard.Visibility = Visibility.Visible;
    }

    private void OnCopyHeaders(object sender, RoutedEventArgs e)
    {
        var text = _headersTarget + "\n" + string.Join("\n", _headers.Select(h => $"{h.Key}: {h.Value}"));
        try { Clipboard.SetText(text); } catch { }
    }

    // "CN=exemplo.com, O=Empresa, ..." → só o CN, se houver.
    private static string ShortName(string dn)
    {
        foreach (var part in dn.Split(','))
        {
            var p = part.Trim();
            if (p.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                return p[3..];
        }
        return dn;
    }

    private void SetLoading(bool on)
    {
        LoadingPanel.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        InspectButton.IsEnabled = !on;
    }

    private void ShowError(string message)
    {
        ErrorBar.Message = message;
        ErrorBar.IsOpen = true;
    }

    private static Color FromHex(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;
}
