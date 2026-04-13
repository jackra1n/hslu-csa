using ZumoLib;

namespace ZumoApp;

public class MazeRunner
{
    private const ushort MoveSpeed = 128;
    private const ushort MoveAcceleration = 256;

    private const short TurnAngle = 94;
    private const short CellSizeMm = 230;

    private const int FrontBlockedMm = 130;
    private const int SideOpenMm = 160;
    private const int ExitThresholdMm = 1250;

    private const int SectorHalfWidth = 15;
    private const int SectorStep = 2;
    private const int EmergencyStopSamples = 3;

    private const int MarkerSamples = 2;
    private static readonly TimeSpan MarkerCooldown = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan PostTurnSensorSettle = TimeSpan.FromMilliseconds(140);

    private const int AlignOffsetDeg = 20;
    private const double AlignMinCorrectionDeg = 1.0;
    private const double AlignMaxCorrectionDeg = 15.0;
    private const int AlignMaxWallDistMm = 300;

    private const int SideTooCloseMm = 80;
    private const int SideDriftMinDiffMm = 30;
    private const int MaxMidDriveCorrections = 2;

    private const int MaxSteps = 320;

    private DateTime _lastMarkerTriggerAt = DateTime.MinValue;

    private enum Heading { North, East, South, West }

    private Heading _heading = Heading.South;
    private int _posX;
    private int _posY;
    private readonly Dictionary<(int, int), int> _visitCount = new();
    private bool _lastMarkerDetected;

