using CommunityToolkit.Mvvm.ComponentModel;
using Orelhao.Core;

namespace Orelhao.ViewModels;

/// <summary>
/// Envolve um <see cref="Shortcut"/> do Core com notificação de mudança, para
/// a UI reagir a renomear/favoritar sem redesenhar tudo na mão.
/// </summary>
public partial class ShortcutViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _command = "";
    [ObservableProperty] private string _icon = Constants.DefaultIcon;
    [ObservableProperty] private bool _favorite;

    // Grupo dono deste atalho — usado pelos Favoritos/Início para mostrar de
    // onde ele vem. Não é serializado (o vínculo já está na estrutura).
    [ObservableProperty] private string _groupName = "";

    public ShortcutViewModel() { }

    public ShortcutViewModel(Shortcut model, string groupName)
    {
        _name = model.Name;
        _command = model.Command;
        _icon = string.IsNullOrEmpty(model.Icon) ? Constants.DefaultIcon : model.Icon;
        _favorite = model.Favorite;
        _groupName = groupName;
    }

    public Shortcut ToModel() => new()
    {
        Name = Name,
        Command = Command,
        Icon = string.IsNullOrEmpty(Icon) ? Constants.DefaultIcon : Icon,
        Favorite = Favorite,
    };
}
