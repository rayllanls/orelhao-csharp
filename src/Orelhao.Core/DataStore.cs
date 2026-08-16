using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Orelhao.Core;

/// <summary>Resultado de uma carga: os dados e se o arquivo estava corrompido.</summary>
/// <param name="Data">Os dados já normalizados (nunca nulo).</param>
/// <param name="Recovered">true se o arquivo era ilegível e virou <c>.bak</c>.</param>
/// <param name="Error">Detalhe do erro quando <paramref name="Recovered"/> é true.</param>
public readonly record struct LoadResult(AppData Data, bool Recovered, string? Error);

/// <summary>
/// Persistência do shortcuts.json. Normalização tolerante (JSON estragado não
/// derruba o app) + escrita atômica (não corrompe o banco se o processo cair
/// no meio). A UI decide o que fazer com <see cref="LoadResult.Recovered"/>.
/// </summary>
public static class DataStore
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        // snake_case para casar com o formato herdado (groups, dns_servers,
        // ssh_defaults, jump_proxy...) e sem escapar acento/emoji.
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static AppData Empty() => new();

    public static LoadResult Load(string path)
    {
        if (!File.Exists(path))
            return new LoadResult(Empty(), false, null);
        try
        {
            var text = File.ReadAllText(path);
            var node = JsonNode.Parse(text);
            return new LoadResult(Normalize(node), false, null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Banco corrompido: preserva o arquivo problemático e começa limpo.
            try { File.Copy(path, path + ".bak", overwrite: true); }
            catch (IOException) { /* se nem copiar der, seguimos limpos mesmo assim */ }
            catch (UnauthorizedAccessException) { }
            return new LoadResult(Empty(), true, ex.Message);
        }
    }

    public static void Save(string path, AppData data)
    {
        // Escrita atômica: grava num .tmp e substitui de uma vez.
        var tmp = path + ".tmp";
        var json = JsonSerializer.Serialize(data, WriteOptions);
        File.WriteAllText(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>
    /// Garante que os dados tenham o formato esperado, ignorando lixo. Versões
    /// antigas do arquivo continuam abrindo porque os campos ausentes são
    /// completados e os malformados, descartados.
    /// </summary>
    public static AppData Normalize(JsonNode? node)
    {
        if (node is not JsonObject root)
            return Empty();

        var data = new AppData
        {
            Groups = NormalizeGroups(root["groups"] as JsonArray),
            DnsServers = NormalizeDns(root["dns_servers"]),
            SshDefaults = NormalizeSshDefaults(root["ssh_defaults"] as JsonObject),
            History = NormalizeHistory(root["history"] as JsonArray),
            FavoriteOrder = NormalizeStringList(root["favorite_order"] as JsonArray),
            Theme = NormalizeTheme(root["theme"]),
            AccentColor = NormalizeAccent(root["accent_color"]),
        };
        return data;
    }

    private static List<Group> NormalizeGroups(JsonArray? groups)
    {
        var result = new List<Group>();
        if (groups is null)
            return result;

        foreach (var g in groups)
        {
            if (g is not JsonObject group)
                continue;
            var name = AsString(group["name"]);
            if (name is null)
                continue; // grupo sem nome é descartado

            var shortcuts = new List<Shortcut>();
            if (group["shortcuts"] is JsonArray scArray)
            {
                foreach (var s in scArray)
                {
                    if (s is not JsonObject sc)
                        continue;
                    var scName = AsString(sc["name"]);
                    var scCmd = AsString(sc["command"]);
                    if (scName is null || scCmd is null)
                        continue; // atalho sem nome ou sem comando não entra

                    var icon = AsString(sc["icon"]);
                    shortcuts.Add(new Shortcut
                    {
                        Name = scName,
                        Command = scCmd,
                        Icon = string.IsNullOrEmpty(icon) ? Constants.DefaultIcon : icon,
                        Favorite = Truthy(sc["favorite"]),
                    });
                }
            }
            result.Add(new Group { Name = name, Shortcuts = shortcuts });
        }
        return result;
    }

    private static List<string> NormalizeDns(JsonNode? dns)
    {
        // Só vale se for uma lista em que todo item é string; senão, padrão.
        if (dns is JsonArray arr)
        {
            var list = new List<string>();
            foreach (var item in arr)
            {
                var s = AsString(item);
                if (s is null)
                    return new List<string>(Constants.DefaultDns);
                list.Add(s);
            }
            return list;
        }
        return new List<string>(Constants.DefaultDns);
    }

    private static SshDefaults NormalizeSshDefaults(JsonObject? raw)
    {
        return new SshDefaults
        {
            Id = AsString(raw?["id"]) ?? "",
            JumpProxy = AsString(raw?["jump_proxy"]) ?? "",
        };
    }

    private static List<HistoryEntry> NormalizeHistory(JsonArray? history)
    {
        var result = new List<HistoryEntry>();
        if (history is null)
            return result;

        foreach (var h in history)
        {
            if (h is not JsonObject entry)
                continue;
            var name = AsString(entry["name"]);
            var cmd = AsString(entry["command"]);
            var when = AsString(entry["when"]);
            if (name is null || cmd is null || when is null)
                continue;

            var icon = AsString(entry["icon"]);
            result.Add(new HistoryEntry
            {
                Name = name,
                Command = cmd,
                When = when,
                Icon = string.IsNullOrEmpty(icon) ? Constants.DefaultIcon : icon,
            });
            if (result.Count >= Constants.HistoryLimit)
                break;
        }
        return result;
    }

    private static List<string> NormalizeStringList(JsonArray? array)
    {
        var list = new List<string>();
        if (array is null)
            return list;
        foreach (var item in array)
            if (AsString(item) is { } s)
                list.Add(s);
        return list;
    }

    private static string NormalizeTheme(JsonNode? node)
    {
        var value = AsString(node);
        return ThemePreference.IsValid(value) ? value! : Constants.DefaultTheme;
    }

    // Aceita só "#RGB" ou "#RRGGBB"; qualquer outra coisa vira vazio (padrão).
    private static string NormalizeAccent(JsonNode? node)
    {
        var value = AsString(node);
        if (value is not null && Regex.IsMatch(value, "^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$"))
            return value;
        return "";
    }

    /// <summary>Devolve a string se o nó for um valor JSON string; senão null.</summary>
    private static string? AsString(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var s))
            return s;
        return null;
    }

    /// <summary>Verdade ao estilo Python: string não-vazia, número != 0, true.</summary>
    private static bool Truthy(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return false;
            case JsonValue value:
                if (value.TryGetValue<bool>(out var b)) return b;
                if (value.TryGetValue<string>(out var s)) return s.Length > 0;
                if (value.TryGetValue<double>(out var d)) return d != 0;
                return true;
            case JsonArray arr:
                return arr.Count > 0;
            case JsonObject obj:
                return obj.Count > 0;
            default:
                return false;
        }
    }
}
