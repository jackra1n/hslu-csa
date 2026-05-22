//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using System;

namespace ZumoLib;

public class Sound : ComDevice
{
    public Sound(ICom com) : base(com, 0x50)
    { }

    public void PlayMusic(SoundItem item)
    {
        byte nr = 9;
        switch (item)
        {
            case SoundItem.KnightRider:
                nr = 0;
                break;
            case SoundItem.StarWars:
                nr = 1;
                break;
            case SoundItem.SuperMario:
                nr = 2;
                break;
            case SoundItem.Wasted:
                nr = 3;
                break;
            case SoundItem.HarryPotter:
                nr = 4;
                break;
            case SoundItem.LOTR:
                nr = 5;
                break;
            case SoundItem.IndianaJones:
                nr = 6;
                break;
            case SoundItem.Bond007:
                nr = 7;
                break;
            case SoundItem.Pacman:
                nr = 8;
                break;
        }

        SetRequest($"1{nr:X1}");
    }
    
    public void PlayBeep(ushort frequency, ushort duration = 1000)
    {
        SetRequest($"0{frequency:X4}{duration:X4}");
    }
}
