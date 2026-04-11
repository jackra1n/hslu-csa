//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using System;
using System.ComponentModel.DataAnnotations;
using System.Device.Gpio;
using ZumoLib.Button;

namespace ZumoLib;

public class Zumo
{
    public static Zumo Instance { get; } = new Zumo();

    private Zumo()
    {
        Com = new Com();
        Gpio = new GpioController();

        Cm4Led = new Cm4Led(Gpio, 18);
        Cm4Button = new Cm4Button(Gpio, 27);
        ZumoButton = new ZumoButton(Com);
        Display = new Display();

        Ping = new Ping(Com);
        Drive = new Drive(Com);

        RgbLedRearLeft = new RgbLedRear(Com, LedRear.Left);
        RgbLedRearRight = new RgbLedRear(Com, LedRear.Right);

        RedLedRearLeft = new RedLedRear(Com, LedRear.Left);
        RedLedRearRight = new RedLedRear(Com, LedRear.Right);

        Lidar = new Lidar(Gpio);
        // Sound = new Sound(Com);
        // ColorSensor = new ColorSensor(Com);
        // RgbLedFront = new RgbLedFront(Com);
    }


    internal GpioController Gpio { get; }
    internal ICom Com { get; }

    public Ping Ping { get; }
    public Drive Drive { get; }

    public RgbLedRear RgbLedRearLeft { get; }
    public RgbLedRear RgbLedRearRight { get; }

    public RedLedRear RedLedRearLeft { get; }
    public RedLedRear RedLedRearRight { get; }

    public Display Display { get; }
    public ILed Cm4Led { get; }
    public IButton Cm4Button { get;  }
    public IButton ZumoButton { get; }

    // public Sound Sound { get; }
    // public ColorSensor ColorSensor { get; }
    // public RgbLedFront RgbLedFront{ get; }
    public Lidar Lidar { get; }
}
