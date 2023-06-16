# Spell Macro Settings

![image](../screenshots/settings-spell-macros.png)

The `Spell Macro Settings` settings tab contains settings for spell macros.

## Default Spell Rotation Mode

This setting determines how the [Spell Queue](../main-window/spell-queue.md) will alternate casting of spells.
By default, this is `Round Robin`.

You can use `Singular Order` to cast spells in the order they are added to the queue, only moving to the next spell when the current spell reaches the desired level.

- `No Rotation` - Do not rotate spells, even when the spell has reached the target level.
- `Singular Order` - Rotate to the next spell once the current one has reached the target level.
- `Round Robin` - Rotate to the next spell after each cast.

**NOTE:** If the `Skip Spells on Cooldown` setting is enabled, it will temporarily skip that spell in the queue.

## Zero Line Delay

This setting determines the delay when casting zero-line (instant) spells.
By default, this is `0.2 seconds` (200 milliseconds).

## Single Line Delay

This setting determines the delay when casting one-line spells.
By default, this is `1 second`.

## Multiple Line Delay

This setting determines the delay when casting multi-line spells, representing the delay between each line.
By default, this is `1 second` per line.

## Auto Fas Spiorad

This setting determines whether the `fas spiorad` spell will be automatically cast for a character when they are below a certain mana threshold.
By default, this is `Disabled`.

**NOTE:** This still requires the character to be in the "macroing" state. No actions are taken when the macro is paused or stopped.

## Use Fas Spiorad When Insufficient Mana

This setting determines whether the `fas spiorad` spell will be cast when the character does not have enough mana to cast the spell they are macroing.
This also includes `Lyliac Plant` and `Lyliac Vineyard`.

By default, this is `Enabled` and usually a much better option than above.

## Require Mana for Casting Spells

This setting determines whether the character will check their mana before casting spells. If they do not have enough mana, they will wait until they do.
By default, this is `Enabled`.

It can be useful to disable this if you are macroing spells that are not defined in the spell metadata.

## Allow Automatic Staff Switching

This setting determines whether the character will automatically switch to the most appropriate staff when casting spells.
By default, this is `Enabled`.

You can disable this if you are getting stuck in a loop of switching staffs or do not want your character to switch equipment.

## Warn on Duplicate Spells in Casting Queue

This setting determines whether the application will warn you when you add a spell to the casting queue that is already in the queue.
By default, this is `Enabled`.

You are still prompted if you wish to override the warning and add it anyways.

## Skip Spells on Cooldown

This setting determines whether spells that are on cooldown will be temporarily skipped in the `Spell Queue`.
By default, this is `Enabled`.

Once the spell comes off cooldown, it will be cast again depending on spell rotation mode.
