using System.Collections.ObjectModel;
using Orelhao.Core;
using Orelhao.ViewModels;

namespace Orelhao.Services;

/// <summary>
/// Estado central do app: os grupos/atalhos, DNS, padrões de SSH, histórico e
/// tema, em coleções observáveis. Carrega do shortcuts.json na abertura e grava
/// a cada mudança. Fonte única de verdade que todas as páginas compartilham.
/// </summary>
public sealed class ShortcutStore
{
    public ObservableCollection<GroupViewModel> Groups { get; } = new();
    public ObservableCollection<string> DnsServers { get; } = new();
    public ObservableCollection<HistoryEntry> History { get; } = new();
    public SshDefaults SshDefaults { get; private set; } = new();
    public string Theme { get; set; } = Constants.DefaultTheme;
    public string AccentColor { get; set; } = "";

    // Ordem escolhida dos favoritos no Início (chaves de FavKey).
    public List<string> FavoriteOrder { get; private set; } = new();

    /// <summary>Preenchido na carga se o arquivo estava corrompido (virou .bak).</summary>
    public string? RecoveryMessage { get; private set; }

    public void Load()
    {
        var result = DataStore.Load(AppPaths.DataFile);
        var data = result.Data;
        RecoveryMessage = result.Recovered
            ? $"Não foi possível ler o shortcuts.json. Uma cópia virou .bak e o app iniciou vazio.\n\nDetalhe: {result.Error}"
            : null;

        Groups.Clear();
        foreach (var g in data.Groups)
            Groups.Add(new GroupViewModel(g));

        DnsServers.Clear();
        foreach (var d in data.DnsServers)
            DnsServers.Add(d);

        History.Clear();
        foreach (var h in data.History)
            History.Add(h);

        SshDefaults = data.SshDefaults;
        FavoriteOrder = data.FavoriteOrder;
        Theme = data.Theme;
        AccentColor = data.AccentColor;
    }

    public void Save()
    {
        var data = new AppData
        {
            Groups = Groups.Select(g => g.ToModel()).ToList(),
            DnsServers = DnsServers.ToList(),
            SshDefaults = SshDefaults,
            History = History.Take(Constants.HistoryLimit).ToList(),
            FavoriteOrder = FavoriteOrder,
            Theme = Theme,
            AccentColor = AccentColor,
        };
        DataStore.Save(AppPaths.DataFile, data);
    }

    /// <summary>Todos os atalhos favoritados, de todos os grupos.</summary>
    public IEnumerable<ShortcutViewModel> Favorites =>
        Groups.SelectMany(g => g.Shortcuts).Where(s => s.Favorite);

    // Chave estável de um favorito (grupo + nome) para lembrar a ordem.
    public static string FavKey(ShortcutViewModel s) => $"{s.GroupName}{s.Name}";

    /// <summary>Favoritos na ordem escolhida no Início (os sem ordem vão ao fim).</summary>
    public IEnumerable<ShortcutViewModel> OrderedFavorites()
        => Favorites
            .OrderBy(s => { var i = FavoriteOrder.IndexOf(FavKey(s)); return i < 0 ? int.MaxValue : i; })
            .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase);

    /// <summary>Grava a nova ordem dos favoritos (após arrastar-e-soltar).</summary>
    public void SetFavoriteOrder(IEnumerable<ShortcutViewModel> ordered)
    {
        FavoriteOrder = ordered.Select(FavKey).ToList();
        Save();
    }

    public int ShortcutCount => Groups.Sum(g => g.Shortcuts.Count);

    /// <summary>
    /// Executa um atalho numa janela nova e registra no histórico (no topo,
    /// respeitando o limite). Grava em seguida.
    /// </summary>
    public void Run(ShortcutViewModel shortcut)
        => RunCommand(shortcut.Name, shortcut.Command, shortcut.Icon);

    /// <summary>Executa um comando bruto (nome/ícone só para o histórico).</summary>
    public void RunCommand(string name, string command, string icon)
    {
        CommandRunner.Run(command);
        History.Insert(0, new HistoryEntry
        {
            Name = name,
            Command = command,
            Icon = icon,
            When = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
        });
        while (History.Count > Constants.HistoryLimit)
            History.RemoveAt(History.Count - 1);
        Save();
    }

    public void ClearHistory()
    {
        History.Clear();
        Save();
    }
}
