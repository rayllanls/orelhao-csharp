using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Orelhao.Core;

namespace Orelhao.Views;

public partial class IpLookupPage : Page
{
    private record ResultRow(string Label, string Value);
    private record ServerVm(string Server, IReadOnlyList<string> Lines);

    public IpLookupPage()
    {
        InitializeComponent();
        MyIpText.Text = NetworkInfo.LocalIp();

        var kinds = Enum.GetValues<DnsRecordKind>();
        DnsTypeCombo.ItemsSource = kinds;
        DnsTypeCombo.SelectedItem = DnsRecordKind.A;
        PropTypeCombo.ItemsSource = kinds;
        PropTypeCombo.SelectedItem = DnsRecordKind.A;

        Loaded += (_, _) => QueryBox.Focus();
    }

    private List<string> Dns => App.Store.DnsServers.ToList();

    private void OnCopyMyIp(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(MyIpText.Text); } catch { }
    }

    private void SetLoading(bool on, params System.Windows.Controls.Control[] buttons)
    {
        LoadingPanel.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        foreach (var b in buttons)
            b.IsEnabled = !on;
    }

    private void ShowError(string message)
    {
        ErrorBar.Message = message;
        ErrorBar.IsOpen = true;
    }

    // ---------- Aba IP ----------

    private void OnQueryKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnQuery(sender, e);
    }

    private async void OnQuery(object sender, RoutedEventArgs e)
    {
        var text = QueryBox.Text.Trim();
        if (text.Length == 0) return;

        SetLoading(true, QueryButton);
        ErrorBar.IsOpen = false;
        ResultCard.Visibility = Visibility.Collapsed;
        try
        {
            if (NetworkInfo.TryParseIp(text, out var ip))
            {
                if (NetworkInfo.IsPrivate(ip)) await ShowPrivateAsync(text);
                else await ShowPublicAsync(text);
            }
            else
            {
                await ShowHostnameAsync(text);
            }
        }
        catch (Exception ex) { ShowError($"Não foi possível consultar \"{text}\".\n{ex.Message}"); }
        finally { SetLoading(false, QueryButton); }
    }

    private async Task ShowPrivateAsync(string ip)
    {
        var names = await DnsService.ReverseAsync(ip, Dns);
        var rows = new List<ResultRow> { new("IP", ip), new("Tipo", "Privado (rede interna)") };
        rows.Add(new ResultRow("Nome (PTR)", names.Count > 0 ? string.Join("\n", names) : "— não encontrado —"));
        ShowResult($"Resultado de {ip}", rows);
    }

    private async Task ShowPublicAsync(string ip)
    {
        var info = await IpLookup.QueryAsync(ip);
        var rows = new List<ResultRow> { new("IP", ip), new("Tipo", "Público (internet)") };
        if (info is not null)
        {
            AddIf(rows, "Hostname", info.Hostname);
            AddIf(rows, "Organização", info.Org);
            AddIf(rows, "Cidade", info.City);
            AddIf(rows, "Região", info.Region);
            AddIf(rows, "País", info.Country);
        }
        ShowResult($"Resultado de {ip}", rows);
    }

    private async Task ShowHostnameAsync(string hostname)
    {
        var ips = await DnsService.ForwardAsync(hostname, Dns);
        var rows = new List<ResultRow> { new("Hostname", hostname) };
        rows.Add(new ResultRow("IP(s)", ips.Count > 0 ? string.Join("\n", ips) : "— não encontrado —"));
        ShowResult($"Resultado de {hostname}", rows);
    }

    private static void AddIf(List<ResultRow> rows, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) rows.Add(new ResultRow(label, value!));
    }

    private string _ipTitle = "";
    private List<ResultRow> _ipRows = new();

    private void ShowResult(string title, List<ResultRow> rows)
    {
        _ipTitle = title;
        _ipRows = rows;
        ResultTitle.Text = title;
        ResultList.ItemsSource = rows;
        ResultCard.Visibility = Visibility.Visible;
    }

    private void OnCopyResult(object sender, RoutedEventArgs e)
    {
        var lines = _ipRows.Select(r => $"{r.Label}: {r.Value.Replace("\n", ", ")}");
        Copy(_ipTitle + "\n" + string.Join("\n", lines));
    }

    // ---------- Aba Registros DNS ----------

    private void OnDnsKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnDnsQuery(sender, e);
    }

    private string _dnsTitle = "";
    private IReadOnlyList<string> _dnsRecords = Array.Empty<string>();

    private async void OnDnsQuery(object sender, RoutedEventArgs e)
    {
        var name = DnsNameBox.Text.Trim();
        if (name.Length == 0 || DnsTypeCombo.SelectedItem is not DnsRecordKind kind) return;

        SetLoading(true, DnsButton);
        ErrorBar.IsOpen = false;
        DnsCard.Visibility = Visibility.Collapsed;
        try
        {
            var records = await DnsService.ResolveAsync(name, kind, Dns);
            _dnsTitle = $"{kind} de {name}";
            _dnsRecords = records;
            DnsResultTitle.Text = _dnsTitle;
            DnsResults.ItemsSource = records;
            DnsEmpty.Visibility = records.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            DnsCard.Visibility = Visibility.Visible;
        }
        catch (Exception ex) { ShowError($"Falha ao consultar {kind} de \"{name}\".\n{ex.Message}"); }
        finally { SetLoading(false, DnsButton); }
    }

    private void OnCopyDns(object sender, RoutedEventArgs e)
        => Copy(_dnsTitle + "\n" + string.Join("\n", _dnsRecords));

    // ---------- Aba Propagação ----------

    private void OnPropKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnPropQuery(sender, e);
    }

    private async void OnPropQuery(object sender, RoutedEventArgs e)
    {
        var name = PropNameBox.Text.Trim();
        if (name.Length == 0 || PropTypeCombo.SelectedItem is not DnsRecordKind kind) return;
        if (Dns.Count == 0)
        {
            ShowError("Nenhum servidor DNS configurado (adicione em Configurações).");
            return;
        }

        SetLoading(true, PropButton);
        ErrorBar.IsOpen = false;
        PropResults.ItemsSource = null;
        try
        {
            var results = await DnsService.PropagationAsync(name, kind, Dns);
            PropResults.ItemsSource = results.Select(r => new ServerVm(
                r.Server,
                r.Error is not null ? new[] { $"erro: {r.Error}" }
                    : r.Records.Count > 0 ? r.Records.ToArray()
                    : new[] { "— sem resposta —" })).ToList();
        }
        catch (Exception ex) { ShowError($"Falha na checagem de propagação.\n{ex.Message}"); }
        finally { SetLoading(false, PropButton); }
    }

    private static void Copy(string text)
    {
        try { Clipboard.SetText(text); } catch { }
    }
}
