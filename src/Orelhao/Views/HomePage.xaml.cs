using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Orelhao.Services;
using Orelhao.ViewModels;

namespace Orelhao.Views;

public partial class HomePage : Page
{
    private static ShortcutStore Store => App.Store;
    private readonly ObservableCollection<ShortcutViewModel> _favorites = new();

    // Estado do arrastar-e-soltar dos favoritos.
    private Point _dragStart;
    private bool _dragging;

    public HomePage()
    {
        InitializeComponent();
        FavoritesList.ItemsSource = _favorites;
        Render();
    }

    private void Render()
    {
        GroupCount.Text = Store.Groups.Count.ToString();
        ShortcutCount.Text = Store.ShortcutCount.ToString();

        _favorites.Clear();
        foreach (var s in Store.OrderedFavorites())
            _favorites.Add(s);

        FavoriteCount.Text = _favorites.Count.ToString();
        NoFavorites.Visibility = _favorites.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        DragHint.Visibility = _favorites.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private MainWindow? Main => Window.GetWindow(this) as MainWindow;

    private void OnSearch(object sender, RoutedEventArgs e) => Main?.OpenPalette();

    private void OnNewShortcut(object sender, RoutedEventArgs e) => Main?.NavigateTo(typeof(GroupsPage));

    private void OnConsultarIp(object sender, RoutedEventArgs e) => Main?.NavigateTo(typeof(IpLookupPage));

    private void OnFavClick(object sender, MouseButtonEventArgs e)
    {
        // Se acabou de arrastar, não dispara a execução.
        if (_dragging)
        {
            _dragging = false;
            return;
        }
        if ((sender as FrameworkElement)?.Tag is ShortcutViewModel s)
            Store.Run(s);
    }

    // ---- Arrastar-e-soltar para reordenar ----

    private void OnFavMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _dragging = false;
    }

    private void OnFavMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;
        var diff = _dragStart - e.GetPosition(null);
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if ((sender as FrameworkElement)?.DataContext is ShortcutViewModel item)
        {
            _dragging = true;
            DragDrop.DoDragDrop((DependencyObject)sender, item, DragDropEffects.Move);
        }
    }

    private void OnFavDragOver(object sender, DragEventArgs e)
    {
        // Sem aceitar explicitamente, o WPF marca o drop como inválido e o
        // evento Drop nunca dispara.
        e.Effects = e.Data.GetDataPresent(typeof(ShortcutViewModel))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFavDrop(object sender, DragEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ShortcutViewModel target)
            return;
        if (e.Data.GetData(typeof(ShortcutViewModel)) is not ShortcutViewModel dragged || dragged == target)
            return;

        var from = _favorites.IndexOf(dragged);
        var to = _favorites.IndexOf(target);
        if (from < 0 || to < 0)
            return;

        _favorites.Move(from, to);
        Store.SetFavoriteOrder(_favorites);
    }
}
