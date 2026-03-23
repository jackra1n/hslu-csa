# LiDAR Integration Guide

This guide is focused on implementing and validating LiDAR parsing in this repository.

Primary implementation file:

- `ZumoTemplate/ZumoLib/Lidar/Lidar.cs`

Detailed protocol background:

- `docs/STL-19P_notes.md`

## Packet layout (47 bytes)

- Byte `0`: header (`0x54`)
- Byte `1`: ver_len (`0x2C`)
- Bytes `2..3`: speed (little-endian)
- Bytes `4..5`: start angle (little-endian, 0.01 deg units)
- Bytes `6..41`: 12 points x 3 bytes
  - distance LSB
  - distance MSB
  - intensity
- Bytes `42..43`: end angle
- Bytes `44..45`: timestamp
- Byte `46`: CRC8

## Parser state machine

Use a stream parser; do not assume serial read boundaries match frame boundaries.

Recommended flow:

1. Read bytes until first header candidate `0x54`.
2. Read next byte and require `0x2C`.
3. Read remaining `45` bytes to complete one frame.
4. Compute CRC over bytes `0..45`.
5. Compare computed CRC with byte `46`.
6. Parse frame fields.
7. Convert 12 measurements to per-angle storage.
8. On any error, resync from step 1.

## CRC rule

- Lookup-table CRC8 is provided in `Lidar.cs`.
- Compute over first `46` bytes.
- Expected value is byte `46`.

Equivalent validation pattern:

- Either `computed == frame[46]`, or
- Compute over all 47 bytes and expect `0`.

Use only one style in code to avoid confusion.

## Angle interpolation and wrap handling

Given raw angles in 0.01 degree units:

```text
start_deg = start_raw / 100.0
end_deg   = end_raw / 100.0
if end_deg < start_deg: end_deg += 360.0
step = (end_deg - start_deg) / 11.0
angle_i = start_deg + i * step
if angle_i >= 360.0: angle_i -= 360.0
```

- `i` runs from `0` to `11`.
- Map to integer index for `Points[0..359]` consistently (for example nearest-degree rounding).

## Suggested storage rules

- Keep latest valid sample for each integer degree.
- Store:
  - `Distance` in mm
  - `Intensity` raw 8-bit
- Optional filtering before storing:
  - distance `== 0`
  - very low intensity
  - out-of-range distance for your use case

## Threading and robustness notes

- `Run()` should be a long-lived background loop.
- Guard against `SerialPort` exceptions and recover cleanly.
- Keep parsing allocation-free in hot path where possible.
- Track health counters:
  - frames received
  - crc failures
  - sync losses

## Validation checklist

- Can parse continuous stream for multiple minutes without crash.
- CRC failures are detected and skipped.
- Angle wrap packets near `359 -> 0` are mapped correctly.
- `Speed` updates from frame bytes `2..3`.
- `Points[angle]` shows changing distances when objects move.

## Minimal test strategy

- Add a debug mode that prints every Nth valid frame summary:
  - speed
  - start/end angle
  - one selected point
- Add deterministic parser tests with synthetic frames:
  - valid frame
  - bad CRC
  - bad header
  - wrap-around angle case

## Common pitfalls

- Reading a partial frame and parsing it as complete.
- Forgetting wrap correction when `end < start`.
- Mixing little-endian parsing order.
- Overwriting points with invalid data after CRC failure.
