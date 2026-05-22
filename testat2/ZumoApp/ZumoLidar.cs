using System.Runtime.CompilerServices;
using ZumoLib;

namespace ZumoApp {
    public class ZumoLidar {

        private static bool stop = false;
        private static Task? monitorTask;

        /// <summary>
        ///  Schaltet das Lidar ein.
        /// </summary>
        public static void On() {
            Zumo.Instance.Lidar.SetPower(true);
            // Das Lidar muss zuerst ein wenig die Gegend scannen. 
            Console.Write("Init");
            for (int i = 0; i < 20; i++) {
                LidarPoint p = Zumo.Instance.Lidar[45];
                Console.Write(".");
                Thread.Sleep(100);
            }
            Console.WriteLine();
        }
        /// <summary>
        /// Schaltet die Beobachtung vor dem Zumo Roboter ein.
        /// </summary>
        /// <param name="distance">Distanz zum zu einem potentiellen Hindernis in mm.</param>
        public static void LookAt(short distance) {
            stop = false;
            monitorTask = Task.Run(() => {
                while (!stop) {
                    LidarPoint p = Zumo.Instance.Lidar[45];
                    if (p.Distance <= distance && p.Distance > 0) {
                        Console.WriteLine($"Obstacle detected at {p.Distance}mm - emergency stop!");
                        Zumo.Instance.Drive.Stop();
                        return;
                    }
                    Thread.Sleep(100);
                }
            });
        }
        /// <summary>
        /// Wartet bis der Monitor-Task beendet ist.
        /// Muss nach Off() aufgerufen werden, da Off() stop=true setzt
        /// und der Monitor-Task dann beendet wird.
        /// </summary>
        public static void WaitForCompletion() {
            if (monitorTask != null) {
                monitorTask.Wait(TimeSpan.FromSeconds(5));
            }
        }
        /// <summary>
        ///  Schaltet das Lidar aus.
        /// </summary>
        public static void Off() {
            stop = true;
            Zumo.Instance.Lidar.SetPower(false);
            // Der Notstopp muss für die nächste Fahrt wieder aufgehoben werden.
        }
    }
}
