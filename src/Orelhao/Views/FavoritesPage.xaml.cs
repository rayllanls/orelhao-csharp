using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Orelhao.Services;
using Orelhao.ViewModels;

namespace Orelhao.Views;

public partial class FavoritesPage : Page
{
    private static ShortcutStore Store => App.Store;
    private readonly ObservableCollection<ShortcutViewModel> _favorites = new();

    public FavoritesPage()
    {
        InitializeComponent();
        FavoritesList.ItemsSource = _favorites;
        Reload();
    }

    private void Reload()
    {
        _favorites.Clear();
        foreach (var s in Store.Favorites)
            _favorites.Add(s);
        EmptyState.Visibility = _favorites.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static T? Tagged<T>(object sender) where T : class
        => (sender as FrameworkElement)?.Tag as T;

    private void OnRun(object sender, RoutedEventArgs e)
    {
        if (Tagged<ShortcutViewModel>(sender) is { } s)
            Store.Run(s);
    }

    private void OnUnfavorite(object sender, RoutedEventArgs e)
    {
        if (Tagged<ShortcutViewModel>(sender) is { } s)
        {
            s.Favorite = false;
            _favorites.Remove(s);
            Store.Save();
            EmptyState.Visibility = _favorites.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
