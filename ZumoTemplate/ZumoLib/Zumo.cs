//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using System;
using System.Device.Gpio;

namespace ZumoLib;

public class Zumo
{
    public static Zumo Instance { get; } = new Zumo();

    private Zumo()
    {
        Gpio = new GpioController();

        Cm4Led = new Cm4Led(Gpio, 18);
        Cm4Button = new Cm4Button(Gpio, 27);
        //Lidar = new Lidar(Gpio);
    }


    internal GpioController Gpio { get; }

    public ILed Cm4Led { get; private set; }         
    public IButton Cm4Button { get; private set; }
    public Lidar Lidar { get; private set; }
}
