using ZumoLib;

namespace ZumoApp;

public class MazeRunner
{
    private const ushort MoveSpeed = 95;
    private const ushort MoveAcceleration = 110;

    private const short TurnAngleDegrees = 90;
    private const short ProbeStepMillimeters = 70;
    private const short MarkerCrossDistanceMillimeters = 140;
    private const short RecoveryBacktrackMillimeters = -90;
    private const short MaxForwardChunkMillimeters = 30;

    private const int FrontBlockedThresholdMillimeters = 210;
    private const int FrontClearThresholdMillimeters = 280;
    private const int SideOpeningThresholdMillimeters = 320;
    private const int ExitThresholdMillimeters = 1250;

    private const int FrontSectorHalfWidthDegrees = 14;
    private const int SideSectorHalfWidthDegrees = 18;
    private const int SectorAngleStepDegrees = 2;

    private const int MarkerConfirmationSamples = 3;
    private static readonly TimeSpan MarkerSamplePeriod = TimeSpan.FromMilliseconds(35);
    private static readonly TimeSpan MarkerCooldown = TimeSpan.FromMilliseconds(800);

    private const int MaxSteps = 320;

    private DateTime _lastMarkerTriggerAt = DateTime.MinValue;

    public void Run(CancellationToken cancellationToken, bool lidarPrewarmed = false)
    {
        Console.WriteLine("Maze run started.");
        SetStatusLed(24, 24, 100);
        if (!lidarPrewarmed)
        {
            Zumo.Instance.Lidar.SetPower(true);
            Thread.Sleep(2200);
        }

        try
        {
            for (int step = 0; step < MaxSteps; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int front = GetSectorClearance(0, FrontSectorHalfWidthDegrees);
                int right = GetSectorClearance(90, SideSectorHalfWidthDegrees);
                int left = GetSectorClearance(270, SideSectorHalfWidthDegrees);

                Console.WriteLine($"step={step:D3} front={front} right={right} left={left}");

                if (LooksLikeExit(front, right, left))
                {
                    HandleExit(cancellationToken);
                    return;
                }

                if (TryDirection(Direction.Right, right, cancellationToken))
                {
                    continue;
                }

                if (TryDirection(Direction.Front, front, cancellationToken))
                {
                    continue;
                }

                if (TryDirection(Direction.Left, left, cancellationToken))
                {
                    continue;
                }

                RecoverFromDeadEnd(cancellationToken);
            }

            Console.WriteLine("Maze run stopped after max steps.");
            Zumo.Instance.Drive.Stop();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Maze run canceled.");
            Zumo.Instance.Drive.Stop();
        }
        finally
        {
            SetStatusLed(0, 0, 0);
            Zumo.Instance.Lidar.SetPower(false);
        }
    }

    private bool TryDirection(Direction direction, int clearance, CancellationToken cancellationToken)
    {
        if (direction == Direction.Front)
        {
            if (clearance <= FrontClearThresholdMillimeters)
            {
                return false;
            }

            return TryTraverseOpening(Direction.Front, cancellationToken);
        }

        if (clearance <= SideOpeningThresholdMillimeters)
        {
            return false;
        }

        return TryTraverseOpening(direction, cancellationToken);
    }

    private bool TryTraverseOpening(Direction direction, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Trying {direction} opening...");
        SetStatusLed(100, 70, 0);

        switch (direction)
        {
            case Direction.Right:
                DriveTurn(-TurnAngleDegrees, cancellationToken);
                break;
            case Direction.Left:
                DriveTurn(TurnAngleDegrees, cancellationToken);
                break;
        }

        if (!SafeTrackForward(ProbeStepMillimeters, cancellationToken))
        {
            Console.WriteLine("Blocked during approach, recovering...");
            DriveTrackRaw(RecoveryBacktrackMillimeters, cancellationToken);
            UndoTurn(direction, cancellationToken);
            SetStatusLed(120, 0, 0);
            return false;
        }

        if (!ConfirmMarkerDuringCrossing(cancellationToken))
        {
            Console.WriteLine("No valid marker confirmed, backing out...");
            DriveTrackRaw(RecoveryBacktrackMillimeters, cancellationToken);
            UndoTurn(direction, cancellationToken);
            SetStatusLed(120, 0, 0);
            return false;
        }

        Console.WriteLine("Marker confirmed, crossing opening.");
        SetStatusLed(0, 95, 0);
        if (!SafeTrackForward(MarkerCrossDistanceMillimeters, cancellationToken))
        {
            Console.WriteLine("Blocked while crossing, backing out...");
            DriveTrackRaw(RecoveryBacktrackMillimeters, cancellationToken);
            UndoTurn(direction, cancellationToken);
            SetStatusLed(120, 0, 0);
            return false;
        }

        SetStatusLed(24, 24, 100);
        return true;
    }

