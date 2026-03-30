//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using System;

namespace ZumoLib.Button;

public class ZumoButton : ComDevice, IButton
{

    public event EventHandler<ButtonStateChangedEventArgs>? ButtonChanged;

    internal ZumoButton(ICom com) : base(com, 0x61) { }

    
    public bool Pressed { get; private set; }

    protected override bool ProcessEvent(string message)
    {
        if (message == "5!6101")
        {
            ButtonChanged?.Invoke(this, new ButtonStateChangedEventArgs(true));
        }

        if (message == "5!6100")
        {
            ButtonChanged?.Invoke(this, new ButtonStateChangedEventArgs(false));
        }
        return true;
    }
}
