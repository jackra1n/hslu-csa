# Maze Task Spec (Engineering Version)

Source: `TestataufgabeFS256.pdf`

This file translates the assignment into implementation-ready requirements and tests.

## Objective

Robot starts near maze center and must autonomously find an exit without touching walls.

## Functional requirements

- Navigate maze autonomously using onboard sensing (LiDAR and color sensor).
- Only pass openings that are marked on the floor:
  - red (inner ring), or
  - green (outer ring).
- Play a tone when crossing a colored marker.
- Play a melody when leaving the maze.
- Use front RGB LEDs during run for status indication (exact semantics are free choice).

## Environment constraints

- One wall element length: approximately `200 mm`.
- Gap between wall elements can be up to `30 mm`.
- There are multiple valid routes to exit.
- Robot must avoid wall contact throughout run.

## Non-functional requirements

- Behavior should be deterministic enough for repeat demo runs.
- Control loop must react to near obstacles with sufficient margin.
- Sensor processing should tolerate noisy frames and short invalid bursts.

## Recommended software decomposition

- `Perception`
  - LiDAR sector extraction (front/left/right clearance)
  - Color marker detection (red/green + invalid handling)
- `Decision`
  - finite state machine for maze navigation
  - opening qualification by color marker
- `Motion`
  - drive and rotate primitives via UART `0x24`
- `Feedback`
  - RGB LED status patterns
  - sound cues via UART `0x50`

## Suggested runtime states

- `Searching`: scan and move toward candidate opening.
- `Aligning`: orient robot to opening centerline.
- `CrossingMarker`: move across red/green marker and beep.
- `TraversingSegment`: controlled forward motion with wall clearance checks.
- `ExitDetected`: play melody and stop.
- `Recovery`: backup/reorient when blocked or confidence drops.

## Acceptance checklist

- Start orientation and position satisfy assignment start condition.
- Robot exits maze autonomously.
- Robot does not touch walls.
- Tone is emitted when marker is crossed.
- Melody is emitted on exit.
- RGB LEDs are actively used during navigation.

## Test plan

- Dry run with static logs only (no movement) to verify perception outputs.
- Low-speed run to validate obstacle and opening detection.
- Marker test for red and green detection with beep trigger.
- Full maze run repeated multiple times.
- Stress run with slight start-position perturbations.

## Milestones from sheet

- Demo: `13 April 2026`
- Submission deadline: `19 April 2026`

## Common pitfalls

- Overfitting to one exact start pose.
- Using fixed delays instead of sensor-driven transitions.
- Missing debounce/confirmation logic for marker detection.
- No fallback behavior when path confidence drops.
