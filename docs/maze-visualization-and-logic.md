# Zumo Robot Labyrinth: 2D Logic Map

This document outlines the 11x11 grid system used to represent the physical Labyrinth for the Zumo Robot's navigation logic. 

## The Grid Representation

The labyrinth is mapped to an `11x11` matrix. In this layout, the colored floor markers (Red, Green, Gray) are placed on the path *just before* the robot would pass through a wall opening.

| Symbol | Meaning | Description |
|--------|---------|-------------|
| v | Start Position | "The robot begins here at coordinate (5, 5), facing South (downwards). It is surrounded by walls on 3 sides." |
| # | Wall | Impassable boundary. The robot's Lidar should detect this as a collision hazard. |
| . | Open Path | Passable floor. Safe for the robot to drive on. |
| R | Red Marker | Indicates an open gate to the outer ring. The robot is allowed to pass through the wall adjacent to this marker. |
| G | Green Marker | Indicates the final exit. The robot must find and pass through this marker to escape the labyrinth. |
| O | Unmarked / Gray | Indicates a closed gate or false path. The robot must not pass through the wall adjacent to this marker and should treat it as a dead end. |

```text
#  .  #  #  #  #  #  #  #  #  #
#  G  .  .  .  .  .  .  .  O  .
#  .  #  #  #  .  #  #  #  .  #
#  .  #  .  .  O  .  .  #  .  #
#  .  #  .  #  #  #  .  #  .  #
#  .  .  O  #  v  #  R  .  .  #
#  .  #  .  #  .  #  .  #  .  #
#  .  #  .  .  O  .  .  #  .  #
#  .  #  #  #  .  #  #  #  .  #
.  O  .  .  .  .  .  .  .  O  #
#  #  #  #  #  #  #  #  #  .  #
```

Different example:
```text
#  .  #  #  #  #  #  #  #  #  #
#  O  .  .  .  .  .  .  .  O  .
#  .  #  #  #  .  #  #  #  .  #
#  .  #  .  .  R  .  .  #  .  #
#  .  #  .  #  #  #  .  #  .  #
#  .  .  O  #  v  #  O  .  .  #
#  .  #  .  #  .  #  .  #  .  #
#  .  #  .  .  .  .  .  #  .  #
#  .  #  #  #  #  #  #  #  .  #
.  G  .  .  .  .  .  .  .  O  #
#  #  #  #  #  #  #  #  #  .  #
```