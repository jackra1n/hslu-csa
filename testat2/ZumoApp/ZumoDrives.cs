using ZumoLib;

namespace ZumoApp {

    public class ZumoDrives {
        private static Drive drive = Zumo.Instance.Drive;
        public static string ZumoPing() {
            drive.Response = Zumo.Instance.Ping.DoPing() + "\n";
            return drive.Response;
        }
        public static string ZumoDriveA() {
            drive.ResetStop();
            drive.Response = "";
            ZumoDrives.ZumoPing();
            drive.Track(500, 100, 100);
            drive.Turn(135, 100, 100);
            drive.Track(700, 100, 100);
            drive.Turn(-135, 100, 100);
            drive.Track(500, 100, 100);
            drive.Turn(-135, 100, 100);
            drive.Track(700, 100, 100);
            return drive.Response;
        }
        public static string ZumoDriveB() {
            drive.ResetStop();
            drive.Response = "";
            ZumoDrives.ZumoPing();
            drive.Track(500, 100, 100);
            drive.Turn(90, 100, 100);
            drive.Track(700, 100, 100);
            drive.Turn(135, 100, 100);
            drive.Track(500, 100, 100);
            drive.Turn(-135, 100, 100);
            drive.Track(700, 100, 100);
            return drive.Response;
        }
        public static string ZumoDriveC() {
            drive.ResetStop();
            drive.Response = "";
            ZumoDrives.ZumoPing();
            drive.Track(500, 100, 100);
            drive.Turn(135, 100, 100);
            drive.Track(700, 100, 100);
            drive.Turn(135, 100, 100);
            drive.Track(500, 100, 100);
            drive.Turn(135, 100, 100);
            drive.Track(700, 100, 100);
            return drive.Response;
        }
    }
}
