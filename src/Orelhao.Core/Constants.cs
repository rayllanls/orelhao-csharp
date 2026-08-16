namespace Orelhao.Core;

/// <summary>Constantes de domínio, sem qualquer dependência de UI.</summary>
public static class Constants
{
    public const string AppVersion = "1.1";

    // Ícone padrão de um atalho: o nome de um ícone vetorial (Fluent) do WPF-UI.
    // O WPF não renderiza emoji colorido, então usamos ícones vetoriais, que
    // ainda adotam a cor do tema. A grade de opções (compile-checked) fica na
    // camada WPF (IconCatalog). O Core só guarda/repassa o nome como texto.
    public const string DefaultIcon = "Desktop24";

    // Guarda só as últimas execuções, para o arquivo não crescer sem fim.
    public const int HistoryLimit = 100;

    // Base padrão do app: escuro (com acento amarelo, quando AccentColor vazio).
    public const string DefaultTheme = "Dark";

    // Só o fallback de quando ainda não existe shortcuts.json. Os DNS de
    // verdade ficam no arquivo local (não versionado) e são editáveis na tela.
    public static readonly IReadOnlyList<string> DefaultDns = new[] { "1.1.1.1", "8.8.8.8" };
}

/// <summary>
/// Base do tema (só claro/escuro/sistema). A cor de destaque é separada
/// (<see cref="AppData.AccentColor"/>): vazio = amarelo padrão; um hex ativa o
/// modo "fosforado", em que a cor toma texto, ícones e acento.
/// </summary>
public static class ThemePreference
{
    public const string System = "System";
    public const string Light = "Light";
    public const string Dark = "Dark";

    public static readonly IReadOnlyList<string> All = new[] { System, Light, Dark };

    public static bool IsValid(string? value) => value is System or Light or Dark;
}
