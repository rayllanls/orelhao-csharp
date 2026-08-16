using Orelhao.Core;
using Xunit;

namespace Orelhao.Tests;

// Persistência: gravar e ler de volta preserva os dados, e um arquivo ilegível
// é recuperado (vira .bak) sem derrubar o app.
public class DataStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public DataStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "orelhao-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "shortcuts.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* limpeza best-effort */ }
    }

    [Fact]
    public void GravarELerPreservaDados()
    {
        var data = new AppData
        {
            Groups =
            {
                new Group { Name = "SSH", Shortcuts =
                {
                    new Shortcut { Name = "Servidor", Command = "ssh a@b@c@d", Icon = "🚀", Favorite = true },
                }},
            },
            DnsServers = new() { "10.0.0.1" },
            SshDefaults = new() { Id = "12345", JumpProxy = "10.0.0.9" },
            History = { new HistoryEntry { Name = "Servidor", Command = "ssh a@b@c@d", Icon = "🚀", When = "18/07/2026 10:00" } },
            Theme = ThemePreference.Dark,
        };

        DataStore.Save(_path, data);
        var loaded = DataStore.Load(_path);

        Assert.False(loaded.Recovered);
        var d = loaded.Data;
        Assert.Equal("SSH", d.Groups[0].Name);
        Assert.Equal("ssh a@b@c@d", d.Groups[0].Shortcuts[0].Command);
        Assert.Equal("🚀", d.Groups[0].Shortcuts[0].Icon);
        Assert.True(d.Groups[0].Shortcuts[0].Favorite);
        // DefaultDns é o fallback; aqui gravamos um DNS próprio e ele deve voltar.
        Assert.Equal(new[] { "10.0.0.1" }, d.DnsServers);
        Assert.Equal("12345", d.SshDefaults.Id);
        Assert.Single(d.History);
        Assert.Equal(ThemePreference.Dark, d.Theme);
    }

    [Fact]
    public void ArquivoIlegivelEhRecuperado()
    {
        File.WriteAllText(_path, "{ isto não é json válido ");
        var loaded = DataStore.Load(_path);

        Assert.True(loaded.Recovered);
        Assert.NotNull(loaded.Error);
        Assert.Empty(loaded.Data.Groups);
        Assert.True(File.Exists(_path + ".bak")); // o arquivo problemático foi preservado
    }

    [Fact]
    public void ArquivoInexistenteViraBancoVazio()
    {
        var loaded = DataStore.Load(_path);
        Assert.False(loaded.Recovered);
        Assert.Empty(loaded.Data.Groups);
        Assert.Equal(Constants.DefaultDns, loaded.Data.DnsServers);
    }
}
