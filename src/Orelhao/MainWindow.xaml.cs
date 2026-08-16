using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Orelhao.Core;
using Orelhao.Views;
using Wpf.Ui.Controls;

namespace Orelhao;

public partial class MainWindow : FluentWindow
{
    // Cada aba (Tag da barra lateral) e a página que abre. Instância nova a cada
    // visita: as páginas releem o estado (contadores, favoritos) sempre atuais.
    private static readonly Dictionary<string, Func<Page>> Pages = new()
    {
        ["Home"] = () => new HomePage(),
        ["Groups"] = () => new GroupsPage(),
        ["Favorites"] = () => new FavoritesPage(),
        ["History"] = () => new HistoryPage(),
        ["Ip"] = () => new IpLookupPage(),
        ["Web"] = () => new WebInspectorPage(),
        ["Settings"] = () => new SettingsPage(),
        ["About"] = () => new AboutPage(),
    };

    private static readonly Dictionary<Type, string> TypeToTag = new()
    {
        [typeof(HomePage)] = "Home",
        [typeof(GroupsPage)] = "Groups",
        [typeof(FavoritesPage)] = "Favorites",
        [typeof(HistoryPage)] = "History",
        [typeof(IpLookupPage)] = "Ip",
        [typeof(WebInspectorPage)] = "Web",
        [typeof(SettingsPage)] = "Settings",
        [typeof(AboutPage)] = "About",
    };

    public MainWindow()
    {
        InitializeComponent();
        SidebarMyIp.Text = NetworkInfo.LocalIp();

        Loaded += (_, _) =>
        {
            // Gancho de teste: ORELHAO_PAGE abre direto naquela aba.
            var tag = Environment.GetEnvironmentVariable("ORELHAO_PAGE");
            if (string.IsNullOrEmpty(tag) || !Pages.ContainsKey(tag))
                tag = "Home";
            CheckNav(tag);

            // Gancho de teste: abre um fluxo direto para reproduzir bugs.
            switch (Environment.GetEnvironmentVariable("ORELHAO_OPEN"))
            {
                case "shortcut":
                    new Views.ShortcutDialog(isNew: true, groups: new[] { "SSH" }) { Owner = this }.ShowDialog();
                    break;
                case "palette":
                    OpenPalette();
                    break;
                case "theme":
                    new Views.ThemeDialog { Owner = this }.ShowDialog();
                    break;
            }
        };
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (ContentFrame is null)
            return; // durante a inicialização do XAML
        if (sender is RadioButton { Tag: string tag } && Pages.TryGetValue(tag, out var factory))
            ContentFrame.Navigate(factory());
    }

    /// <summary>Seleciona a aba pela Tag (dispara a navegação pelo RadioButton).</summary>
    private void CheckNav(string tag)
    {
        foreach (var rb in FindNavButtons())
        {
            if ((rb.Tag as string) == tag)
            {
                if (rb.IsChecked == true)
                    OnNavChecked(rb, new RoutedEventArgs()); // já marcado: navega mesmo assim
                else
                    rb.IsChecked = true;
                return;
            }
        }
    }

    private IEnumerable<RadioButton> FindNavButtons()
    {
        // Percorre a árvore visual atrás dos itens de navegação.
        var stack = new Stack<DependencyObject>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var d = stack.Pop();
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
            for (var i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(d, i);
                if (child is RadioButton { GroupName: "Nav" } rb)
                    yield return rb;
                stack.Push(child);
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenPalette();
            e.Handled = true;
        }
        else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            NavigateTo(typeof(FavoritesPage));
            e.Handled = true;
        }
    }

    /// <summary>Troca de aba por código (ações do Início, atalhos de teclado).</summary>
    public void NavigateTo(Type pageType)
    {
        if (TypeToTag.TryGetValue(pageType, out var tag))
            CheckNav(tag);
    }

    /// <summary>Abre a paleta rápida (Ctrl+K e ação do Início).</summary>
    public void OpenPalette()
    {
        var palette = new CommandPaletteWindow { Owner = this };
        palette.ShowDialog();
    }
}
