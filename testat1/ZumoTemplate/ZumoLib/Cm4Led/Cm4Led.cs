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
        Gpio.OpenPin(pin, PinMode.Output);
    }

    internal GpioController Gpio { get; }
    public int Pin { get; }

    public bool Enabled
    {
        get { return (bool)Gpio.Read(Pin); }
        set
        {
            if (value != Enabled)
            {
                Gpio.Write(Pin, value);
                LedStateChanged?.Invoke(this, new LedStateChangedEventArgs(value));
            }
        }
    }

    public void Toggle() { Enabled = !Enabled; }

}