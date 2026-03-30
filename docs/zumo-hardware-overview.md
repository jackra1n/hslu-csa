# Zumo Hardware Overview (Programming Focus)

This file is a quick map of hardware modules, protocol addresses, and project integration points.

## Runtime architecture

- App logic lives in `ZumoTemplate/ZumoApp/Program.cs`.
- Hardware access lives in `ZumoTemplate/ZumoLib`.
- `ZumoTemplate/ZumoLib/Zumo.cs` is the singleton entry point for board devices.
- Low-level commands to robot modules are sent over UART with ASCII hex protocol.

## CM4 board GPIO

- User LED: GPIO pin `18` (output).
- User button: GPIO pin `27` (input, active-low).
- Current wrappers:
  - `ZumoTemplate/ZumoLib/Cm4Led/Cm4Led.cs`
  - `ZumoTemplate/ZumoLib/Cm4Button/Cm4Button.cs`

## UART dispatcher map

These are the module IDs used after `5>` (set) or `5?` (get):

- Drive module: `0x24`
- Quadrature encoders: `0x22`
- Front RGB LEDs (8x): `0x11`
- Top RGB LEDs (2x): `0x12`
- Color sensor: `0x31`
- Sound: `0x50`

Use `docs/uart-protocol-reference.md` for command-level details.

## LiDAR integration points

- Parser implementation target: `ZumoTemplate/ZumoLib/Lidar/Lidar.cs`
- Point model: `ZumoTemplate/ZumoLib/Lidar/LidarPoint.cs`
- Expected serial framing: header `0x54`, ver_len `0x2C`, `47` bytes per frame.
- Full parser guidance: `docs/lidar-integration-guide.md`.
- Datasheet-level detail: `docs/STL-19P_notes.md`.

## Data conventions that affect code

- UART commands must end with line feed.
- Many payload fields are signed 16-bit two's complement values encoded in hex.
- Drive module also supports an offset-enabled move command (`5>24C...OO`) for drift correction.
- LiDAR distance unit is mm.
- LiDAR angles are clockwise and require wrap-around handling near `360` degrees.

## Common pitfalls

- Mixing unsigned and signed payload parsing for motion commands.
- Forgetting to append line feed when sending UART commands.
- Assuming LiDAR packet reads are aligned to frame boundaries.
- Ignoring angle wrap-around and CRC validation.
