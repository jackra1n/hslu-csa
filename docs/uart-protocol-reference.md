# Zumo UART Protocol Reference

This document is the programming reference for commands sent to Zumo modules over UART.

## Message format

- Set command prefix: `5>`
- Get command prefix: `5?`
- Every command must end with line feed.
- General structure:
  - set: `5><module><subcommand><payload>`
  - get: `5?<module><subcommand>`

Example connectivity check:

- Send: `5>D1Ping`
- Reply: `5<D1Ping`

## Numeric encoding rules

- Hex payloads are ASCII-encoded.
- Signed 16-bit values use two's complement.
- Examples:
  - `005A` = `+90`
  - `FFA6` = `-90`

## Drive module (`0x24`)

### Move distance

- Command: `5>242SSSSVVVVAAAA`
- `SSSS`: distance (mm, signed)
- `VVVV`: speed (mm/s)
- `AAAA`: acceleration (mm/s^2)
- Positive distance: forward, negative: backward.
- Query remaining distance: `5?242`.

### Rotate in place

- Command: `5>24ASSSSVVVVAAAA`
- `SSSS`: angle (degrees, signed)
- Positive angle: clockwise, negative: counter-clockwise.
- Example: `5>24A005A03E803E8` rotates `+90` with speed `1000`, accel `1000`.

### Constant wheel speeds

- Command: `5>241LLLLRRRR`
- `LLLL` / `RRRR`: left/right wheel speed in mm/s (signed).

### Arc movement

- Command: `5>249SSSSRRRRVVVVAAAA`
- `SSSS`: arc angle (degrees, signed)
- `RRRR`: radius (mm)
- `VVVV`: speed (mm/s)
- `AAAA`: acceleration (mm/s^2)
- Positive angle: right curve, negative: left curve.

### Rotation calibration factor

- Set: `5>24BNNNN`
- Default is `130/100` (factor 1.3).
- Temporary neutral factor: `5>24B0064`.
- Formula:

```text
factor = (target_angle / measured_angle) * 100
```

### Readbacks

- Current target speed: `5?241` -> payload `LLLLRRRR`
- Remaining distance: `5?242` -> payload `SSSS`

## Quadrature encoder module (`0x22`)

- Read wheel speed: `5?220` -> payload `0LLLLRRRR`
- Read wheel distance: `5?221` -> payload `1LLLLRRRR`
- Reset distance counters: `5>220`
- Set distance factor: `5>221NNNN`
- Read distance factor: `5?222`

Calibration example from sheet:

- Command `5>22103B6` if 1000 mm target produced about 950 mm measured.

## RGB LED modules

- Front 8 LEDs module: `0x11`
- Top 2 LEDs module: `0x12`

### Set color

- Front: `5>11LLRRGGBB`
- Top: `5>12LLRRGGBB`
- `LL` is a LED bitmask.

Examples:

- All front LEDs red: `5>11FFFF0000`
- All front LEDs blue: `5>11FF0000FF`

### Turn off

- Front off: `5>11OF`
- Top off: `5>12OF`

### Read color

- Front query: `5?11LL`
- Top query: `5?12LL`
- Response payload: `LLRRGGBB`

## Color sensor module (`0x31`)

### Read hue

- Query: `5?310`
- Response payload: `HHHH` (hue angle in degrees).
- For low-saturation or dark signals, sensor can return an invalid sentinel value.

### Black/white calibration

- Command: `5>316XX`
- `XX=00`: capture black reference.
- `XX=01`: capture white reference and apply calibration.

## Sound module (`0x50`)

### Beep

- Command: `5>500FFFFTTTT`
- `FFFF`: frequency in Hz
- `TTTT`: duration in ms
- Example `440 Hz` for `1000 ms`: `5>50001B803E8`

### Play predefined melody

- Command: `5>501N`
- `N` is low nibble song index:
  - `0`: Knight Rider
  - `1`: Star Wars
  - `2`: Super Mario
  - `3`: Wasted
  - `4`: Harry Potter
  - `5`: Lord of the Rings
  - `6`: Indiana Jones
  - `7`: James Bond 007
  - `8`: Pacman

## Implementation notes

- Build command strings centrally to avoid format drift.
- Keep helpers for:
  - int16 <-> 4-char hex
  - line-feed termination
  - response parsing per module
- Add unit tests for two's complement conversion and command serialization.

## Common pitfalls

- Omitting line feed terminator.
- Treating signed fields as unsigned.
- Sending malformed payload length for subcommand.
- Not validating response prefixes before parsing payload.
