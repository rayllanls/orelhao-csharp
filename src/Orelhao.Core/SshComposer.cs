using System.Text.RegularExpressions;

namespace Orelhao.Core;

/// <summary>As quatro partes de um comando SSH via proxy (formato do CyberArk).</summary>
public readonly record struct SshParts(string Id, string Account, string Address, string Jump);

/// <summary>
/// Monta e desmonta o comando <c>ssh cofre@conta@servidor@proxyjump</c>.
/// Funções puras — o coração testável do app, decide em que servidor se entra.
/// </summary>
public static partial class SshComposer
{
    // 'ssh' seguido de um único token sem espaços, com folga de espaços em volta.
    [GeneratedRegex(@"^\s*ssh\s+(\S+)\s*$")]
    private static partial Regex SshPattern();

    /// <summary>
    /// Monta <c>ssh a@b@c@d</c> a partir das quatro caixas. Campo vazio (ou só
    /// espaços) é omitido — monta com o que tiver, sem deixar '@@' pelo caminho.
    /// Sem nenhuma parte, devolve string vazia, não um 'ssh ' solto.
    /// </summary>
    public static string BuildSshCommand(string? id, string? account, string? address, string? jump)
    {
        var parts = new[] { id, account, address, jump }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());
        var joined = string.Join("@", parts);
        return joined.Length == 0 ? "" : "ssh " + joined;
    }

    /// <summary>
    /// Quebra <c>ssh a@b@c@d</c> de volta nas quatro partes, ou <c>null</c> se
    /// não casar. Só a forma pura de 4 partes é reconhecida — argumento extra
    /// (ex.: <c>-v</c>) ou número de partes diferente não desmonta.
    /// </summary>
    public static SshParts? ParseSshCommand(string? command)
    {
        var match = SshPattern().Match(command ?? "");
        if (!match.Success)
            return null;
        var parts = match.Groups[1].Value.Split('@');
        if (parts.Length != 4)
            return null;
        return new SshParts(parts[0], parts[1], parts[2], parts[3]);
    }
}
