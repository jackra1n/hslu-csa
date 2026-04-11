//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using ZumoLib;

namespace ZumoApp;

class Program
{
    static void Main(string[] args)
    {
        Utils.WaitForDebugger();

        Zumo.Instance.Cm4Button.ButtonChanged += ButtonChanged;
        Zumo.Instance.ZumoButton.ButtonChanged += ButtonChanged2;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("F1   Track +1000 mm");
            Console.WriteLine("F2   Track -1000 mm");
            Console.WriteLine("F3   Turn +90°");
            Console.WriteLine("F4   Turn -90°");
            Console.WriteLine("F5   Lidar On");
            Console.WriteLine("F6   Lidar Off");
            Console.WriteLine("F8   Ping Zumo");
            Console.WriteLine("F9   Toggle Led");
            bool redir = Console.IsInputRedirected;
            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.F1:
                    Console.WriteLine("Driving forward 500 mm");
                    TryDrive(500);
                    break;

                case ConsoleKey.F2:
                    Console.WriteLine("Driving backward 500 mm");
                    TryDrive(-500);
                    break;

                case ConsoleKey.F3:
                    //Zumo.Instance.Drive.DriveTurn(90, 100, 100);
                    break;

                case ConsoleKey.F4:
                    //Zumo.Instance.Drive.DriveTurn(-90, 100, 100);
                    break;

                case ConsoleKey.F5:
                    Zumo.Instance.Lidar.SetPower(true);
                    while (!Console.KeyAvailable)
                    {
                        LidarPoint p = Zumo.Instance.Lidar[45];
                        //Console.SetCursorPosition(0, 0);
                        Console.WriteLine($"Speed {Zumo.Instance.Lidar.Speed} °/sec \tDistance: {p.Distance / 1000f} m    ");
                        Thread.Sleep(200);
                    }
                    break;
                case ConsoleKey.F6:
                    Zumo.Instance.Lidar.SetPower(false);
                    break;

                case ConsoleKey.F8:
                    bool result = Zumo.Instance.Ping.DoPing();
                    Console.WriteLine("Ping " + (result ? "OK" : "timeout"));
                    break;

                case ConsoleKey.F9:
                    Zumo.Instance.Cm4Led.Toggle();
                    break;

                case ConsoleKey.Escape:
                    Zumo.Instance.Lidar.SetPower(false);
                    return;
            }
        }
    }


    public static void ButtonChanged(object? sender, ButtonStateChangedEventArgs args)
    {
        Console.WriteLine("CM4 Button State: " + args.Pressed);
    }

    public static void ButtonChanged2(object? sender, ButtonStateChangedEventArgs args)
    {
        Console.WriteLine("Zumo Button State: " + args.Pressed);
    }

    private static void TryDrive(short distance)
    {
        Zumo.Instance.Drive.ResetEncoderDistance();

        bool accepted = Zumo.Instance.Drive.Forward(distance, 100, 100);
        Console.WriteLine($"Track command accepted: {accepted} ({Zumo.Instance.Drive.LastResponse})");

        if (!accepted)
        {
            short speed = distance >= 0 ? (short)80 : (short)-80;
            Console.WriteLine("Falling back to constant-speed test for 800 ms");
            bool constantAccepted = Zumo.Instance.Drive.ConstantSpeed(speed, speed);
            Console.WriteLine($"Constant-speed command accepted: {constantAccepted} ({Zumo.Instance.Drive.LastResponse})");
            Thread.Sleep(800);
            Zumo.Instance.Drive.Stop();
        }

        Thread.Sleep(100);
    }
}
