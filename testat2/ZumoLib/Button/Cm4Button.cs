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

public class Cm4Button : IButton
{
        public event EventHandler<ButtonStateChangedEventArgs>? ButtonChanged;
     
        internal Cm4Button(GpioController gpio, int pin)
        {
            Pin = pin;
            Gpio = gpio;
            Gpio.OpenPin(pin, PinMode.Input);

            Thread t = new Thread(Run);
            t.IsBackground = true;
            t.Start();
        }

        internal GpioController Gpio { get; }
        public int Pin { get; }


        public bool Pressed
        {
            get { return Gpio.Read(Pin) == PinValue.Low; }
        }

        private void Run()
        {
            bool oldState = Pressed;
            while(true)
            {
                //Console.WriteLine(Pin + " => " + Gpio.Read(Pin));
                bool newState = Pressed;
                if (oldState != newState)
                {
                    ButtonChanged?.Invoke(this, new ButtonStateChangedEventArgs(newState));
                    oldState = newState;
                }
                Thread.Sleep(50);
            }
        }   
}
