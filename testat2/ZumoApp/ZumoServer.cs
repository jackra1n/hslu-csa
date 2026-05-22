using ZumoLib;

namespace ZumoApp {
    public class ZumoServer {
        private static bool finished = false;
        static void Main() {
            while (!finished) {
                Console.WriteLine("Press A or B or C and <Enter> to start...");
                string choise = Console.ReadLine();
                string response = "wrong choise";
                switch (choise.ToUpper()) {
                    case "A":
                        response = ZumoDrives.ZumoDriveA();
                        break;
                    case "B":
                        response = ZumoDrives.ZumoDriveB();
                        break;
                    case "C":
                        response = ZumoDrives.ZumoDriveC();
                        break;
                    default: 
                        finished = true;
                        response = "Exit";
                        break;
                }
                Console.WriteLine(response);
            }
        }
    }
}