    private bool ConfirmMarkerDuringCrossing(CancellationToken cancellationToken)
    {
        int consecutive = 0;
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(1200);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DetectedColor color = Zumo.Instance.ColorSensor.ReadDetectedColor();
            if (color is DetectedColor.Red or DetectedColor.Green)
            {
                consecutive++;
                Console.WriteLine($"Marker sample {consecutive}/{MarkerConfirmationSamples}: {color}");
            }
            else
            {
                consecutive = 0;
            }

            if (consecutive >= MarkerConfirmationSamples)
            {
                TriggerMarkerTone();
                return true;
            }

            Thread.Sleep(MarkerSamplePeriod);
        }

        return false;
    }

    private void TriggerMarkerTone()
    {
        DateTime now = DateTime.UtcNow;
        if (now - _lastMarkerTriggerAt < MarkerCooldown)
        {
            return;
        }

        _lastMarkerTriggerAt = now;
        Zumo.Instance.Sound.Beep(1500, 120);
    }

    private static bool LooksLikeExit(int front, int right, int left)
    {
        return front >= ExitThresholdMillimeters && right >= ExitThresholdMillimeters && left >= ExitThresholdMillimeters;
    }

    private static void HandleExit(CancellationToken cancellationToken)
    {
        Console.WriteLine("Exit detected.");
        Zumo.Instance.Sound.Play(SoundItem.SuperMario);
        DriveTrackRaw(250, cancellationToken);
        Zumo.Instance.Drive.Stop();
    }

    private static void RecoverFromDeadEnd(CancellationToken cancellationToken)
    {
        Console.WriteLine("Dead end recovery: turn around.");
        DriveTurn(180, cancellationToken);
    }

    private static void UndoTurn(Direction direction, CancellationToken cancellationToken)
    {
        switch (direction)
        {
            case Direction.Right:
                DriveTurn(TurnAngleDegrees, cancellationToken);
                break;
            case Direction.Left:
                DriveTurn(-TurnAngleDegrees, cancellationToken);
                break;
        }
    }

    private static void DriveTrackRaw(short length, CancellationToken cancellationToken)
    {
        if (!Zumo.Instance.Drive.DriveTrack(length, MoveSpeed, MoveAcceleration))
        {
            Console.WriteLine("DriveTrack command rejected.");
            return;
        }

        WaitDriveFinished(cancellationToken);
    }

    private static void DriveTurn(short angle, CancellationToken cancellationToken)
    {
        if (!Zumo.Instance.Drive.DriveTurn(angle, MoveSpeed, MoveAcceleration))
        {
            Console.WriteLine("DriveTurn command rejected.");
            return;
        }

        WaitDriveFinished(cancellationToken);
    }

    private static void WaitDriveFinished(CancellationToken cancellationToken)
    {
        DateTime timeout = DateTime.UtcNow.AddSeconds(6);

        while (Zumo.Instance.Drive.DriveIsRunning())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DateTime.UtcNow > timeout)
            {
                Console.WriteLine("Drive wait timeout.");
                break;
            }

            Thread.Sleep(20);
        }
    }

    private static bool SafeTrackForward(short length, CancellationToken cancellationToken)
    {
        if (length <= 0)
        {
            DriveTrackRaw(length, cancellationToken);
            return true;
        }

        int remaining = length;
        while (remaining > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int clearance = GetSectorClearance(0, FrontSectorHalfWidthDegrees);
            if (clearance == 0 || clearance <= FrontBlockedThresholdMillimeters)
            {
                Console.WriteLine($"Safety stop: front clearance {clearance} mm");
                Zumo.Instance.Drive.Stop();
                return false;
            }

            short chunk = (short)Math.Min(remaining, MaxForwardChunkMillimeters);
            DriveTrackRaw(chunk, cancellationToken);
            remaining -= chunk;
        }

        return true;
    }

    private static int GetSectorClearance(int centerAngle, int halfWidth)
    {
        List<int> values = new List<int>((halfWidth * 2 / SectorAngleStepDegrees) + 1);

        for (int offset = -halfWidth; offset <= halfWidth; offset += SectorAngleStepDegrees)
        {
            int angle = (centerAngle + offset + 360) % 360;
            int distance = Zumo.Instance.Lidar[angle].Distance;
            if (distance > 0)
            {
                values.Add(distance);
            }
        }

        if (values.Count == 0)
        {
            return 0;
        }

        values.Sort();
        int quantileIndex = Math.Max(0, (values.Count - 1) / 4);
        return values[quantileIndex];
    }

    private static void SetStatusLed(byte r, byte g, byte b)
    {
        Zumo.Instance.RgbLedRearLeft.SetValue(r, g, b);
        Zumo.Instance.RgbLedRearRight.SetValue(r, g, b);
    }

    private enum Direction
    {
        Right,
        Front,
        Left,
    }
}
