# STL-19P / LD19 Programming Notes

## 1. What matters most for programming

* The LiDAR sends data over **UART**
* Communication is **one-way only**
* You do **not** send commands to start scanning
* Once the motor is stable, it starts continuously streaming measurement packets
* Packet format is fixed and contains:

  * header
  * packet info / point count
  * speed
  * start angle
  * 12 measurement points
  * end angle
  * timestamp
  * CRC8  

---

## 2. Electrical / serial interface

### Connector pins

| Pin | Name | Direction | Meaning              |
| --- | ---- | --------: | -------------------- |
| 1   | TX   |    Output | LiDAR data output    |
| 2   | PWM  |     Input | Motor control signal |
| 3   | GND  |     Power | Ground               |
| 4   | P5V  |     Power | 5V supply            |

Notes:

* Supply voltage: **4.5V to 5.5V**, typical **5V**
* UART logic level is **3.3V**
* If you are **not** using external speed control, **PWM must be connected to GND**.  

### UART settings

* **Baud rate:** `230400`
* **Data bits:** `8`
* **Stop bits:** `1`
* **Parity:** none
* **Flow control:** none  

---

## 3. Basic device behavior

* Default scan speed is about **10 Hz**
* The STL-19P performs about **5000 measurements per second**
* The older LD19 manual says **4500 measurements/s**; for STL-19P-specific code, use the STL-19P docs as the main source.   

### Startup timing

After power-on:

* communication system init: about **500 ms**
* angle/ranging init completes after about **200 ms more**
* then valid ranging data is streamed 

---

## 4. Packet format

Each packet is:

| Field       |         Size | Notes                                                |
| ----------- | -----------: | ---------------------------------------------------- |
| Header      |       1 byte | fixed `0x54`                                         |
| VerLen      |       1 byte | fixed `0x2C`                                         |
| Speed       |      2 bytes | little-endian, unit = degrees/sec                    |
| Start Angle |      2 bytes | little-endian, unit = `0.01°`                        |
| Data        | 12 × 3 bytes | each point = distance LSB + distance MSB + intensity |
| End Angle   |      2 bytes | little-endian, unit = `0.01°`                        |
| Timestamp   |      2 bytes | little-endian, unit = ms, wraps at `30000`           |
| CRC8        |       1 byte | CRC of all previous bytes in the frame               |

So the total frame size is:

* `1 + 1 + 2 + 2 + (12 * 3) + 2 + 2 + 1 = 47 bytes`  

### Constants

```c
#define HEADER 0x54
#define POINT_PER_PACK 12
#define VERLEN 0x2C
#define FRAME_SIZE 47
```

The manuals show the same struct layout:

```c
typedef struct __attribute__((packed)) {
    uint16_t distance;
    uint8_t intensity;
} LidarPointStructDef;

typedef struct __attribute__((packed)) {
    uint8_t header;
    uint8_t ver_len;
    uint16_t speed;
    uint16_t start_angle;
    LidarPointStructDef point[12];
    uint16_t end_angle;
    uint16_t timestamp;
    uint8_t crc8;
} LiDARFrameTypeDef;
```

 

---

## 5. Meaning of each measurement point

Each point uses **3 bytes**:

* `distance_lsb`
* `distance_msb`
* `intensity`

### Distance

* 16-bit little-endian
* unit: **mm**

### Intensity

* 8-bit signal strength / confidence-like value
* higher value = stronger return
* LD19 manual says a white object within 6m typically gives intensity around **200** 

---

## 6. Angle calculation

The packet only gives:

* `start_angle`
* `end_angle`

You compute the 12 individual point angles by **linear interpolation**.

### Formula

```text
step = (end_angle - start_angle) / (len - 1)
angle_i = start_angle + step * i
```

Where:

* `len = 12`
* `i` is from `0` to `11`
* angle unit before conversion is `0.01°` if you use raw values from the packet 

### Practical version

```text
start_deg = start_angle_raw / 100.0
end_deg   = end_angle_raw / 100.0
step      = (end_deg - start_deg) / 11.0
angle_i   = start_deg + step * i
```

### Important wrap-around note

If a packet crosses `360° -> 0°`, then `end_angle` can look smaller than `start_angle`.
In that case, correct it like this:

```text
if end_deg < start_deg:
    end_deg += 360.0
```

And after computing:

```text
if angle_i >= 360.0:
    angle_i -= 360.0
```

The manuals do not explicitly spell out the wrap fix, but it is the necessary interpretation of their interpolation formula for circular data.

---

## 7. Coordinate system

The sensor uses a **left-handed coordinate system**:

* origin = rotation center
* front of sensor = **0°**
* angle increases **clockwise**  

### Practical consequence

If you convert to Cartesian:

```text
x = r * cos(theta)
y = r * sin(theta)
```

be careful: many robotics/math stacks assume **counter-clockwise** angles.
So depending on your coordinate convention, you may need to negate the angle or flip `y`.

A common safe approach is:

```text
theta_math = -theta_lidar
x = r * cos(theta_math)
y = r * sin(theta_math)
```

if your app expects standard mathematical CCW angles.

---

## 8. CRC8 validation

The frame ends with a **CRC8** byte.
The manuals provide a 256-byte lookup table and this algorithm:

