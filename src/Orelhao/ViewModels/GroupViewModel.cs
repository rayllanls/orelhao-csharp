using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Orelhao.Core;

namespace Orelhao.ViewModels;

/// <summary>Um grupo nomeado com sua coleção observável de atalhos.</summary>
public partial class GroupViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";

    public ObservableCollection<ShortcutViewModel> Shortcuts { get; } = new();

    public GroupViewModel() { }

    public GroupViewModel(Group model)
    {
        _name = model.Name;
        foreach (var s in model.Shortcuts)
            Shortcuts.Add(new ShortcutViewModel(s, model.Name));
    }

    // Quando o grupo é renomeado, os atalhos precisam saber o novo nome de
    // origem (Favoritos/Início mostram de onde vêm).
    partial void OnNameChanged(string value)
    {
        foreach (var s in Shortcuts)
            s.GroupName = value;
    }

    public Group ToModel() => new()
    {
        Name = Name,
        Shortcuts = Shortcuts.Select(s => s.ToModel()).ToList(),
    };
}
