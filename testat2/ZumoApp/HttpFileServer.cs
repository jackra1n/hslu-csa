using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ZumoApp {
    public class HttpFileServer {
        private static readonly string _logDirectory =
            Path.Combine(Directory.GetCurrentDirectory(), "logs");

        public static string LogDirectory => _logDirectory;

        public static void Start(int port) {
            Directory.CreateDirectory(_logDirectory);

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            while (true) {
                try {
                    TcpClient client = listener.AcceptTcpClient();
                    Task.Run(() => HandleClient(client));
                } catch (Exception ex) {
                    Console.WriteLine($"HTTP accept error: {ex.Message}");
                }
            }
        }

        static void HandleClient(TcpClient client) {
            using (client) {
                try {
                    NetworkStream stream = client.GetStream();
                    using StreamReader reader = new StreamReader(stream, Encoding.ASCII);
                    using StreamWriter writer = new StreamWriter(stream, Encoding.ASCII);

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

        static void HandleGet(StreamWriter writer, string urlPath) {
            if (urlPath == "/") {
                SendDirectoryListing(writer);
                return;
            }

            string fileName = urlPath.TrimStart('/');
            string filePath = Path.Combine(_logDirectory, fileName);

            string fullFilePath = Path.GetFullPath(filePath);
            string fullLogDir = Path.GetFullPath(_logDirectory);
            if (!fullFilePath.StartsWith(fullLogDir + Path.DirectorySeparatorChar) &&
                fullFilePath != fullLogDir) {
                SendResponse(writer, 403, "Forbidden", null);
                return;
            }

            if (!File.Exists(filePath)) {
                SendResponse(writer, 404, "Not Found", null);
                return;
            }

            try {
                byte[] content = File.ReadAllBytes(filePath);
                string contentType = GetContentType(filePath);
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

        static void SendDirectoryListing(StreamWriter writer) {
            var files = Directory.GetFiles(_logDirectory, "*.txt")
                .Select(f => Path.GetFileName(f))
                .OrderByDescending(f => f);

            var sb = new StringBuilder();
            sb.AppendLine("<html><head><title>Zumo Protocol Logs</title></head><body>");
            sb.AppendLine("<h1>Zumo Protocol Logs</h1><ul>");

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
                _ => "application/octet-stream",
            };
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
}
