using System.Windows;
using System.Windows.Controls;
using Orelhao.Services;
using Orelhao.ViewModels;

namespace Orelhao.Views;

public partial class GroupsPage : Page
{
    private static ShortcutStore Store => App.Store;

    public GroupsPage()
    {
        InitializeComponent();
        DataContext = Store;
        UpdateViews();
    }

    private Window OwnerWindow => Window.GetWindow(this)!;

    // Acha o grupo que contém um atalho (o vínculo real, mais seguro que o nome).
    private GroupViewModel? GroupOf(ShortcutViewModel s)
        => Store.Groups.FirstOrDefault(g => g.Shortcuts.Contains(s));

    private static T? Tagged<T>(object sender) where T : class
        => (sender as FrameworkElement)?.Tag as T;

    // ---- Estado das visões (agrupada x busca x vazio) --------------------

    private bool Searching => !string.IsNullOrWhiteSpace(SearchBox.Text);

    private void OnSearchChanged(object sender, TextChangedEventArgs e) => UpdateViews();

    private void UpdateViews()
    {
        if (Searching)
        {
            var q = SearchBox.Text.Trim();
            var results = Store.Groups
                .SelectMany(g => g.Shortcuts)
                .Where(s =>
                    s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    s.Command.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    s.GroupName.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
            SearchResults.ItemsSource = results;
            SearchResults.Visibility = Visibility.Visible;
            GroupsList.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
        }
        else
        {
            SearchResults.Visibility = Visibility.Collapsed;
            GroupsList.Visibility = Visibility.Visible;
            EmptyState.Visibility = Store.Groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void Save()
    {
        Store.Save();
        UpdateViews();
    }

    // ---- Grupos ----------------------------------------------------------

    private void OnNewGroup(object sender, RoutedEventArgs e)
    {
        var name = InputDialog.Ask(OwnerWindow, "Novo grupo", "Nome do grupo:");
        if (name is null)
            return;
        Store.Groups.Add(new GroupViewModel { Name = name });
        Save();
    }

    private void OnRenameGroup(object sender, RoutedEventArgs e)
    {
        if (Tagged<GroupViewModel>(sender) is not { } group)
            return;
        var name = InputDialog.Ask(OwnerWindow, "Renomear grupo", "Novo nome:", group.Name);
        if (name is null)
            return;
        group.Name = name; // propaga o novo nome de origem aos atalhos
        Save();
    }

    private void OnDeleteGroup(object sender, RoutedEventArgs e)
    {
        if (Tagged<GroupViewModel>(sender) is not { } group)
            return;
        var msg = group.Shortcuts.Count > 0
            ? $"Excluir o grupo \"{group.Name}\" e seus {group.Shortcuts.Count} atalho(s)?"
            : $"Excluir o grupo \"{group.Name}\"?";
        if (MessageBox.Show(msg, "Excluir grupo", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        Store.Groups.Remove(group);
        Save();
    }

    // ---- Atalhos: criar --------------------------------------------------

    private void OnNewShortcut(object sender, RoutedEventArgs e) => CreateShortcut(null);

    private void OnAddToGroup(object sender, RoutedEventArgs e)
        => CreateShortcut(Tagged<GroupViewModel>(sender));

    private void CreateShortcut(GroupViewModel? preferred)
    {
        // Sem grupo nenhum: cria um antes.
        if (Store.Groups.Count == 0)
        {
            var gname = InputDialog.Ask(OwnerWindow, "Novo grupo",
                "Você ainda não tem grupos. Nome do primeiro grupo:");
            if (gname is null)
                return;
            var g = new GroupViewModel { Name = gname };
            Store.Groups.Add(g);
            preferred = g;
            UpdateViews();
        }

        var names = Store.Groups.Select(g => g.Name).ToList();
        var dlg = new ShortcutDialog(isNew: true, groups: names,
            selectedGroup: (preferred ?? Store.Groups[0]).Name) { Owner = OwnerWindow };
        if (dlg.ShowDialog() != true)
            return;

        var target = Store.Groups.FirstOrDefault(g => g.Name == dlg.SelectedGroupName)
                     ?? preferred ?? Store.Groups[0];
        target.Shortcuts.Add(new ShortcutViewModel
        {
            Name = dlg.ShortcutName,
            Command = dlg.ShortcutCommand,
            Icon = dlg.ShortcutIcon,
            GroupName = target.Name,
        });
        Save();
    }

    // ---- Atalhos: ações --------------------------------------------------

    private void OnRun(object sender, RoutedEventArgs e)
    {
        if (Tagged<ShortcutViewModel>(sender) is { } s)
            Store.Run(s);
    }

    private void OnToggleFavorite(object sender, RoutedEventArgs e)
    {
        if (Tagged<ShortcutViewModel>(sender) is { } s)
        {
            s.Favorite = !s.Favorite;
            Store.Save();
        }
    }

    private void OnEdit(object sender, RoutedEventArgs e)
    {
        if (Tagged<ShortcutViewModel>(sender) is not { } s)
            return;
        var dlg = new ShortcutDialog(isNew: false, s.Name, s.Command, s.Icon) { Owner = OwnerWindow };
        if (dlg.ShowDialog() != true)
            return;
        s.Name = dlg.ShortcutName;
        s.Command = dlg.ShortcutCommand;
        s.Icon = dlg.ShortcutIcon;
        Save();
    }

    private void OnDuplicate(object sender, RoutedEventArgs e)
    {
        if (Tagged<ShortcutViewModel>(sender) is not { } s || GroupOf(s) is not { } group)
            return;
        var copy = new ShortcutViewModel
        {
            Name = s.Name + " (cópia)",
            Command = s.Command,
            Icon = s.Icon,
            Favorite = s.Favorite,
            GroupName = group.Name,
        };
        var idx = group.Shortcuts.IndexOf(s);
        group.Shortcuts.Insert(idx + 1, copy);
        Save();
    }

    private void OnMoveUp(object sender, RoutedEventArgs e) => Move(sender, -1);
    private void OnMoveDown(object sender, RoutedEventArgs e) => Move(sender, +1);

    private void Move(object sender, int delta)
    {
        if (Searching)
        {
            MessageBox.Show("Limpe a busca para reordenar — na lista filtrada o vizinho na tela não é o vizinho real.",
                "Reordenar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (Tagged<ShortcutViewModel>(sender) is not { } s || GroupOf(s) is not { } group)
            return;
        var i = group.Shortcuts.IndexOf(s);
        var j = i + delta;
        if (j < 0 || j >= group.Shortcuts.Count)
            return;
        group.Shortcuts.Move(i, j);
        Save();
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (Tagged<ShortcutViewModel>(sender) is not { } s || GroupOf(s) is not { } group)
            return;
        if (MessageBox.Show($"Excluir o atalho \"{s.Name}\"?", "Excluir atalho",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        group.Shortcuts.Remove(s);
        Save();
    }
}
