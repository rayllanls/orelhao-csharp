using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Orelhao.Core;

/// <summary>Dados públicos de um IP, do ipinfo.io.</summary>
public sealed class IpInfo
{
    [JsonPropertyName("ip")] public string? Ip { get; set; }
    [JsonPropertyName("hostname")] public string? Hostname { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("region")] public string? Region { get; set; }
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("org")] public string? Org { get; set; }
    [JsonPropertyName("loc")] public string? Loc { get; set; }
    [JsonPropertyName("postal")] public string? Postal { get; set; }
    [JsonPropertyName("timezone")] public string? Timezone { get; set; }
}

/// <summary>Consulta dados públicos de um IP no ipinfo.io.</summary>
public static class IpLookup
{
    // HttpClient único e reutilizado (o padrão recomendado; um por requisição
    // esgota sockets). Timeout curto: a tela não pode travar esperando.
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8),
        DefaultRequestHeaders = { { "User-Agent", "Orelhao" } },
    };

    public static async Task<IpInfo?> QueryAsync(string ip, CancellationToken ct = default)
    {
        var url = $"https://ipinfo.io/{Uri.EscapeDataString(ip)}/json";
        return await Http.GetFromJsonAsync<IpInfo>(url, ct).ConfigureAwait(false);
    }
}
