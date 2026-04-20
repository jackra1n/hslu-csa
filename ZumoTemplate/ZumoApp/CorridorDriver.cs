using ZumoLib;

namespace ZumoApp;

public class CorridorDriver
{
    private const double CenteringGain = 0.4;
    private const int MaxCorrectionSpeed = 40;
    private const int SideIgnoreNoWallMm = 600;
    private const int FrontStopMm = 120;
    private const int FrontSectorHalfWidth = 15;
    private const int SideSectorHalfWidth = 7;
    private const int SideSectorStep = 2;
    private const int PollIntervalMs = 30;

    public void Drive(int distanceMm, ushort baseSpeed, CancellationToken ct)
    {
        Console.WriteLine($"CorridorDrive: target={distanceMm}mm speed={baseSpeed}");

        if (!Zumo.Instance.Drive.ResetEncoderDistance())
        {
            Console.WriteLine("CorridorDrive: failed to reset encoders.");
            return;
        }

        Zumo.Instance.Drive.ConstantSpeed((short)baseSpeed, (short)baseSpeed);

        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                int front = GetSectorClearance(0, FrontSectorHalfWidth);
                if (front > 0 && front <= FrontStopMm)
                {
                    Console.WriteLine($"CorridorDrive: FRONT STOP at {front}mm");
                    break;
                }

                int rightDist = GetSideSectorClearance(45);
                int leftDist = GetSideSectorClearance(315);

                short correction = ComputeCorrection(rightDist, leftDist);

                short leftSpeed = (short)(baseSpeed + correction);
                short rightSpeed = (short)(baseSpeed - correction);

                Zumo.Instance.Drive.ConstantSpeed(leftSpeed, rightSpeed);

                var (_, _, totalDist) = ReadEncoderDistance();
                if (totalDist >= distanceMm)
                {
                    Console.WriteLine($"CorridorDrive: reached target ({totalDist}/{distanceMm}mm)");
                    break;
                }

                Console.WriteLine($"  dist={totalDist} front={front} R={rightDist} L={leftDist} corr={correction} spd=({leftSpeed},{rightSpeed})");

                Thread.Sleep(PollIntervalMs);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("CorridorDrive: canceled.");
        }
        finally
        {
            Zumo.Instance.Drive.Stop();
        }
    }

    private static int GetSideSectorClearance(int centerAngle)
    {
        List<int> values = new();

        for (int offset = -SideSectorHalfWidth; offset <= SideSectorHalfWidth; offset += SideSectorStep)
        {
            int angle = (centerAngle + offset + 360) % 360;
            int distance = Zumo.Instance.Lidar[angle].Distance;
            if (distance > 0 && distance < SideIgnoreNoWallMm) values.Add(distance);
        }

        if (values.Count == 0) return 0;

        values.Sort();
        int qi = Math.Max(0, (values.Count - 1) / 4);
        return values[qi];
    }

    private static short ComputeCorrection(int rightDist, int leftDist)
    {
        if (rightDist == 0 && leftDist == 0) return 0;

        int diff;
        if (rightDist == 0)
        {
            diff = -leftDist;
        }
        else if (leftDist == 0)
        {
            diff = rightDist;
        }
        else
        {
            diff = rightDist - leftDist;
        }

        double raw = diff * CenteringGain;
        raw = Math.Clamp(raw, -MaxCorrectionSpeed, MaxCorrectionSpeed);
        return (short)Math.Round(raw);
    }

    private static int GetSectorClearance(int centerAngle, int halfWidth)
    {
        List<int> values = new();

        for (int offset = -halfWidth; offset <= halfWidth; offset += 2)
        {
            int angle = (centerAngle + offset + 360) % 360;
            int distance = Zumo.Instance.Lidar[angle].Distance;
            if (distance > 0) values.Add(distance);
        }

        if (values.Count == 0) return 0;

        values.Sort();
        int qi = Math.Max(0, (values.Count - 1) / 4);
        return values[qi];
    }

    private static (short left, short right, int total) ReadEncoderDistance()
    {
        (short left, short right) = Zumo.Instance.Drive.GetEncoderDistance();
        int total = (Math.Abs(left) + Math.Abs(right)) / 2;
        return (left, right, total);
    }
}
