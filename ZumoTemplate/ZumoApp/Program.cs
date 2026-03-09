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
        Console.WriteLine("Zumo starting...");

        Zumo.Instance.Cm4Button.ButtonChanged += ButtonChanged;

        Console.WriteLine("CM4 button monitoring enabled.");
        Console.WriteLine("Press Enter to toggle the CM4 LED. Press Ctrl+C to exit.");

        // Test Button
#if false
        Zumo.Instance.Cm4Button.ButtonChanged += ButtonChanged;
#endif        

        // Test Led
#if false
        for (int i = 0; i < 6; i++)
        {
            Zumo.Instance.Cm4Led.Toggle();
            Thread.Sleep(100);
        }
#endif

        while (true)
        {
            Console.ReadLine();
            Zumo.Instance.Cm4Led.Toggle();
            Console.WriteLine("LED State: " + Zumo.Instance.Cm4Led.Enabled);
        }

        // Test Lidar
#if false        
        Lidar lidar = Zumo.Instance.Lidar;
        lidar.SetPower(false);
        Console.ReadKey();
        lidar.SetPower(true);

        while (!Console.KeyAvailable)
        {
            LidarPoint p = lidar[45];
            Console.WriteLine(lidar.Speed + "\t" + p.Distance / 1000f);
            Thread.Sleep(200);
        }

        lidar.SetPower(false);
#endif

    }
    
    public static void ButtonChanged(object? sender, ButtonStateChangedEventArgs args)
    {
        Console.WriteLine("Button State: " + args.Pressed);
    }
}
