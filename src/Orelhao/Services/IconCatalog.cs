using Wpf.Ui.Controls;

namespace Orelhao.Services;

/// <summary>
/// Ícones disponíveis para os atalhos — vetoriais (Fluent), do WPF-UI. Usar o
/// enum <see cref="SymbolRegular"/> direto deixa a lista compile-checked (nome
/// inválido não compila). O atalho guarda o nome do símbolo como texto.
/// </summary>
public static class IconCatalog
{
    public const SymbolRegular Default = SymbolRegular.Desktop24;

    public static readonly SymbolRegular[] Choices =
    {
        SymbolRegular.Desktop24, SymbolRegular.Laptop24, SymbolRegular.Globe24,
        SymbolRegular.Window24, SymbolRegular.Server24, SymbolRegular.Database24,
        SymbolRegular.Cloud24, SymbolRegular.Wifi124, SymbolRegular.Key24,
        SymbolRegular.LockClosed24, SymbolRegular.LockOpen24, SymbolRegular.ShieldTask24,
        SymbolRegular.Fire24, SymbolRegular.Settings24, SymbolRegular.Wrench24,
        SymbolRegular.Router24, SymbolRegular.PlugConnected24, SymbolRegular.DataHistogram24,
        SymbolRegular.DataArea24, SymbolRegular.Board24, SymbolRegular.Box24,
        SymbolRegular.Archive24, SymbolRegular.Folder24, SymbolRegular.Document24,
        SymbolRegular.Search24, SymbolRegular.Bug24, SymbolRegular.Rocket24,
        SymbolRegular.Flash24, SymbolRegular.Link24, SymbolRegular.People24,
        SymbolRegular.Person24, SymbolRegular.Building24, SymbolRegular.Mail24,
        SymbolRegular.Phone24, SymbolRegular.Storage24, SymbolRegular.Code24,
        SymbolRegular.Star24, SymbolRegular.Home24, SymbolRegular.Apps24,
        SymbolRegular.Bookmark24, SymbolRegular.Tag24, SymbolRegular.Clock24,
    };

    /// <summary>Converte o nome guardado num símbolo; desconhecido vira o padrão.</summary>
    public static SymbolRegular Parse(string? name)
        => Enum.TryParse<SymbolRegular>(name, out var symbol) ? symbol : Default;
}
