//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using System;
using System.Globalization;

namespace ZumoLib;

public class Drive : ComDevice
{
    private const byte DriveDispatcher = 0x24;
    private const byte EncoderDispatcher = 0x22;

    public event EventHandler? DriveFinished;

    public Drive(ICom com) : base(com, DriveDispatcher, EncoderDispatcher)
    {
    }

    public bool Forward(short distance, short speed = 10, short acceleration = 1)
    {
        return SendSetAndCheck(DriveDispatcher, $"2{distance:X4}{speed:X4}{acceleration:X4}");
    }

    public bool DriveTrack(short distance, ushort speed, ushort acceleration, sbyte offset = 0)
    {
        return SendSetAndCheck(DriveDispatcher, $"C{distance:X4}{speed:X4}{acceleration:X4}{unchecked((byte)offset):X2}");
    }

    public bool Rotate(short angle, short speed = 1000, short acceleration = 1000)
    {
        return SendSetAndCheck(DriveDispatcher, $"A{angle:X4}{speed:X4}{acceleration:X4}");
    }

    public bool DriveTurn(short angle, ushort speed, ushort acceleration)
    {
        return Rotate(angle, unchecked((short)speed), unchecked((short)acceleration));
    }

    public bool ConstantSpeed(short leftSpeed, short rightSpeed)
    {
        return SendSetAndCheck(DriveDispatcher, $"1{leftSpeed:X4}{rightSpeed:X4}");
    }

    public bool CurveWithRadius(short angle, short radius, short speed = 1000, short acceleration = 1000)
    {
        return SendSetAndCheck(DriveDispatcher, $"9{angle:X4}{radius:X4}{speed:X4}{acceleration:X4}");
    }

    public bool SetRotationCalibrationFactor(short calibrationFactor)
    {
        return SendSetAndCheck(DriveDispatcher, $"B{calibrationFactor:X4}");
    }

    public bool DriveTurnCalib(short factor)
    {
        return SetRotationCalibrationFactor(factor);
    }

    public (short leftSpeed, short rightSpeed) GetCurrentSpeed()
    {
        string response = GetRequest(5, DriveDispatcher, "1");
        if (response.Length >= 13)
        {
            return (ParseInt16(response, 5), ParseInt16(response, 9));
        }

        return (0, 0);
    }

    public short GetRemainingDistance()
    {
        string response = GetRequest(5, DriveDispatcher, "2");
        if (response.Length >= 9)
        {
            return ParseInt16(response, 5);
        }

        return 0;
    }

    public short DriveGetRemainingDistance()
    {
        return GetRemainingDistance();
    }

    public bool DriveIsRunning()
    {
        string response = GetRequest(5, DriveDispatcher, "7");
        return response.Length >= 6 && byte.Parse(response.Substring(5), NumberStyles.HexNumber, CultureInfo.InvariantCulture) == 1;
    }

    public (short leftSpeed, short rightSpeed) GetEncoderSpeed()
    {
        string response = GetRequest(5, EncoderDispatcher, "0");
        if (response.Length >= 14)
        {
            return (ParseInt16(response, 6), ParseInt16(response, 10));
        }

        return (0, 0);
    }

    public (short leftDistance, short rightDistance) GetEncoderDistance()
    {
        string response = GetRequest(5, EncoderDispatcher, "1");
        if (response.Length >= 14)
        {
            return (ParseInt16(response, 6), ParseInt16(response, 10));
        }

        return (0, 0);
    }

    public bool ResetEncoderDistance()
    {
        return SendSetAndCheck(EncoderDispatcher, "0");
    }

    public bool SetEncoderDistanceFactor(short calibrationFactor)
    {
        return SendSetAndCheck(EncoderDispatcher, $"1{calibrationFactor:X4}");
    }

    public void Stop()
    {
        ConstantSpeed(0, 0);
    }

    protected override bool ProcessEvent(string message)
    {
        if (message == "5!24FF")
        {
            DriveFinished?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    private static short ParseInt16(string response, int startIndex)
    {
        return unchecked((short)ushort.Parse(response.Substring(startIndex, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private bool SendSetAndCheck(byte dispatcher, string payload)
    {
        string response = SetRequest(5, dispatcher, payload);
        return IsAcceptedResponse(response);
    }

    private static bool IsAcceptedResponse(string response)
    {
        return !string.IsNullOrEmpty(response) && !response.Contains("$03$", StringComparison.Ordinal);
    }
}
