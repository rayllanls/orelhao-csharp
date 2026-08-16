using System.Net;
using DnsClient;
using DnsClient.Protocol;

namespace Orelhao.Core;

/// <summary>Tipos de registro DNS que o toolkit consulta.</summary>
public enum DnsRecordKind { A, AAAA, CNAME, MX, TXT, NS, PTR }

/// <summary>Resposta de um servidor numa checagem de propagação.</summary>
/// <param name="Server">IP do servidor DNS consultado.</param>
/// <param name="Records">Registros retornados (formatados).</param>
/// <param name="Error">Mensagem de erro, se a consulta a esse servidor falhou.</param>
public readonly record struct DnsServerResult(string Server, IReadOnlyList<string> Records, string? Error);

/// <summary>
/// Consultas DNS contra servidores escolhidos (os resolvedores da tela), não os
/// do sistema. Suporta vários tipos de registro e checagem de propagação
/// (perguntar a cada servidor em paralelo e comparar as respostas).
/// </summary>
public static class DnsService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    private static LookupClient BuildClient(IEnumerable<string> dnsServers)
    {
        var endpoints = dnsServers
            .Select(s => IPAddress.TryParse(s, out var ip) ? new IPEndPoint(ip, 53) : null)
            .Where(e => e is not null)
            .Cast<IPEndPoint>()
            .ToArray();

        var options = new LookupClientOptions(endpoints)
        {
            Timeout = Timeout,
            UseCache = false,
            Retries = 1,
        };
        return new LookupClient(options);
    }

    private static QueryType ToQueryType(DnsRecordKind kind) => kind switch
    {
        DnsRecordKind.A => QueryType.A,
        DnsRecordKind.AAAA => QueryType.AAAA,
        DnsRecordKind.CNAME => QueryType.CNAME,
        DnsRecordKind.MX => QueryType.MX,
        DnsRecordKind.TXT => QueryType.TXT,
        DnsRecordKind.NS => QueryType.NS,
        DnsRecordKind.PTR => QueryType.PTR,
        _ => QueryType.A,
    };

    // Formata cada registro num texto legível; tipos desconhecidos caem no ToString.
    private static string Format(DnsResourceRecord record) => record switch
    {
        ARecord a => a.Address.ToString(),
        AaaaRecord aaaa => aaaa.Address.ToString(),
        CNameRecord c => c.CanonicalName.Value.TrimEnd('.'),
        MxRecord m => $"{m.Preference}  {m.Exchange.Value.TrimEnd('.')}",
        TxtRecord t => string.Join(" ", t.Text),
        NsRecord n => n.NSDName.Value.TrimEnd('.'),
        PtrRecord p => p.PtrDomainName.Value.TrimEnd('.'),
        _ => record.ToString() ?? "",
    };

    /// <summary>Consulta um tipo de registro (usa todos os servidores; o primeiro que responder).</summary>
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        string name, DnsRecordKind kind, IReadOnlyList<string> dnsServers, CancellationToken ct = default)
    {
        var client = BuildClient(dnsServers);
        var result = await client.QueryAsync(name.Trim(), ToQueryType(kind), cancellationToken: ct).ConfigureAwait(false);
        return result.Answers.Select(Format).Where(s => s.Length > 0).ToList();
    }

    /// <summary>
    /// Checagem de propagação: pergunta o mesmo registro a cada servidor
    /// separadamente, em paralelo, para comparar as respostas.
    /// </summary>
    public static async Task<IReadOnlyList<DnsServerResult>> PropagationAsync(
        string name, DnsRecordKind kind, IReadOnlyList<string> dnsServers, CancellationToken ct = default)
    {
        var tasks = dnsServers.Select(async server =>
        {
            try
            {
                var records = await ResolveAsync(name, kind, new[] { server }, ct).ConfigureAwait(false);
                return new DnsServerResult(server, records, null);
            }
            catch (Exception ex)
            {
                return new DnsServerResult(server, Array.Empty<string>(), ex.Message);
            }
        });
        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>Resolve o nome (PTR) de um IP. Lista vazia se nada responder.</summary>
    public static async Task<IReadOnlyList<string>> ReverseAsync(
        string ip, IReadOnlyList<string> dnsServers, CancellationToken ct = default)
    {
        if (!IPAddress.TryParse(ip, out var address))
            return Array.Empty<string>();

        var client = BuildClient(dnsServers);
        var result = await client.QueryReverseAsync(address, ct).ConfigureAwait(false);
        return result.Answers.PtrRecords()
            .Select(r => r.PtrDomainName.Value.TrimEnd('.'))
            .ToList();
    }

    /// <summary>Resolve um hostname para IP(s). Lista vazia se nada responder.</summary>
    public static async Task<IReadOnlyList<string>> ForwardAsync(
        string hostname, IReadOnlyList<string> dnsServers, CancellationToken ct = default)
    {
        var client = BuildClient(dnsServers);
        var result = await client.QueryAsync(hostname, QueryType.A, cancellationToken: ct).ConfigureAwait(false);
        return result.Answers.ARecords()
            .Select(r => r.Address.ToString())
            .ToList();
    }
}
