//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using System;

namespace ZumoLib;

public class RgbLedRear : ComDevice
{
    public RgbLedRear(ICom com, LedRear ledRear) : base(com, 0x12)
    {
        LedRear = ledRear;
    }

    public LedRear LedRear{ get; }

    public void SetValue(byte r, byte g, byte b)
    {
        SetRequest($"{(byte)LedRear:X2}{r:X2}{g:X2}{b:X2}");
    }
}
