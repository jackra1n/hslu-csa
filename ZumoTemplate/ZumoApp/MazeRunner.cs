using ZumoLib;

namespace ZumoApp;

public class MazeRunner
{
    private const ushort MoveSpeed = 110;
    private const ushort MoveAcceleration = 120;

    private const short TurnAngleDegrees = 90;
    private const short ProbeStepMillimeters = 80;
    private const short MarkerCrossDistanceMillimeters = 140;
    private const short RecoveryBacktrackMillimeters = -90;

    private const int FrontBlockedThresholdMillimeters = 230;
    private const int FrontClearThresholdMillimeters = 290;
    private const int SideOpeningThresholdMillimeters = 330;
    private const int ExitThresholdMillimeters = 1250;

    private const int MarkerConfirmationSamples = 3;
    private static readonly TimeSpan MarkerSamplePeriod = TimeSpan.FromMilliseconds(35);
    private static readonly TimeSpan MarkerCooldown = TimeSpan.FromMilliseconds(800);

    private const int MaxSteps = 320;

    private DateTime _lastMarkerTriggerAt = DateTime.MinValue;

    public void Run(CancellationToken cancellationToken)
    {
        Console.WriteLine("Maze run started.");
        Zumo.Instance.Lidar.SetPower(true);
        Thread.Sleep(600);

        try
        {
            for (int step = 0; step < MaxSteps; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int front = MedianDistance(0, 2);
                int right = MedianDistance(90, 2);
                int left = MedianDistance(270, 2);

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

        switch (direction)
        {
            case Direction.Right:
                DriveTurn(-TurnAngleDegrees, cancellationToken);
                break;
            case Direction.Left:
                DriveTurn(TurnAngleDegrees, cancellationToken);
                break;
        }

        DriveTrack(ProbeStepMillimeters, cancellationToken);

        if (MedianDistance(0, 1) <= FrontBlockedThresholdMillimeters)
        {
            Console.WriteLine("Blocked during approach, recovering...");
            DriveTrack(RecoveryBacktrackMillimeters, cancellationToken);
            UndoTurn(direction, cancellationToken);
            return false;
        }

        if (!ConfirmMarkerDuringCrossing(cancellationToken))
        {
            Console.WriteLine("No valid marker confirmed, backing out...");
            DriveTrack(RecoveryBacktrackMillimeters, cancellationToken);
            UndoTurn(direction, cancellationToken);
            return false;
        }

        Console.WriteLine("Marker confirmed, crossing opening.");
        DriveTrack(MarkerCrossDistanceMillimeters, cancellationToken);
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
        DriveTrack(250, cancellationToken);
        Zumo.Instance.Drive.Stop();
    }

    private static void RecoverFromDeadEnd(CancellationToken cancellationToken)
    {
        Console.WriteLine("Dead end recovery: turn left.");
        DriveTurn(TurnAngleDegrees, cancellationToken);
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

    private static void DriveTrack(short length, CancellationToken cancellationToken)
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

    private static int MedianDistance(int centerAngle, int span)
    {
        List<int> values = new List<int>(span * 2 + 1);

        for (int offset = -span; offset <= span; offset++)
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
        return values[values.Count / 2];
    }

    private enum Direction
    {
        Right,
        Front,
        Left,
    }
}
