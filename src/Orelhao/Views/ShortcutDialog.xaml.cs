using System.Windows;
using System.Windows.Controls;
using Orelhao.Core;
using Orelhao.Services;
using Wpf.Ui.Controls;

namespace Orelhao.Views;

/// <summary>Diálogo de cadastro/edição de um atalho.</summary>
public partial class ShortcutDialog : FluentWindow
{
    private readonly bool _isNew;
    private bool _loaded;

    public string ShortcutName => NameBox.Text.Trim();
    public string ShortcutCommand => CommandBox.Text.Trim();
    public string ShortcutIcon { get; private set; } = Constants.DefaultIcon;

    /// <summary>Grupo escolhido no combo (só quando <c>groups</c> foi informado).</summary>
    public string? SelectedGroupName => GroupCombo.SelectedItem as string;

    /// <param name="isNew">true para cadastro novo (usa/atualiza os padrões de SSH).</param>
    /// <param name="name">Nome atual, ao editar.</param>
    /// <param name="command">Comando atual, ao editar.</param>
    /// <param name="icon">Ícone atual, ao editar.</param>
    /// <param name="groups">Nomes de grupos para o seletor (mostra o combo). Null = escondido.</param>
    /// <param name="selectedGroup">Grupo pré-selecionado no combo.</param>
    public ShortcutDialog(bool isNew, string name = "", string command = "", string? icon = null,
        IReadOnlyList<string>? groups = null, string? selectedGroup = null)
    {
        InitializeComponent();
        _isNew = isNew;
        Title = isNew ? "Novo atalho" : "Editar atalho";

        if (groups is { Count: > 0 })
        {
            GroupRow.Visibility = System.Windows.Visibility.Visible;
            GroupCombo.ItemsSource = groups;
            GroupCombo.SelectedItem = selectedGroup ?? groups[0];
        }

        IconGrid.ItemsSource = IconCatalog.Choices;
        ShortcutIcon = string.IsNullOrEmpty(icon) ? Constants.DefaultIcon : icon!;
        IconPreview.Symbol = IconCatalog.Parse(ShortcutIcon);
        NameBox.Text = name;
        CommandBox.Text = command;

        // Ao editar, se o comando é um SSH via proxy, preenche o compositor.
        var parts = SshComposer.ParseSshCommand(command);
        if (parts is { } p)
        {
            SshId.Text = p.Id;
            SshAccount.Text = p.Account;
            SshAddress.Text = p.Address;
            SshJump.Text = p.Jump;
        }
        else if (isNew)
        {
            // Cadastro novo: puxa id e proxyjump lembrados do último.
            SshId.Text = App.Store.SshDefaults.Id;
            SshJump.Text = App.Store.SshDefaults.JumpProxy;
        }

        _loaded = true;
        Loaded += (_, _) => NameBox.Focus();
    }

    private void OnPickIcon(object sender, RoutedEventArgs e) => IconPopup.IsOpen = true;

    private void OnIconChosen(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: SymbolRegular symbol })
        {
            ShortcutIcon = symbol.ToString();
            IconPreview.Symbol = symbol;
        }
        IconPopup.IsOpen = false;
    }

    private void OnSshChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loaded)
            return;
        // Preview ao vivo: monta o comando conforme as caixas são preenchidas.
        var cmd = SshComposer.BuildSshCommand(SshId.Text, SshAccount.Text, SshAddress.Text, SshJump.Text);
        if (cmd.Length > 0)
            CommandBox.Text = cmd;
    }

    private void OnApplySsh(object sender, RoutedEventArgs e)
    {
        CommandBox.Text = SshComposer.BuildSshCommand(
            SshId.Text, SshAccount.Text, SshAddress.Text, SshJump.Text);
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (ShortcutName.Length == 0)
        {
            System.Windows.MessageBox.Show("Dê um nome ao atalho.", "Nome obrigatório",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            NameBox.Focus();
            return;
        }
        if (ShortcutCommand.Length == 0)
        {
            System.Windows.MessageBox.Show("Informe o comando do atalho.", "Comando obrigatório",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            CommandBox.Focus();
            return;
        }

        // Cadastro novo: lembra id e proxyjump para o próximo atalho.
        if (_isNew)
        {
            var id = SshId.Text.Trim();
            var jump = SshJump.Text.Trim();
            if (id.Length > 0) App.Store.SshDefaults.Id = id;
            if (jump.Length > 0) App.Store.SshDefaults.JumpProxy = jump;
        }

        DialogResult = true;
    }
}
