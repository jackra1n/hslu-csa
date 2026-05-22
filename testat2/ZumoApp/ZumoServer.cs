using System.Net;
using System.Net.Sockets;
using ZumoLib;

namespace ZumoApp {
    public class ZumoServer {
        private const int TcpPort = 5000;
        private const int HttpPort = 8080;
        private const string StudentName = "Jacek Lajdecki";
        private const short LidarDistanceMm = 300;

        static void Main() {
            Utils.WaitForDebugger();

            Task.Run(() => HttpFileServer.Start(HttpPort));
            Console.WriteLine($"HTTP file server started on port {HttpPort}");

            var listener = new TcpListener(IPAddress.Any, TcpPort);
            listener.Start();
            Console.WriteLine($"Zumo TCP server listening on port {TcpPort}");

            while (true) {
                Console.WriteLine("Waiting for client connection...");
                TcpClient client;
                try {
                    client = listener.AcceptTcpClient();
                } catch (Exception ex) {
                    Console.WriteLine($"Accept error: {ex.Message}");
                    continue;
                }

                Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
                HandleClient(client);
            }
        }

        static void HandleClient(TcpClient client) {
            ZumoLidar.On();
            try {
                using (client) {
                    NetworkStream stream = client.GetStream();
                    using StreamReader reader = new StreamReader(stream);
                    using StreamWriter writer = new StreamWriter(stream);
                    writer.AutoFlush = true;

                    writer.WriteLine("Zumo Remote Control - Select route: A, B, C");

                    string? input = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(input)) return;

                    string choice = input.Trim().ToUpper();

                    string? logFileName = null;
                    switch (choice) {
                        case "A":
                            logFileName = ExecuteDrive(ZumoDrives.ZumoDriveA, "A");
                            break;
                        case "B":
                            logFileName = ExecuteDrive(ZumoDrives.ZumoDriveB, "B");
                            break;
                        case "C":
                            logFileName = ExecuteDrive(ZumoDrives.ZumoDriveC, "C");
                            break;
                        default:
                            writer.WriteLine("Invalid choice. Use A, B, or C.");
                            break;
                    }

                    if (logFileName != null) {
                        writer.WriteLine($"Drive completed. Log available at port {HttpPort}: /{logFileName}");
                    } else if (choice == "A" || choice == "B" || choice == "C") {
                        writer.WriteLine("Drive failed. No log saved.");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Client handler error: {ex.Message}");
            } finally {
                ZumoLidar.Off();
            }
        }

        static string? ExecuteDrive(Func<string> driveFunc, string routeName) {
            Console.WriteLine($"Starting drive {routeName}...");

            try {
                ZumoLidar.On();
                ZumoLidar.LookAt(LidarDistanceMm);

                string response = driveFunc();
                ZumoLidar.Off();

                string logFileName = SaveLogFile(routeName, response);
                Console.WriteLine($"Log saved to: {logFileName}");

                Zumo.Instance.Drive.ResetStop();

                return logFileName;
            } catch (Exception ex) {
                Console.WriteLine($"Drive {routeName} error: {ex.Message}");
                ZumoLidar.Off();
                Zumo.Instance.Drive.ResetStop();
                return null;
            }
        }

        static string SaveLogFile(string routeName, string response) {
            string logDir = HttpFileServer.LogDirectory;
            Directory.CreateDirectory(logDir);

            string timestamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            string fileName = $"protocol_{routeName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(logDir, fileName);

            string header = $"// {StudentName} // {timestamp}";

            using StreamWriter sw = new StreamWriter(filePath, false);
            sw.WriteLine(header);
            sw.Write(response);

            return fileName;
        }
    }
}