    public void Run(CancellationToken cancellationToken, bool lidarPrewarmed = false)
    {
        Console.WriteLine("Maze run started.");
        SetStatusLed(24, 24, 100);
        if (!lidarPrewarmed)
        {
            Zumo.Instance.Lidar.SetPower(true);
            Thread.Sleep(2200);
        }

        RecordVisit();

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

                Heading rightH = RotateRight(_heading);
                Heading leftH = RotateLeft(_heading);

                var options = new List<(Direction dir, int visits)>();
                if (right > SideOpenMm)
                    options.Add((Direction.Right, GetVisits(CellInDirection(rightH))));
                if (front > SideOpenMm)
                    options.Add((Direction.Front, GetVisits(CellInDirection(_heading))));
                if (left > SideOpenMm)
                    options.Add((Direction.Left, GetVisits(CellInDirection(leftH))));

                if (options.Count > 0)
                {
                    (Direction dir, int visits) best;
                    if (_lastMarkerDetected)
                    {
                        best = options
                            .OrderByDescending(o =>
                            {
                                var (cx, cy) = CellInDirection(HeadingForDirection(o.dir));
                                return DistanceFromOrigin(cx, cy);
                            })
                            .First();
                        Console.WriteLine($"Marker bias: choosing {best.dir} (outward)");
                    }
                    else
                    {
                        best = options.OrderBy(o => o.visits).First();
                        Console.WriteLine($"Choice: {best.dir} (target visits={best.visits})");
                    }

                    SetStatusLed(100, 70, 0);
                    ExecuteDirection(best.dir, cancellationToken);
                }
                else
                {
                    RecoverFromDeadEnd(cancellationToken);
                }
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

        ExecuteDirection(direction, cancellationToken);
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

    private void ExecuteDirection(Direction dir, CancellationToken cancellationToken)
    {
        switch (dir)
        {
            case Direction.Right:
                DriveTurn(TurnAngle, cancellationToken);
                _heading = RotateRight(_heading);
                break;
            case Direction.Left:
                DriveTurn(-TurnAngle, cancellationToken);
                _heading = RotateLeft(_heading);
                break;
        }

        if (dir != Direction.Front)
        {
            Thread.Sleep(PostTurnSensorSettle);
            AlignToWall(cancellationToken);
            Thread.Sleep(PostTurnSensorSettle);
        }

        DriveOneCell(cancellationToken);
        AdvancePosition();
    }

    private void DriveOneCell(CancellationToken cancellationToken)
    {
        SetStatusLed(0, 95, 0);
        Console.WriteLine("Drive one cell: 200 mm.");
        DriveTrackWithSafety(CellSizeMm, cancellationToken);
        _lastMarkerDetected = CheckForMarker();
        SetStatusLed(24, 24, 100);
    }

    private bool CheckForMarker()
    {
        int streak = 0;
        for (int i = 0; i < 5; i++)
        {
            DetectedColor color = Zumo.Instance.ColorSensor.ReadDetectedColor();
            if (color is DetectedColor.Red or DetectedColor.Green)
            {
                Console.WriteLine($"Marker detected: {color}");
                streak++;
                if (streak >= MarkerSamples)
                {
                    TriggerMarkerTone();
                    return true;
                }
            }
            else
            {
                streak = 0;
            }

            Thread.Sleep(30);
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
        //Zumo.Instance.Sound.Beep(1500, 120);
        Zumo.Instance.Sound.PlayRandomSound();
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
        _heading = Reverse(_heading);
    }

    private double? ComputeWallError(int centerAngle, double tanDelta)
    {
        int d1 = MedianLidarReading((centerAngle - AlignOffsetDeg + 360) % 360, 3, 20);
        int d2 = MedianLidarReading((centerAngle + AlignOffsetDeg) % 360, 3, 20);
        if (d1 <= 0 || d2 <= 0) return null;
        double err = Math.Atan((d1 - d2) / ((double)(d1 + d2) * tanDelta)) * 180.0 / Math.PI;
        Console.WriteLine($"Align @{centerAngle}: d1={d1} d2={d2} err={err:F1}deg");
        return err;
    }

    private void AlignToWall(CancellationToken cancellationToken)
    {
        int rightCenter = Zumo.Instance.Lidar[90].Distance;
        int leftCenter = Zumo.Instance.Lidar[270].Distance;
        int backCenter = Zumo.Instance.Lidar[180].Distance;

        double tanDelta = Math.Tan(AlignOffsetDeg * Math.PI / 180.0);
        var estimates = new List<double>(3);

        if (rightCenter > 0 && rightCenter <= AlignMaxWallDistMm)
        {
            double? err = ComputeWallError(90, tanDelta);
            if (err.HasValue) estimates.Add(err.Value);
        }

        if (leftCenter > 0 && leftCenter <= AlignMaxWallDistMm)
        {
            double? err = ComputeWallError(270, tanDelta);
            if (err.HasValue) estimates.Add(err.Value);
        }

        if (backCenter > 0 && backCenter <= AlignMaxWallDistMm)
        {
            double? err = ComputeWallError(180, tanDelta);
            if (err.HasValue) estimates.Add(err.Value);
        }

        if (estimates.Count == 0)
        {
            Console.WriteLine($"Align skip: no wall (R={rightCenter} L={leftCenter} B={backCenter})");
            return;
        }

        if (estimates.Count >= 2)
        {
            double maxSpread = estimates.Max() - estimates.Min();
            if (maxSpread > 10.0)
            {
                Console.WriteLine($"Align skip: walls disagree (spread={maxSpread:F1}deg)");
                return;
            }
        }

        double epsilonDeg = estimates.Average();

        if (Math.Abs(epsilonDeg) < AlignMinCorrectionDeg)
            return;

        if (Math.Abs(epsilonDeg) > AlignMaxCorrectionDeg)
        {
            Console.WriteLine($"Align skip: correction too large ({epsilonDeg:F1}deg)");
            return;
        }

        short correction = (short)Math.Round(epsilonDeg);
        Console.WriteLine($"Align correct: {correction}deg (from {estimates.Count} wall(s))");
        DriveTurn(correction, cancellationToken);
    }

    private static Heading RotateRight(Heading h) => (Heading)(((int)h + 1) % 4);
    private static Heading RotateLeft(Heading h) => (Heading)(((int)h + 3) % 4);
    private static Heading Reverse(Heading h) => (Heading)(((int)h + 2) % 4);

    private Heading HeadingForDirection(Direction dir) => dir switch
    {
        Direction.Right => RotateRight(_heading),
        Direction.Left => RotateLeft(_heading),
        _ => _heading,
    };

    private static int DistanceFromOrigin(int x, int y) => Math.Max(Math.Abs(x), Math.Abs(y));

    private (int x, int y) CellInDirection(Heading h) => h switch
    {
        Heading.North => (_posX, _posY - 1),
        Heading.South => (_posX, _posY + 1),
        Heading.East => (_posX + 1, _posY),
        Heading.West => (_posX - 1, _posY),
        _ => (_posX, _posY),
    };

    private int GetVisits((int x, int y) cell) => _visitCount.GetValueOrDefault(cell);

    private void RecordVisit()
    {
        var key = (_posX, _posY);
        _visitCount[key] = _visitCount.GetValueOrDefault(key) + 1;
    }

    private void AdvancePosition()
    {
        (_posX, _posY) = CellInDirection(_heading);
        RecordVisit();
        Console.WriteLine($"Position: ({_posX},{_posY}) heading={_heading} visits={GetVisits((_posX, _posY))}");
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

        return WaitDriveFinished(cancellationToken, monitorFrontSafety: true,
                                midDriveCorrectionsLeft: MaxMidDriveCorrections);
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

    private bool WaitDriveFinished(CancellationToken cancellationToken, bool monitorFrontSafety,
                                   int midDriveCorrectionsLeft = 0)
    {
        DateTime timeout = DateTime.UtcNow.AddSeconds(6);
        int consecutiveBlocked = 0;
        int consecutiveDrift = 0;
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

                if (!stoppedBySafety && midDriveCorrectionsLeft > 0)
                {
                    int rDist = Zumo.Instance.Lidar[90].Distance;
                    int lDist = Zumo.Instance.Lidar[270].Distance;
                    bool rClose = rDist > 0 && rDist < SideTooCloseMm;
                    bool lClose = lDist > 0 && lDist < SideTooCloseMm;

                    if ((rClose || lClose) && Math.Abs(rDist - lDist) > SideDriftMinDiffMm)
                    {
                        consecutiveDrift++;
                        if (consecutiveDrift >= 2)
                        {
                            Zumo.Instance.Drive.Stop();
                            short remaining = Zumo.Instance.Drive.GetRemainingDistance();
                            Console.WriteLine($"Drift stop: R={rDist} L={lDist} remaining={remaining}mm");
                            AlignToWall(cancellationToken);
                            if (remaining > 20)
                            {
                                Zumo.Instance.Drive.DriveTrack(remaining, MoveSpeed, MoveAcceleration);
                                midDriveCorrectionsLeft--;
                                consecutiveDrift = 0;
                                consecutiveBlocked = 0;
                                timeout = DateTime.UtcNow.AddSeconds(6);
                                continue;
                            }
                            break;
                        }
                    }
                    else
                    {
                        consecutiveDrift = 0;
                    }
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

    private static int MedianLidarReading(int angle, int samples, int delayMs)
    {
        var vals = new List<int>(samples);
        for (int i = 0; i < samples; i++)
        {
            int d = Zumo.Instance.Lidar[angle].Distance;
            if (d > 0) vals.Add(d);
            if (i < samples - 1) Thread.Sleep(delayMs);
        }
        if (vals.Count == 0) return 0;
        vals.Sort();
        return vals[vals.Count / 2];
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
