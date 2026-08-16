using System.Text.Json.Nodes;
using Orelhao.Core;
using Xunit;

namespace Orelhao.Tests;

// A normalização precisa aguentar um JSON estragado sem derrubar o app:
// completa campo ausente e descarta o malformado.
public class NormalizeTests
{
    // Atalho: normaliza a partir de um literal JSON, como o arquivo em disco.
    private static AppData N(string json) => DataStore.Normalize(JsonNode.Parse(json));

    [Fact]
    public void DadosValidosPassam()
    {
        var d = N("""
            {"groups": [{"name": "SSH", "shortcuts": [{"name": "A", "command": "ssh x"}]}],
             "dns_servers": ["10.0.0.1"],
             "ssh_defaults": {"id": "12345", "jump_proxy": "10.0.0.9"}}
            """);
        Assert.Single(d.Groups[0].Shortcuts);
        Assert.Equal(new[] { "10.0.0.1" }, d.DnsServers);
        Assert.Equal("12345", d.SshDefaults.Id);
        Assert.Equal("10.0.0.9", d.SshDefaults.JumpProxy);
    }

    [Fact]
    public void NaoObjetoViraBancoVazio()
    {
        var d = N("""["lixo"]""");
        Assert.Empty(d.Groups);
        Assert.Equal(Constants.DefaultDns, d.DnsServers);
    }

    [Fact] // atalho sem command não entra — senão o app quebra ao executar
    public void AtalhoMalformadoEhDescartado()
    {
        var d = N("""
            {"groups": [{"name": "G", "shortcuts": [
                {"name": "bom", "command": "ssh x"},
                {"name": "sem comando"},
                {"command": "sem nome"},
                "nem objeto"
            ]}]}
            """);
        Assert.Equal(new[] { "bom" }, d.Groups[0].Shortcuts.Select(s => s.Name));
    }

    [Fact]
    public void GrupoSemNomeEhDescartado()
    {
        var d = N("""{"groups": [{"shortcuts": []}, {"name": "ok", "shortcuts": []}]}""");
        Assert.Equal(new[] { "ok" }, d.Groups.Select(g => g.Name));
    }

    [Fact]
    public void DnsInvalidoCaiNoPadrao()
    {
        Assert.Equal(Constants.DefaultDns, N("""{"dns_servers": [1, 2]}""").DnsServers);
        Assert.Equal(Constants.DefaultDns, N("""{"dns_servers": "10.0.0.1"}""").DnsServers);
    }

    [Fact]
    public void SshDefaultsAusenteOuLixo()
    {
        Assert.Equal("", N("{}").SshDefaults.Id);
        Assert.Equal("", N("{}").SshDefaults.JumpProxy);
        Assert.Equal("", N("""{"ssh_defaults": "lixo"}""").SshDefaults.Id);
        Assert.Equal("", N("""{"ssh_defaults": {"id": 42}}""").SshDefaults.Id);
    }

    [Fact]
    public void SshDefaultsParcial()
    {
        var d = N("""{"ssh_defaults": {"id": "12345"}}""");
        Assert.Equal("12345", d.SshDefaults.Id);
        Assert.Equal("", d.SshDefaults.JumpProxy);
    }

    [Fact] // chave desconhecida não vira campo — só é ignorada
    public void ChaveDesconhecidaEhIgnorada()
    {
        var d = N("""{"groups": [], "coisa_estranha": 123}""");
        Assert.Empty(d.Groups);
    }

    [Fact]
    public void HistoryValidaELimita()
    {
        var d = N("""
            {"groups": [], "history": [
                {"name": "A", "command": "x", "when": "01/01 10:00"},
                {"name": "B"},
                "lixo",
                {"name": "C", "command": "y", "when": "02/01 11:00", "icon": "Z"}
            ]}
            """);
        Assert.Equal(new[] { "A", "C" }, d.History.Select(h => h.Name));
        Assert.Equal(Constants.DefaultIcon, d.History[0].Icon); // A sem icon
        Assert.Equal("Z", d.History[1].Icon);

        var big = "[" + string.Join(",",
            Enumerable.Range(0, Constants.HistoryLimit + 50)
                .Select(i => $$"""{"name": "{{i}}", "command": "x", "when": "t"}""")) + "]";
        Assert.Equal(Constants.HistoryLimit, N($$"""{"history": {{big}}}""").History.Count);
    }

    [Fact]
    public void HistoryAusenteViraListaVazia()
        => Assert.Empty(N("{}").History);

    [Fact]
    public void IconePreservadoOuPadrao()
    {
        var d = N("""
            {"groups": [{"name": "G", "shortcuts": [
                {"name": "com", "command": "x", "icon": "ABC"},
                {"name": "sem", "command": "x"},
                {"name": "vazio", "command": "x", "icon": ""},
                {"name": "lixo", "command": "x", "icon": 42}
            ]}]}
            """);
        Assert.Equal(
            new[] { "ABC", Constants.DefaultIcon, Constants.DefaultIcon, Constants.DefaultIcon },
            d.Groups[0].Shortcuts.Select(s => s.Icon));
    }

    [Fact]
    public void FavoritoNormalizadoParaBool()
    {
        var d = N("""
            {"groups": [{"name": "G", "shortcuts": [
                {"name": "a", "command": "x", "favorite": true},
                {"name": "b", "command": "x"},
                {"name": "c", "command": "x", "favorite": "sim"},
                {"name": "d", "command": "x", "favorite": 0}
            ]}]}
            """);
        Assert.Equal(new[] { true, false, true, false },
            d.Groups[0].Shortcuts.Select(s => s.Favorite));
    }
}
