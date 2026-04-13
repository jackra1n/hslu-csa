using ZumoLib;

namespace ZumoApp;

public class MazeRunner
{
    private const ushort MoveSpeed = 128;
    private const ushort MoveAcceleration = 256;

    private const short TurnAngle = 93;
    private const short CellSizeMm = 200;

    private const int FrontBlockedMm = 100;
    private const int SideOpenMm = 160;
    private const int ExitThresholdMm = 1250;

    private const int SectorHalfWidth = 15;
    private const int SectorStep = 2;
    private const int EmergencyStopSamples = 3;

    private const int MarkerSamples = 3;
    private static readonly TimeSpan MarkerCooldown = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan PostTurnSensorSettle = TimeSpan.FromMilliseconds(140);

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
            HandleInitialExit(cancellationToken);
            for (int step = 0; step < MaxSteps; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int front = GetSectorClearance(0, SectorHalfWidth);
                int right = GetSectorClearance(90, SectorHalfWidth);
                int left = GetSectorClearance(270, SectorHalfWidth);

                Console.WriteLine($"step={step:D3} front={front} right={right} left={left}");

                if (LooksLikeExit(front, right, left))
                {
                    HandleExit(cancellationToken);
                    return;
                }

                if (right > SideOpenMm)
                {
                    Console.WriteLine("Choice: right opening, turn +90 then one cell.");
                    SetStatusLed(100, 70, 0);
                    DriveTurn(TurnAngle, cancellationToken);
                    Thread.Sleep(PostTurnSensorSettle);
                    DriveOneCell(cancellationToken);
                    continue;
                }

                if (front > SideOpenMm)
                {
                    Console.WriteLine("Choice: forward one cell.");
                    SetStatusLed(100, 70, 0);
                    DriveOneCell(cancellationToken);
                    continue;
                }

                if (left > SideOpenMm)
                {
                    Console.WriteLine("Choice: left opening, turn -90 then one cell.");
                    SetStatusLed(100, 70, 0);
                    DriveTurn(-TurnAngle, cancellationToken);
                    Thread.Sleep(PostTurnSensorSettle);
                    DriveOneCell(cancellationToken);
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

    private void HandleInitialExit(CancellationToken cancellationToken)
    {
        int front = GetSectorClearance(0, SectorHalfWidth);
        int right = GetSectorClearance(90, SectorHalfWidth);
        int left = GetSectorClearance(270, SectorHalfWidth);

        Console.WriteLine($"step=INIT front={front} right={right} left={left}");
        Direction direction = ChooseInitialExitDirection(front, right, left);
        Console.WriteLine($"Initial exit: face {direction} (most open).");

        switch (direction)
        {
            case Direction.Right:
                DriveTurn(TurnAngle, cancellationToken);
                break;
            case Direction.Left:
                DriveTurn(-TurnAngle, cancellationToken);
                break;
            case Direction.Front:
                break;
        }

        if (direction != Direction.Front)
        {
            Thread.Sleep(PostTurnSensorSettle);
        }

        DriveOneCell(cancellationToken);
    }

    private static Direction ChooseInitialExitDirection(int front, int right, int left)
    {
        if (right >= left && right >= front)
        {
            return Direction.Right;
        }

        if (left >= front)
        {
            return Direction.Left;
        }

        return Direction.Front;
    }

    private void DriveOneCell(CancellationToken cancellationToken)
    {
        SetStatusLed(0, 95, 0);
        Console.WriteLine("Drive one cell: 200 mm.");
        DriveTrackWithSafety(CellSizeMm, cancellationToken);
        CheckForMarker();
        SetStatusLed(24, 24, 100);
    }

    private void CheckForMarker()
    {
        int streak = 0;
        for (int i = 0; i < 5; i++)
        {
            DetectedColor color = Zumo.Instance.ColorSensor.ReadDetectedColor();
            if (color is DetectedColor.Red or DetectedColor.Green)
            {
                streak++;
                if (streak >= MarkerSamples)
                {
                    TriggerMarkerTone();
                    return;
                }
            }
            else
            {
                streak = 0;
            }

            Thread.Sleep(30);
        }
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
        return front >= ExitThresholdMm && right >= ExitThresholdMm && left >= ExitThresholdMm;
    }

    private void HandleExit(CancellationToken cancellationToken)
    {
        Console.WriteLine("Exit detected.");
        Zumo.Instance.Sound.Play(SoundItem.SuperMario);
        DriveTrackRaw(250, cancellationToken);
        Zumo.Instance.Drive.Stop();
    }

    private void RecoverFromDeadEnd(CancellationToken cancellationToken)
    {
        Console.WriteLine("Dead end recovery: turn around.");
        DriveTurn(180, cancellationToken);
    }

    private void DriveTrackRaw(short length, CancellationToken cancellationToken)
    {
        if (!Zumo.Instance.Drive.DriveTrack(length, MoveSpeed, MoveAcceleration))
        {
            Console.WriteLine("DriveTrack command rejected.");
            return;
        }

        WaitDriveFinished(cancellationToken, monitorFrontSafety: false);
    }

    private bool DriveTrackWithSafety(short length, CancellationToken cancellationToken)
    {
        if (!Zumo.Instance.Drive.DriveTrack(length, MoveSpeed, MoveAcceleration))
        {
            Console.WriteLine("DriveTrack command rejected.");
            return false;
        }

        return WaitDriveFinished(cancellationToken, monitorFrontSafety: true);
    }

    private void DriveTurn(short angle, CancellationToken cancellationToken)
    {
        if (!Zumo.Instance.Drive.DriveTurn(angle, MoveSpeed, MoveAcceleration))
        {
            Console.WriteLine("DriveTurn command rejected.");
            return;
        }

        WaitDriveFinished(cancellationToken, monitorFrontSafety: false);
    }

    private bool WaitDriveFinished(CancellationToken cancellationToken, bool monitorFrontSafety)
    {
        DateTime timeout = DateTime.UtcNow.AddSeconds(6);
        int consecutiveBlocked = 0;
        bool stoppedBySafety = false;

        while (Zumo.Instance.Drive.DriveIsRunning())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (monitorFrontSafety)
            {
                int clearance = GetSectorClearance(0, SectorHalfWidth);
                if (clearance > 0 && clearance <= FrontBlockedMm)
                {
                    consecutiveBlocked++;
                    if (consecutiveBlocked >= EmergencyStopSamples)
                    {
                        Console.WriteLine($"Emergency stop: front clearance {clearance} mm");
                        Zumo.Instance.Drive.Stop();
                        stoppedBySafety = true;
                    }
                }
                else
                {
                    consecutiveBlocked = 0;
                }
            }

            if (DateTime.UtcNow > timeout)
            {
                Console.WriteLine("Drive wait timeout.");
                break;
            }

            Thread.Sleep(20);
        }

        return !stoppedBySafety;
    }

    private static int GetSectorClearance(int centerAngle, int halfWidth)
    {
        List<int> values = new List<int>((halfWidth * 2 / SectorStep) + 1);

        for (int offset = -halfWidth; offset <= halfWidth; offset += SectorStep)
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
