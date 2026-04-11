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
            Console.WriteLine("F1   Track +500 mm");
            Console.WriteLine("F2   Track -500 mm");
            Console.WriteLine("F3   Turn +90°");
            Console.WriteLine("F4   Turn -90°");
            Console.WriteLine("F5   Lidar On");
            Console.WriteLine("F6   Lidar Off");
            Console.WriteLine("F7   Read Color Sensor");
            Console.WriteLine("F8   Ping Zumo");
            Console.WriteLine("F9   Toggle Led");
            Console.WriteLine("F11  Color Calibrate Black");
            Console.WriteLine("F12  Color Calibrate White");
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
                    Console.WriteLine("Turning clockwise 90°");
                    TryRotate(90);
                    break;

                case ConsoleKey.F4:
                    Console.WriteLine("Turning counter-clockwise 90°");
                    TryRotate(-90);
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

                case ConsoleKey.F7:
                    ReadColorSensor();
                    break;

                case ConsoleKey.F8:
                    bool result = Zumo.Instance.Ping.DoPing();
                    Console.WriteLine("Ping " + (result ? "OK" : "timeout"));
                    break;

                case ConsoleKey.F9:
                    Zumo.Instance.Cm4Led.Toggle();
                    break;

                case ConsoleKey.F11:
                    RunColorCalibrationStep(true);
                    break;

                case ConsoleKey.F12:
                    RunColorCalibrationStep(false);
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

        if (args.Pressed)
        {
            SoundItem[] songs = Enum.GetValues<SoundItem>();
            SoundItem song = songs[Random.Shared.Next(songs.Length)];
            Zumo.Instance.Sound.Play(song);
        }
    }

    private static void TryDrive(short distance)
    {
        if (!Zumo.Instance.Drive.Forward(distance, 100, 100))
        {
            Console.WriteLine("Drive command rejected.");
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
