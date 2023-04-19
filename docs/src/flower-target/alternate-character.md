# Alternate Character Flower

![image](../screenshots/flower-target-character.png)

This will cast the `Lyliac Plant` spell on another character logged in on the same computer.

**NOTE:** The other character must be within visible range of the character casting flower.

## Options

- `Character` - specifies the other character you wish to cast flower on.
- `Mouse Offset X/Y` - offsets the mouse screen coordinates by the specified amount.
- `Flower Interval` - the time interval between casting flower on the same character.
- `Flower Threshold` - the minimum mana percentage of the character before flower will be cast on them.

**NOTE:** The `Flower Interval` and `Flower Threshold` conditions are evaluated independent of each other.
They act as a logical `OR` condition, where **either** condition can be met to cast flower.
