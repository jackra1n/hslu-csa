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

public class Cm4Led : ILed
{
    public event EventHandler<LedStateChangedEventArgs>? LedStateChanged;

    internal Cm4Led(GpioController gpio, int pin)
    {
        Pin = pin;
        Gpio = gpio;

        Gpio.OpenPin(Pin, PinMode.Output);
        Enabled = false;
    }

    internal GpioController Gpio { get; }
    internal int Pin { get; }

    public bool Enabled
    {
        get { return Gpio.Read(Pin) == PinValue.High; }
        set
        {
            bool current = Enabled;
            if (current == value)
            {
                return;
            }

            Gpio.Write(Pin, value ? PinValue.High : PinValue.Low);
            LedStateChanged?.Invoke(this, new LedStateChangedEventArgs(value));
        }
    }

    public void Toggle()
    {
        Enabled = !Enabled;
    }

}
