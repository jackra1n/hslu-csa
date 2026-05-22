using System.Diagnostics;
using ZumoLib;

namespace ZumoApp;

public class CorridorDriver
{
    private const double CenteringGain = 0.4;
    private const int MaxCorrectionSpeed = 40;
    private const int NoWallThresholdMm = 300;
    private const int BothWallsFarThresholdMm = 600;
    private const int HysteresisIterations = 3;
    private const int FrontStopMm = 120;
    private const int FrontSectorHalfWidth = 15;
    private const int SideSectorHalfWidth = 7;
    private const int SideSectorStep = 2;
    private const int PollIntervalMs = 30;
    private const int StartupGraceIterations = 7;
    private const double FudgeFactor = 1.036;

    public void Drive(int distanceMm, ushort baseSpeed, CancellationToken ct)
    {
        int driveTimeMs = (int)((distanceMm * 1000.0) / baseSpeed * FudgeFactor);
        Console.WriteLine($"CorridorDrive: target={distanceMm}mm speed={baseSpeed} time={driveTimeMs}ms");

        int rightInit = GetSideSectorClearance(45);
        int leftInit = GetSideSectorClearance(315);
        sbyte initialOffset = ComputeOffsetRaw(rightInit, leftInit);
        Console.WriteLine($"  initial offset={initialOffset} (R={rightInit} L={leftInit})");

        Zumo.Instance.Drive.ConstantSpeed(
            (short)(baseSpeed + initialOffset),
            (short)(baseSpeed - initialOffset));

        var stopwatch = Stopwatch.StartNew();
        int iteration = 0;
        int lastPrintedIter = -1;
        int consecutiveZeroOffset = 0;

        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                iteration++;

                int front = GetSectorClearance(0, FrontSectorHalfWidth);
                if (front > 0 && front <= FrontStopMm)
                {
                    Console.WriteLine($"CorridorDrive: FRONT STOP at {front}mm");
                    break;
                }

                long elapsed = stopwatch.ElapsedMilliseconds;
                if (elapsed >= driveTimeMs)
                {
                    Console.WriteLine($"CorridorDrive: reached target ({elapsed}ms / {driveTimeMs}ms)");
                    break;
                }

                int rightDist = GetSideSectorClearance(45);
                int leftDist = GetSideSectorClearance(315);

                sbyte rawOffset = ComputeOffsetRaw(rightDist, leftDist);

                sbyte offset;
                if (rawOffset == 0)
                {
                    consecutiveZeroOffset++;
                    offset = 0;
                }
                else if (consecutiveZeroOffset < HysteresisIterations)
                {
                    offset = 0;
                    consecutiveZeroOffset++;
                }
                else
                {
                    offset = rawOffset;
                    consecutiveZeroOffset = 0;
                }

                if (iteration > StartupGraceIterations)
                {
                    short leftSpeed = (short)(baseSpeed + offset);
                    short rightSpeed = (short)(baseSpeed - offset);
                    Zumo.Instance.Drive.ConstantSpeed(leftSpeed, rightSpeed);
                }

                if (iteration - lastPrintedIter >= 3)
                {
                    int estDist = (int)(elapsed * baseSpeed / 1000.0);
                    Console.WriteLine($"  iter={iteration} dist=~{estDist}mm time={elapsed}ms front={front} R={rightDist} L={leftDist} offset={offset}");
                    lastPrintedIter = iteration;
                }

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
            long elapsed = stopwatch.ElapsedMilliseconds;
            int estDist = (int)(elapsed * baseSpeed / 1000.0);
            Console.WriteLine($"  final: time={elapsed}ms est_dist=~{estDist}mm");
        }
    }

    private static sbyte ComputeOffsetRaw(int rightDist, int leftDist)
    {
        bool rFar = rightDist == 0 || rightDist > BothWallsFarThresholdMm;
        bool lFar = leftDist == 0 || leftDist > BothWallsFarThresholdMm;

        if (rFar && lFar) return 0;

        int diff;
        if (rFar)
        {
            diff = -leftDist;
        }
        else if (lFar)
        {
            diff = rightDist;
        }
        else
        {
            diff = rightDist - leftDist;
        }

        double raw = diff * CenteringGain;
        raw = Math.Clamp(raw, -MaxCorrectionSpeed, MaxCorrectionSpeed);
        return (sbyte)Math.Round(raw);
    }

    private static int GetSideSectorClearance(int centerAngle)
    {
        List<int> values = new();

        for (int offset = -SideSectorHalfWidth; offset <= SideSectorHalfWidth; offset += SideSectorStep)
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
}
