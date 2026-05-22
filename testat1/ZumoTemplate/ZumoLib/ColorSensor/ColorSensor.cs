using System;
using System.Globalization;

namespace ZumoLib;

public class ColorSensor : ComDevice
{
    private const byte ColorSensorDispatcher = 0x31;
    private const int ResponsePayloadIndex = 4;
    private const int ResponsePayloadLength = 4;

    public ColorSensor(ICom com) : base(com, ColorSensorDispatcher)
    {
    }

    public ushort? ReadHue()
    {
        string response = GetRequest(ColorSensorDispatcher, "0");
        if (response.Length < ResponsePayloadIndex + ResponsePayloadLength)
        {
            return null;
        }

        if (!ushort.TryParse(response.Substring(ResponsePayloadIndex, ResponsePayloadLength), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort hue))
        {
            return null;
        }

        return hue <= 359 ? hue : null;
    }

    public DetectedColor ReadDetectedColor()
    {
        return Classify(ReadHue());
    }

    public bool CalibrateBlack()
    {
        return SendSetAndCheck("600");
    }

    public bool CalibrateWhite()
    {
        return SendSetAndCheck("601");
    }

    public static DetectedColor Classify(ushort? hue)
    {
        if (!hue.HasValue)
        {
            return DetectedColor.Unknown;
        }

        ushort value = hue.Value;
        if (value >= 340 || value <= 20)
        {
            return DetectedColor.Red;
        }

        if (value >= 90 && value <= 150)
        {
            return DetectedColor.Green;
        }

        return DetectedColor.Other;
    }

    private bool SendSetAndCheck(string payload)
    {
        string response = SetRequest(ColorSensorDispatcher, payload);
        return !string.IsNullOrEmpty(response) && !response.Contains("$03$", StringComparison.Ordinal);
    }
}
