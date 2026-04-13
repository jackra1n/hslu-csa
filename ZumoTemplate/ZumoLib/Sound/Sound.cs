using System;

namespace ZumoLib;

public class Sound : ComDevice
{
    private const byte SoundDispatcher = 0x50;

    public Sound(ICom com) : base(com, SoundDispatcher)
    {
    }

    public bool Beep(ushort frequency, ushort duration)
    {
        return SendSetAndCheck($"0{frequency:X4}{duration:X4}");
    }

    public bool Play(SoundItem item)
    {
        if (!Enum.IsDefined(item))
        {
            throw new ArgumentOutOfRangeException(nameof(item));
        }

        return SendSetAndCheck($"1{(int)item:X1}");
    }

    public bool PlayRandomSound()
    {
        var items = Enum.GetValues<SoundItem>();
        var random = new Random();
        var item = items[random.Next(items.Length)];
        return Play(item);
    }

    private bool SendSetAndCheck(string payload)
    {
        string response = SetRequest(SoundDispatcher, payload);
        return !string.IsNullOrEmpty(response) && !response.Contains("$03$", StringComparison.Ordinal);
    }
}