```c
uint8_t CalCRC8(uint8_t *p, uint8_t len){
    uint8_t crc = 0;
    uint16_t i;
    for (i = 0; i < len; i++){
        crc = CrcTable[(crc ^ *p++) & 0xff];
    }
    return crc;
}
```

You calculate the CRC over **all bytes before the CRC byte**.  

### Practical validation rule

For a 47-byte frame:

* bytes `0..45` are input to CRC
* byte `46` is expected CRC

---

## 9. Recommended parser logic

### Frame sync

Because UART is a byte stream, do not assume reads align with packets.

Recommended parser flow:

1. Read bytes continuously
2. Search for header byte `0x54`
3. Check next byte is `0x2C`
4. Read remaining bytes until full 47-byte frame
5. Validate CRC
6. Parse fields
7. Emit 12 points

### Good defensive checks

Reject frame if:

* header != `0x54`
* ver_len != `0x2C`
* CRC mismatch
* absurd angle/speed values
* impossible distance if your app wants sanity filtering

---

## 10. Minimal field parsing example

### Little-endian helpers

```text
u16 = low_byte | (high_byte << 8)
```

### Offsets

```text
0   header
1   ver_len
2   speed_lsb
3   speed_msb
4   start_angle_lsb
5   start_angle_msb
6..41   12 points × 3 bytes
42  end_angle_lsb
43  end_angle_msb
44  timestamp_lsb
45  timestamp_msb
46  crc8
```

### Point layout

For point `i`:

```text
base = 6 + i * 3
distance  = frame[base] | (frame[base + 1] << 8)
intensity = frame[base + 2]
```

---

## 11. Example values from the manual

Example packet from the LD19 manual:

```text
54 2C 68 08 AB 7E E0 00 E4 DC 00 E2 D9 00 E5 D5 00 E3 D3 00 E4 D0 00 E9 CD 00 E4 CA 00 E2 C7 00 E9 C5 00 E5 C2 00 E5 C0 00 E5 BE 82 3A 1A 50
```

Parsed:

* speed = `0x0868` = **2152 deg/s**
* start angle = `0x7EAB` = **324.27°**
* end angle = `0x82BE` = **334.70°**
* point 1 distance = `0x00E0` = **224 mm**
* point 1 intensity = `0xE4` = **228** 

---

## 12. Performance numbers that may matter in software

Useful app-level expectations:

* range:

  * **0.03–12 m** on white target (~80% reflectivity)
  * **0.03–8 m** on black target (~4% reflectivity)
* scan frequency:

  * typical **10 Hz**
  * about **6–13 Hz** overall depending on control
* angular resolution:

  * about **0.72° at 10 Hz**
* anti-ambient-light:

  * up to **60 kLux** 

### Accuracy

* `±10 mm` for `0.03–0.5 m`
* `±20 mm` for `0.5–2 m`
* `±30 mm` for `2–12 m` on white target
* `±30 mm` for `2–8 m` on black target 

---

## 13. PWM / motor control notes

For pure data reading, you can usually ignore PWM and just:

* supply 5V
* connect GND
* read TX
* keep PWM grounded

For external motor control, the documents are a bit inconsistent:

* **STL-19P datasheet:** PWM control frequency listed as **20–50 kHz**, typical **30 kHz** 
* **FHL-LD19P development manual:** describes multiple mode-switch frequencies like `0.5–1.5 kHz`, `3–5 kHz`, `6–8 kHz` for external/internal/standby switching 
* **LD19 manual:** external speed control trigger is **20–50 kHz**, recommended **30 kHz** 

### Practical advice

For software integration, the safest assumption is:

* **If you do not need motor control, ground PWM**
* Only implement external PWM control if you specifically test your exact hardware revision

---

## 14. Linux / ROS / ROS2 notes

### Raw serial access

On Linux, the docs tell you to check the device and set permissions:

```bash
ls /dev/ttyUSB*
sudo chmod 777 /dev/ttyUSB0
```

 

### ROS repositories mentioned in the docs

ROS:

```bash
git clone https://github.com/DFRobotdl/ldlidar_stl_ros.git
```

ROS2:

```bash
git clone https://github.com/DFRobotdl/ldlidar_stl_ros2.git
```

SDK:

```bash
git clone https://github.com/DFRobotdl/ldlidar_stl_sdk.git
```

These links are listed in the LD19 manual. 

---

## 15. Things I would implement first

### Minimum viable parser

1. open UART at `230400 8N1`
2. read stream
3. find `0x54 0x2C`
4. read 47-byte frame
5. CRC check
6. parse 12 points
7. interpolate angles
8. optionally convert to `(x, y)`

### Nice extras

* drop invalid frames
* smooth speed estimate from `speed`
* reject distance `0`
* filter low intensity points
* detect angle wrap-around cleanly
* publish as ROS LaserScan / PointCloud2 if needed

---

## 16. Biggest gotchas

* **Don’t wait for a command protocol** — there isn’t one for normal operation; it just streams. 
* **PWM must be grounded** if unused. 
* **Angles are clockwise**, not the usual math convention. 
* **Verify CRC** or you will eventually parse garbage.
* **Handle angle wrap-around** near 360°.
* Some docs mix **LD19** and **STL-19P** naming; protocol looks essentially the same, but hardware-control details can differ a bit between manuals.
