# Color Sensor Calibration and Detection

Sources:

- `CSA-Lab-Teil2v2.pdf`
- `Farbmuster.pdf`

This guide defines a practical approach to detect maze floor markers (red/green) robustly.

## Sensor protocol

- Module dispatcher: `0x31`
- Read hue command: `5?310`
- Response payload: `HHHH` (hue in degrees)
- Calibration command: `5>316XX`
  - `XX=00`: store black reference
  - `XX=01`: store white reference and apply calibration

## Hue references from provided pattern

- Red around `0` degrees (also near `360`)
- Yellow around `60` degrees
- Green around `120` degrees
- Blue around `240` degrees

For task logic we only need robust red/green classification.

## Recommended calibration workflow

1. Place sensor over black reference.
2. Send `5>31600`.
3. Place sensor over white reference.
4. Send `5>31601`.
5. Verify hue readings on known red and green samples.

Perform this at run-start or once per environment session.

## Suggested classification windows

Use angular windows with margin and wrap handling:

- Red window: `[340..359] U [0..20]`
- Green window: `[90..150]`

Tune widths empirically on your floor material and ambient light.

## Debounce strategy

- Sample hue periodically (for example every 20-50 ms).
- Require N consecutive samples in target window before triggering event (for example `N=3..5`).
- Add cooldown interval after trigger to avoid duplicate beeps on the same marker.

## Handling invalid or unstable readings

- Treat documented invalid sentinel as `Unknown`.
- Also classify as `Unknown` when short-term variance is high.
- Keep a simple confidence score for color decisions.
- Never change high-level state based on a single sample.

## Integration suggestions

- Emit tone via sound module on confirmed marker crossing.
- Update front RGB LEDs to visualize current color classification.
- Log hue stream and final classification decisions during tuning.

## Minimal pseudocode

```text
if hue is invalid:
    class = Unknown
else if hue in red window (with wrap):
    class = Red
else if hue in green window:
    class = Green
else:
    class = Other

apply consecutive-sample debounce
apply trigger cooldown
```

## Common pitfalls

- Ignoring red wrap-around near 0/360 degrees.
- Triggering actions from single noisy hue samples.
- Not recalibrating when lighting/floor conditions change.
- Using too narrow windows that fail in real runs.
