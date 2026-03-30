//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using System;

namespace ZumoLib;

public class Display
{
    private const string backlightPath = "/sys/class/backlight/10-0045/brightness";

    public void Dim(byte value)
    {
        File.WriteAllText(backlightPath, value.ToString());
    }

    public void PowerOff()
    {
        Dim(0);
    }

    public void PowerOn()
    {
        Dim(255);
    }

}
