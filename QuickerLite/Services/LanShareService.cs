using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickerLite.Services;

public sealed class LanShareService
{
    private TcpListener? listener;
    private CancellationTokenSource? cancellation;

    public bool IsRunning => listener is not null;

    public string? RootDirectory { get; private set; }

    public string? ShareUrl { get; private set; }

    public string Start(string rootDirectory, string ipAddress, int port, bool restartIfRunning)
    {
        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException(rootDirectory);
        }

        if (IsRunning)
        {
            if (!restartIfRunning)
            {
                return ShareUrl ?? "";
            }

            Stop();
        }

        if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
        {
            ipAddress = GetDefaultLanIpAddress();
        }

        if (port is < 1 or > 65535)
        {
            port = 8080;
        }

        RootDirectory = Path.GetFullPath(rootDirectory);
        ShareUrl = $"http://{ipAddress}:{port}/";
        listener = new TcpListener(IPAddress.Any, port);
        cancellation = new CancellationTokenSource();
        listener.Start();
        _ = Task.Run(() => ListenAsync(cancellation.Token));
        return ShareUrl;
    }

    public void Stop()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;

        listener?.Stop();
        listener = null;
        RootDirectory = null;
        ShareUrl = null;
    }

    public static string GetDefaultLanIpAddress()
    {
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address.Address))
            .Select(address => address.Address.ToString())
            .ToList();

        return addresses.FirstOrDefault(IsPrivateIpv4)
            ?? addresses.FirstOrDefault()
            ?? "127.0.0.1";
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && listener is not null)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClientAsync(client), token);
            }
            catch when (token.IsCancellationRequested)
            {
            }
            catch
            {
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using var _ = client;
        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            var requestLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                return;
            }

            while (!string.IsNullOrEmpty(await reader.ReadLineAsync()))
            {
            }

            var parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteSimpleResponseAsync(stream, 405, "Method Not Allowed", "Only GET is supported.");
                return;
            }

            await HandlePathAsync(stream, parts[1]);
        }
        catch
        {
        }
    }

    private async Task HandlePathAsync(NetworkStream stream, string requestPath)
    {
        var root = RootDirectory;
        if (string.IsNullOrWhiteSpace(root))
        {
            await WriteSimpleResponseAsync(stream, 404, "Not Found", "Share is not running.");
            return;
        }

        var pathPart = requestPath.Split('?', 2)[0].TrimStart('/');
        var relative = Uri.UnescapeDataString(pathPart);
        var localPath = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsPathInsideRoot(root, localPath))
        {
            await WriteSimpleResponseAsync(stream, 403, "Forbidden", "Path is outside share root.");
            return;
        }

        if (Directory.Exists(localPath))
        {
            await WriteDirectoryListingAsync(stream, root, localPath, requestPath);
        }
        else if (File.Exists(localPath))
        {
            await WriteFileAsync(stream, localPath);
        }
        else
        {
            await WriteSimpleResponseAsync(stream, 404, "Not Found", "File not found.");
        }
    }

    private static async Task WriteDirectoryListingAsync(Stream stream, string root, string directory, string requestPath)
    {
        var cleanPath = requestPath.Split('?', 2)[0].TrimEnd('/');
        var html = new StringBuilder();
        html.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>Quicker Lite LAN Share</title>");
        html.Append("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;}a{display:block;padding:6px 0;color:#1261a6;text-decoration:none;}a:hover{text-decoration:underline}.meta{color:#666;font-size:12px}</style>");
        html.Append("</head><body>");
        html.Append("<h2>Quicker Lite LAN Share</h2>");
        html.Append("<div class=\"meta\">").Append(WebUtility.HtmlEncode(directory)).Append("</div><hr>");

        if (!PathsEqual(root, directory))
        {
            var parentPath = cleanPath.Contains('/') ? cleanPath[..cleanPath.LastIndexOf('/')] : "";
            html.Append("<a href=\"").Append(string.IsNullOrEmpty(parentPath) ? "/" : parentPath + "/").Append("\">../</a>");
        }

        foreach (var dir in Directory.GetDirectories(directory).OrderBy(Path.GetFileName))
        {
            html.Append("<a href=\"").Append(ToHref(root, dir)).Append("/\">[DIR] ")
                .Append(WebUtility.HtmlEncode(Path.GetFileName(dir))).Append("/</a>");
        }

        foreach (var file in Directory.GetFiles(directory).OrderBy(Path.GetFileName))
        {
            var info = new FileInfo(file);
            html.Append("<a href=\"").Append(ToHref(root, file)).Append("\">[FILE] ")
                .Append(WebUtility.HtmlEncode(info.Name)).Append(" <span class=\"meta\">")
                .Append(FormatSize(info.Length)).Append("</span></a>");
        }

        html.Append("</body></html>");
        await WriteResponseAsync(stream, 200, "OK", "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html.ToString()));
    }

    private static async Task WriteFileAsync(Stream stream, string file)
    {
        var header =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: application/octet-stream\r\n" +
            $"Content-Length: {new FileInfo(file).Length}\r\n" +
            $"Content-Disposition: attachment; filename=\"{EscapeHeaderValue(Path.GetFileName(file))}\"\r\n" +
            "Connection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);
        await stream.WriteAsync(headerBytes);
        await using var fileStream = File.OpenRead(file);
        await fileStream.CopyToAsync(stream);
    }

    private static async Task WriteSimpleResponseAsync(Stream stream, int statusCode, string reason, string message)
    {
        await WriteResponseAsync(stream, statusCode, reason, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes(message));
    }

    private static async Task WriteResponseAsync(Stream stream, int statusCode, string reason, string contentType, byte[] body)
    {
        var header =
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Connection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
        await stream.WriteAsync(body);
    }

    private static bool IsPathInsideRoot(string root, string path)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || PathsEqual(normalizedRoot, normalizedPath);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ToHref(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
        return "/" + Uri.EscapeDataString(relative).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }

    private static string EscapeHeaderValue(string value)
    {
        return value.Replace("\"", "'", StringComparison.Ordinal).Replace("\r", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal);
    }

    private static bool IsPrivateIpv4(string ip)
    {
        return ip.StartsWith("10.", StringComparison.Ordinal)
            || ip.StartsWith("192.168.", StringComparison.Ordinal)
            || ip.StartsWith("172.16.", StringComparison.Ordinal)
            || ip.StartsWith("172.17.", StringComparison.Ordinal)
            || ip.StartsWith("172.18.", StringComparison.Ordinal)
            || ip.StartsWith("172.19.", StringComparison.Ordinal)
            || ip.StartsWith("172.2", StringComparison.Ordinal)
            || ip.StartsWith("172.30.", StringComparison.Ordinal)
            || ip.StartsWith("172.31.", StringComparison.Ordinal);
    }
}
