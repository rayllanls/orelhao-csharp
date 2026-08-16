using System.Net;
using System.Net.Sockets;

namespace Orelhao.Core;

/// <summary>Informações da rede local da máquina.</summary>
public static class NetworkInfo
{
    /// <summary>
    /// IP da placa que sairia para a internet — o IP da máquina. Conectar um
    /// socket UDP não manda pacote nenhum: só faz o SO escolher a rota e, com
    /// ela, a interface local. É instantâneo e offline. Sem rede, cai no loopback.
    /// </summary>
    public static string LocalIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 80));
            if (socket.LocalEndPoint is IPEndPoint local)
                return local.Address.ToString();
        }
        catch (SocketException) { }
        return "127.0.0.1";
    }

    /// <summary>
    /// true se o IP é de uma faixa privada/local (RFC 1918, loopback,
    /// link-local, CGNAT). Decide se a consulta vai ao DNS interno ou à internet.
    /// </summary>
    public static bool IsPrivate(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;
        if (ip.AddressFamily != AddressFamily.InterNetwork)
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal;

        var b = ip.GetAddressBytes();
        return b[0] switch
        {
            10 => true,                                   // 10.0.0.0/8
            127 => true,                                  // 127.0.0.0/8
            169 when b[1] == 254 => true,                 // 169.254.0.0/16 link-local
            172 when b[1] >= 16 && b[1] <= 31 => true,    // 172.16.0.0/12
            192 when b[1] == 168 => true,                 // 192.168.0.0/16
            100 when b[1] >= 64 && b[1] <= 127 => true,   // 100.64.0.0/10 CGNAT
            _ => false,
        };
    }

    /// <summary>Tenta interpretar o texto como IP; útil para escolher a consulta.</summary>
    public static bool TryParseIp(string text, out IPAddress ip)
        => IPAddress.TryParse(text.Trim(), out ip!);
}
