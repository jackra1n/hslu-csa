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

        MazeRunner runner = new MazeRunner();
        CancellationTokenSource? runToken = null;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("F1   Start maze run (with color calibration)");
            Console.WriteLine("F2   Stop maze run");
            Console.WriteLine("F3   Control");
            Console.WriteLine("ESC  Exit");
            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.F1:
                    if (runToken != null)
                    {
                        Console.WriteLine("Maze run already active.");
                        break;
                    }

                    Console.WriteLine("Calibrating color sensor...");
                    Console.WriteLine("Place robot on BLACK surface and press Enter...");
                    Console.ReadLine();
                    RunColorCalibrationStep(true);
                    Thread.Sleep(400);

                    Console.WriteLine("Place robot on WHITE surface and press Enter...");
                    Console.ReadLine();
                    RunColorCalibrationStep(false);
                    Thread.Sleep(400);

                    Console.WriteLine("Powering on LiDAR and waiting for stable data...");
                    Zumo.Instance.Lidar.SetPower(true);
                    Thread.Sleep(2200);

                    Console.WriteLine("Calibration complete.");
                    Console.WriteLine("Place robot at maze start position and press Enter to start maze run...");
                    Console.ReadLine();
                    Console.WriteLine("Starting maze run...");
                    runToken = new CancellationTokenSource();
                    CancellationTokenSource runTokenLocal = runToken;
                    Task.Run(() =>
                    {
                        try
                        {
                            runner.Run(runTokenLocal.Token, lidarPrewarmed: true);
                        }
                        finally
                        {
                            runTokenLocal.Dispose();
                            if (ReferenceEquals(runToken, runTokenLocal))
                            {
                                runToken = null;
                            }
                        }
                    });
                    break;

                case ConsoleKey.F2:
                    runToken?.Cancel();
                    break;

                case ConsoleKey.F3:
                    RunControlMenu();
                    break;

                case ConsoleKey.Escape:
                    runToken?.Cancel();
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

        if (args.Pressed)
        {
            SoundItem[] songs = Enum.GetValues<SoundItem>();
            SoundItem song = songs[Random.Shared.Next(songs.Length)];
            Zumo.Instance.Sound.Play(song);
        }
    }

    private static void RunControlMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Control Menu");
            Console.WriteLine("F1   Turn +90°");
            Console.WriteLine("F2   Turn -90°");
            Console.WriteLine("F3   Lidar On");
            Console.WriteLine("F4   Lidar Off");
            Console.WriteLine("F5   Read Color Sensor");
            Console.WriteLine("F6   Ping Zumo");
            Console.WriteLine("F7   Toggle Led");
            Console.WriteLine("ESC  Back");

            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.F1:
                    Console.WriteLine("Turning clockwise 90°");
                    TryRotate(90);
                    break;

                case ConsoleKey.F2:
                    Console.WriteLine("Turning counter-clockwise 90°");
                    TryRotate(-90);
                    break;

                case ConsoleKey.F3:
                    Zumo.Instance.Lidar.SetPower(true);
                    while (!Console.KeyAvailable)
                    {
                        LidarPoint p = Zumo.Instance.Lidar[45];
                        Console.WriteLine($"Speed {Zumo.Instance.Lidar.Speed} °/sec \tDistance: {p.Distance / 1000f} m    ");
                        Thread.Sleep(200);
                    }
                    Console.ReadKey(intercept: true);
                    break;

                case ConsoleKey.F4:
                    Zumo.Instance.Lidar.SetPower(false);
                    break;

                case ConsoleKey.F5:
                    ReadColorSensor();
                    break;

                case ConsoleKey.F6:
                    bool result = Zumo.Instance.Ping.DoPing();
                    Console.WriteLine("Ping " + (result ? "OK" : "timeout"));
                    break;

                case ConsoleKey.F7:
                    Zumo.Instance.Cm4Led.Toggle();
                    break;

                case ConsoleKey.Escape:
                    return;
            }
        }
    }

    private static void TryRotate(short angle)
    {
        if (!Zumo.Instance.Drive.Rotate(angle))
        {
            Console.WriteLine("Rotate command rejected.");
        }
    }

    private static void ReadColorSensor()
    {
        ushort? hue = Zumo.Instance.ColorSensor.ReadHue();
        DetectedColor color = ColorSensor.Classify(hue);

        if (hue.HasValue)
        {
            Console.WriteLine($"Color sensor hue: {hue.Value}°, detected: {color}");
        }
        else
        {
            Console.WriteLine($"Color sensor hue invalid, detected: {color}");
        }
    }

    private static void RunColorCalibrationStep(bool blackReference)
    {
        bool result = blackReference
            ? Zumo.Instance.ColorSensor.CalibrateBlack()
            : Zumo.Instance.ColorSensor.CalibrateWhite();

        Console.WriteLine(result
            ? $"Color sensor {(blackReference ? "black" : "white")} calibration accepted."
            : $"Color sensor {(blackReference ? "black" : "white")} calibration rejected.");
    }
}
