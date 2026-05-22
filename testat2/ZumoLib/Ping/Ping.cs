//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//    
using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ZumoLib;

public class Ping : ComDevice
{

    public Ping(ICom com) : base(com, 0xD1) { }

    public string DoPing()
    {
        return SetRequest(5, 0xD1, "Ping");
    }

}
