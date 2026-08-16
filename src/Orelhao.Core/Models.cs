namespace Orelhao.Core;

// Modelos de domínio. Classes simples (POCO) e mutáveis: servem tanto para
// serializar no shortcuts.json quanto para a UI ligar (binding) direto.
// A camada WPF envolve o que precisa notificar mudança.

/// <summary>Um atalho de terminal: nome, comando e um emoji de ícone.</summary>
public sealed class Shortcut
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Icon { get; set; } = Constants.DefaultIcon;
    public bool Favorite { get; set; }

    public Shortcut Clone() => new()
    {
        Name = Name, Command = Command, Icon = Icon, Favorite = Favorite,
    };
}

/// <summary>Um grupo nomeado de atalhos.</summary>
public sealed class Group
{
    public string Name { get; set; } = "";
    public List<Shortcut> Shortcuts { get; set; } = new();
}

/// <summary>
/// Do compositor SSH, só o ID do cofre e o jump proxy se repetem entre
/// cadastros — são os únicos que vale lembrar como padrão do próximo.
/// </summary>
public sealed class SshDefaults
{
    public string Id { get; set; } = "";
    public string JumpProxy { get; set; } = "";
}

/// <summary>Uma execução registrada no histórico.</summary>
public sealed class HistoryEntry
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Icon { get; set; } = Constants.DefaultIcon;
    public string When { get; set; } = "";
}

/// <summary>Raiz dos dados persistidos (shortcuts.json).</summary>
public sealed class AppData
{
    public List<Group> Groups { get; set; } = new();
    public List<string> DnsServers { get; set; } = new(Constants.DefaultDns);
    public SshDefaults SshDefaults { get; set; } = new();
    public List<HistoryEntry> History { get; set; } = new();

    // Ordem escolhida dos favoritos no painel de Início (chaves "gruponome").
    // Favoritos fora desta lista vão para o fim.
    public List<string> FavoriteOrder { get; set; } = new();

    // Base do tema: "System" segue o Windows; "Light"/"Dark" fixam.
    public string Theme { get; set; } = Constants.DefaultTheme;

    // Cor de destaque personalizada (hex "#RRGGBB"). Vazio = amarelo padrão.
    // Com uma cor definida, o app entra no modo fosforado (cor em tudo).
    public string AccentColor { get; set; } = "";
}
