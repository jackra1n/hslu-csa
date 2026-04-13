# Zumo Robot Labyrinth: 2D Logic Map

This document outlines the 11x11 grid system used to represent the physical Labyrinth for the Zumo Robot's navigation logic. 

## The Grid Representation

The labyrinth is mapped to an `11x11` matrix. In this layout, the colored floor markers (Red, Green, Gray) are placed on the path *just before* the robot would pass through a wall opening.

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