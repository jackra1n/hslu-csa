using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleHttpServer;

public class SimpleHttpServer {
    private static readonly string ContentDirectory =
        Path.Combine(Directory.GetCurrentDirectory(), "logs");

    public static void Main() {
        Directory.CreateDirectory(ContentDirectory);
        Console.WriteLine($"Content directory: {ContentDirectory}");

        var listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine("HTTP file server listening on port 8080");

        while (true) {
            try {
                TcpClient client = listener.AcceptTcpClient();
                Task.Run(() => {
                    new HttpHandler(client, ContentDirectory).Do();
                });
            } catch (Exception ex) {
                Console.WriteLine($"Accept error: {ex.Message}");
            }
        }
    }
}
