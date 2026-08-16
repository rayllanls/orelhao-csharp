using System.Windows;
using System.Windows.Controls;
using Orelhao.Core;
using Orelhao.Services;

namespace Orelhao.Views;

public partial class HistoryPage : Page
{
    private static ShortcutStore Store => App.Store;

    public HistoryPage()
    {
        InitializeComponent();
        DataContext = Store;
    }

    private void OnRerun(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is HistoryEntry entry)
            // Reexecuta pelo comando gravado; registra a nova execução no topo.
            Store.RunCommand(entry.Name, entry.Command, entry.Icon);
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        if (Store.History.Count == 0)
            return;
        if (MessageBox.Show("Limpar todo o histórico de execuções?", "Histórico",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            Store.ClearHistory();
    }
}
