//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//   
using System;

namespace ZumoLib;

public class Utils
{
    /// <summary>
    /// This function waits until the Debugger has been attached or the Enter Key has been pressed
    /// </summary>
    /// <returns>
    /// true => Debugger is attached
    /// false => Enter Key was pressed
    /// </returns>
    public static bool WaitForDebugger()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && args[1] == "--debug")
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Waiting for Debugger or press <Enter> to continue");
            Console.ForegroundColor = color;
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    return true;
                }
                Thread.Sleep(100);
            }
        }
        return false;
    }
}
