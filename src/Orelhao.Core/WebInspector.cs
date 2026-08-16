using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Orelhao.Core;

/// <summary>Cabeçalhos de uma resposta HTTP(S).</summary>
public sealed record HttpHeaderInfo(
    int StatusCode, string StatusText, string FinalUrl,
    IReadOnlyList<KeyValuePair<string, string>> Headers);

/// <summary>Dados do certificado TLS de um host.</summary>
public sealed record CertInfo(
    string Subject, string Issuer, DateTime NotBefore, DateTime NotAfter,
    int DaysRemaining, IReadOnlyList<string> SubjectAltNames,
    bool Trusted, string? TrustNote);

/// <summary>
/// Inspeção rápida de um alvo web: cabeçalhos HTTP e o certificado SSL/TLS —
/// sem abrir navegador nem ferramentas pesadas. Só APIs nativas do .NET.
/// </summary>
public static class WebInspector
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10,
    })
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    public static async Task<HttpHeaderInfo> HeadersAsync(string target, CancellationToken ct = default)
    {
        var url = NormalizeUrl(target);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.ParseAdd("Orelhao");

        // ResponseHeadersRead: não baixa o corpo, só o necessário para os headers.
        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        var headers = new List<KeyValuePair<string, string>>();
        foreach (var h in resp.Headers)
            headers.Add(new(h.Key, string.Join(", ", h.Value)));
        foreach (var h in resp.Content.Headers)
            headers.Add(new(h.Key, string.Join(", ", h.Value)));

        var final = resp.RequestMessage?.RequestUri?.ToString() ?? url.ToString();
        return new HttpHeaderInfo((int)resp.StatusCode, resp.ReasonPhrase ?? "",
            final, headers.OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public static async Task<CertInfo> CertificateAsync(string target, CancellationToken ct = default)
    {
        var (host, port) = ParseHostPort(target);

        using var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);

        X509Certificate2? cert = null;
        var trusted = false;
        string? trustNote = null;

        using var ssl = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false,
            (_, certificate, _, errors) =>
            {
                if (certificate is not null)
                    cert = new X509Certificate2(certificate);
                trusted = errors == SslPolicyErrors.None;
                if (errors != SslPolicyErrors.None)
                    trustNote = errors.ToString();
                return true; // aceita mesmo inválido, só para inspecionar
            });

        await ssl.AuthenticateAsClientAsync(host).ConfigureAwait(false);

        if (cert is null)
            throw new InvalidOperationException("O host não apresentou certificado.");

        var days = (int)Math.Floor((cert.NotAfter - DateTime.Now).TotalDays);
        return new CertInfo(cert.Subject, cert.Issuer, cert.NotBefore, cert.NotAfter,
            days, GetSans(cert), trusted, trustNote);
    }

    private static IReadOnlyList<string> GetSans(X509Certificate2 cert)
    {
        foreach (var ext in cert.Extensions)
        {
            if (ext.Oid?.Value == "2.5.29.17") // Subject Alternative Name
            {
                try
                {
                    var san = new X509SubjectAlternativeNameExtension(ext.RawData);
                    return san.EnumerateDnsNames().ToList();
                }
                catch { return Array.Empty<string>(); }
            }
        }
        return Array.Empty<string>();
    }

    private static Uri NormalizeUrl(string target)
    {
        var t = target.Trim();
        if (!t.Contains("://"))
            t = "https://" + t;
        return new Uri(t);
    }

    private static (string Host, int Port) ParseHostPort(string target)
    {
        var t = target.Trim();
        // Tira o esquema, se veio uma URL completa.
        if (t.Contains("://") && Uri.TryCreate(t, UriKind.Absolute, out var uri))
            return (uri.Host, uri.Port == -1 ? 443 : uri.Port);
        // host[:porta]
        var parts = t.Split(':', 2);
        var port = parts.Length == 2 && int.TryParse(parts[1], out var p) ? p : 443;
        return (parts[0], port);
    }
}
