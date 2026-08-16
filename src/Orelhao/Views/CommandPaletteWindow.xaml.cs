using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Orelhao.ViewModels;
using Wpf.Ui.Controls;

namespace Orelhao.Views;

/// <summary>Paleta rápida (Ctrl+K): busca por nome/comando/grupo e lança direto.</summary>
public partial class CommandPaletteWindow : FluentWindow
{
    private readonly List<ShortcutViewModel> _all;

    public CommandPaletteWindow()
    {
        InitializeComponent();
        _all = App.Store.Groups.SelectMany(g => g.Shortcuts).ToList();
        Refresh("");
        Loaded += (_, _) => SearchBox.Focus();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e) => Refresh(SearchBox.Text);

    private void Refresh(string query)
    {
        query = query.Trim();
        IEnumerable<ShortcutViewModel> items = _all;
        if (query.Length > 0)
            items = _all.Where(s =>
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Command.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.GroupName.Contains(query, StringComparison.OrdinalIgnoreCase));

        ResultList.ItemsSource = items.ToList();
        if (ResultList.Items.Count > 0)
            ResultList.SelectedIndex = 0;

        // Sem nenhum atalho cadastrado, a busca não tem o que mostrar.
        EmptyHint.Visibility = _all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                break;
            case Key.Enter:
                RunSelected();
                break;
            case Key.Down when ResultList.Items.Count > 0:
                ResultList.SelectedIndex = Math.Min(ResultList.SelectedIndex + 1, ResultList.Items.Count - 1);
                ResultList.ScrollIntoView(ResultList.SelectedItem);
                e.Handled = true;
                break;
            case Key.Up when ResultList.Items.Count > 0:
                ResultList.SelectedIndex = Math.Max(ResultList.SelectedIndex - 1, 0);
                ResultList.ScrollIntoView(ResultList.SelectedItem);
                e.Handled = true;
                break;
        }
    }

    private void OnRunSelected(object sender, MouseButtonEventArgs e) => RunSelected();

    private void RunSelected()
    {
        if (ResultList.SelectedItem is ShortcutViewModel s)
        {
            App.Store.Run(s);
            Close();
        }
    }
}
