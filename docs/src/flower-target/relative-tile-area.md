# Relative Tile Area Flower

![image](../screenshots/flower-target-relative-tile-area.png)

This will cast the `Lyliac Plant` spell in an circular area relative to the character's current position.
The spell will be cast on the tiles in a clockwise order.

![image](../screenshots/tile-radius-example.png)

In the above example, the `Inner Radius` is set to **2**, and the `Outer Radius` is set to **4**.
The dead-zone is the area in the center of the circle, where no flower will be cast.
The solid blue tiles are the tiles that will be selected for casting flower, in clockwise order.

This can be useful in some instances, such as periodically casting flower on targets within an area without knowing their exact location.

## Options

- `Relative Tile` - the tile relative to your character's current position. This will be the center of the circular region.
- `Inner Radius` - the inner radius of the circular region. This is the dead-zone, where no flower will be cast.
- `Outer Radius` - the outer radius of the circular region. This is the maximum distance from the center where flower will be cast.
- `Mouse Offset X/Y` - offsets the mouse screen coordinates by the specified amount.
- `Flower Interval` - the time interval between casting flower on the same tile area.

**NOTE:** The `Flower Threshold` option is not available for this target type.
