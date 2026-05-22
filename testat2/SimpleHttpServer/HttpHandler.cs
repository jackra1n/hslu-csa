using System.Net.Sockets;
using System.Net;
using System.Text;

namespace SimpleHttpServer;

public class HttpHandler {
    private readonly TcpClient _client;
    private readonly string _contentDirectory;

    public HttpHandler(TcpClient client, string contentDirectory) {
        _client = client;
        _contentDirectory = contentDirectory;
    }

    public void Do() {
        using (_client) {
            try {
                NetworkStream stream = _client.GetStream();
                using StreamReader reader = new StreamReader(stream, Encoding.ASCII);
                using StreamWriter writer = new StreamWriter(stream, Encoding.ASCII);

                Console.WriteLine($"Connection from: {_client.Client.RemoteEndPoint}");

                string? requestLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(requestLine)) return;

                string[] parts = requestLine.Split(' ');
                if (parts.Length < 3) {
                    SendResponse(writer, 400, "Bad Request", null);
                    return;
                }

                string method = parts[0];
                string urlPath = Uri.UnescapeDataString(parts[1]);

                while (true) {
                    string? headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) break;
                }

                if (method == "GET") {
                    HandleGet(writer, urlPath);
                } else {
                    SendResponse(writer, 405, "Method Not Allowed", null);
                }
            } catch {
            }
        }
    }

    void HandleGet(StreamWriter writer, string urlPath) {
        if (urlPath == "/") {
            SendDirectoryListing(writer);
            return;
        }

        string fileName = urlPath.TrimStart('/');
        string filePath = Path.Combine(_contentDirectory, fileName);

        string fullFilePath = Path.GetFullPath(filePath);
        string fullContentDir = Path.GetFullPath(_contentDirectory);
        if (!fullFilePath.StartsWith(fullContentDir + Path.DirectorySeparatorChar) &&
            fullFilePath != fullContentDir) {
            SendResponse(writer, 403, "Forbidden", null);
            return;
        }

        if (!File.Exists(filePath)) {
            SendResponse(writer, 404, "Not Found", null);
            return;
        }

        try {
            string contentType = GetContentType(filePath);
            byte[] content = File.ReadAllBytes(filePath);
            string header =
                "HTTP/1.1 200 OK\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {content.Length}\r\n" +
                "Connection: close\r\n" +
                "\r\n";

            writer.Write(header);
            writer.Flush();
            writer.BaseStream.Write(content, 0, content.Length);
            writer.BaseStream.Flush();
        } catch {
            SendResponse(writer, 500, "Internal Server Error", null);
        }
    }

    static string GetContentType(string filePath) {
        string ext = Path.GetExtension(filePath).ToLower();
        return ext switch {
            ".txt" => "text/plain; charset=ASCII",
            ".html" => "text/html; charset=ASCII",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }

    void SendDirectoryListing(StreamWriter writer) {
        var files = Directory.GetFiles(_contentDirectory)
            .Select(f => Path.GetFileName(f))
            .OrderByDescending(f => f);

        var sb = new StringBuilder();
        sb.AppendLine("<html><head><title>Protocol Logs</title></head><body>");
        sb.AppendLine("<h1>Protocol Logs</h1><ul>");

        foreach (string file in files) {
            sb.AppendLine($"<li><a href=\"/{file}\">{file}</a></li>");
        }

        sb.AppendLine("</ul></body></html>");

        byte[] html = Encoding.ASCII.GetBytes(sb.ToString());
        string header =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: text/html; charset=ASCII\r\n" +
            $"Content-Length: {html.Length}\r\n" +
            "Connection: close\r\n" +
            "\r\n";

        writer.Write(header);
        writer.Flush();
        writer.BaseStream.Write(html, 0, html.Length);
        writer.BaseStream.Flush();
    }

    static void SendResponse(StreamWriter writer, int statusCode, string statusText, byte[]? content) {
        content ??= Encoding.ASCII.GetBytes(
            $"<html><body><h1>{statusCode} {statusText}</h1></body></html>");

        string header =
            $"HTTP/1.1 {statusCode} {statusText}\r\n" +
            "Content-Type: text/html; charset=ASCII\r\n" +
            $"Content-Length: {content.Length}\r\n" +
            "Connection: close\r\n" +
            "\r\n";

        writer.Write(header);
        writer.Flush();
        writer.BaseStream.Write(content, 0, content.Length);
        writer.BaseStream.Flush();
    }
}
