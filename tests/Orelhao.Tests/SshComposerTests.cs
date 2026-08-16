using Orelhao.Core;
using Xunit;

namespace Orelhao.Tests;

// Monta/lê o comando SSH via proxy — as funções que decidem em que servidor
// se entra. Dados fictícios de propósito: este arquivo é versionado.
public class BuildSshCommandTests
{
    [Fact]
    public void Completo()
        => Assert.Equal("ssh 12345@operador@10.0.0.50@10.0.0.9",
            SshComposer.BuildSshCommand("12345", "operador", "10.0.0.50", "10.0.0.9"));

    [Fact] // campo em branco some da montagem, sem deixar '@@' pelo caminho
    public void CampoVazioEOmitido()
        => Assert.Equal("ssh 12345@10.0.0.50@10.0.0.9",
            SshComposer.BuildSshCommand("12345", "", "10.0.0.50", "10.0.0.9"));

    [Fact]
    public void SoOServidor()
        => Assert.Equal("ssh 10.0.0.50", SshComposer.BuildSshCommand("", "", "10.0.0.50", ""));

    [Fact]
    public void SemJump()
        => Assert.Equal("ssh 12345@operador@10.0.0.50",
            SshComposer.BuildSshCommand("12345", "operador", "10.0.0.50", ""));

    [Fact] // sem nenhuma parte, o comando é vazio — não um 'ssh ' inútil
    public void TudoVazioNaoGeraSshSolto()
        => Assert.Equal("", SshComposer.BuildSshCommand("", "", "", ""));

    [Fact]
    public void EspacosSaoAparados()
        => Assert.Equal("ssh 12345@operador@srv.exemplo.local@10.0.0.9",
            SshComposer.BuildSshCommand("  12345  ", "operador", " srv.exemplo.local ", "10.0.0.9"));

    [Fact]
    public void SoEspacosContaComoVazio()
        => Assert.Equal("ssh 10.0.0.50", SshComposer.BuildSshCommand("   ", "", "10.0.0.50", ""));

    [Fact] // null não quebra a montagem
    public void NullNaoQuebra()
        => Assert.Equal("ssh 10.0.0.50", SshComposer.BuildSshCommand(null, null, "10.0.0.50", null));
}

public class ParseSshCommandTests
{
    [Fact] // o que build monta, parse desmonta de volta igual
    public void RoundTrip()
    {
        var cmd = SshComposer.BuildSshCommand("12345", "operador", "10.0.0.50", "10.0.0.9");
        Assert.Equal(new SshParts("12345", "operador", "10.0.0.50", "10.0.0.9"),
            SshComposer.ParseSshCommand(cmd));
    }

    [Fact]
    public void ComandoQueNaoEhSsh()
        => Assert.Null(SshComposer.ParseSshCommand("route print"));

    [Fact] // só a forma de 4 partes é reconhecida
    public void SshComMenosDeQuatroPartes()
    {
        Assert.Null(SshComposer.ParseSshCommand("ssh 10.0.0.50"));
        Assert.Null(SshComposer.ParseSshCommand("ssh user@host"));
    }

    [Fact]
    public void VazioENull()
    {
        Assert.Null(SshComposer.ParseSshCommand(""));
        Assert.Null(SshComposer.ParseSshCommand(null));
    }

    [Fact]
    public void EspacoEmVolta()
        => Assert.Equal(new SshParts("a", "b", "c", "d"), SshComposer.ParseSshCommand("  ssh a@b@c@d  "));

    [Fact] // 'ssh a@b@c@d -v' não é a forma pura; não deve ser desmontado
    public void ComandoComArgumentoExtraNaoCasa()
        => Assert.Null(SshComposer.ParseSshCommand("ssh a@b@c@d -v"));
}
