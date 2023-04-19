# Getting Started

So you just installed SleepHunter, and you're ready to start using it. Now what?

One tip is that most of the user interface supports tooltips.
If you hover your mouse over a control, you will see information appear with a description of what it does.

See the `Main Window` section in the sidebar for more information.

## How do I macro a skill?

You can macro any number of skills at the same time, simply double-click on the skill you wish to macro as part of your rotation.

The skill will be highlighted in white as well as a colorful box around it, indicating that is is "enabled".

Once you have enabled all the skills you wish to macro, you can start the macro by clicking the `Start Macroing` button in the toolbar.
The selected character will then begin to macro the skills you have enabled.

You can toggle any skill on or off at any time by double-clicking on it, even while the macro is running.

See more information about the [Skills Tab](./main-window/skills-tab.md).

## How do I macro a spell?

You can macro a spell by double-clicking on the spell you wish to macro as part of your `Spell Queue`.
You will get a popup dialog asking you to select the target for the spell, as well as the maximum level you wish to cast the spell to.

You can leave the `Offset X/Y` to zero, as that is only needed for fine-tuning the mouse coordinates on non-standard targets.

Once you add a spell to the queue, you will see it appear on the right sidebar. You can add as many spells as you wish to the queue.
The spell queue supports drag and drop for re-ordering.

Once you have added all the spells you wish to macro, you can start the macro by clicking the `Start Macroing` button in the toolbar.

You can add and remove spells from the queue at any time, even while the macro is running.

See more information about the [Spell Queue](./main-window/spell-queue.md).

## How do I flower another character? (Lyliac Plant)

You can cast `Lyliac Plant` (flower) on another character by adding a new flower target at the bottom of the `Flowering` tab.
Similar to the spell macro, you will get a popup dialog asking you to select the recipient for `Lyliac Plant`, as well as some logic parameters.

You can leave the `Offset X/Y` to zero, as that is only needed for fine-tuning the mouse coordinates on non-standard targets.

Once you add a flower target, you will set it appear in the `Flowering` tab queue. You can add as many flower targets as you wish.
The flower queue supports drag and drop for re-ordering.

Once you have added all the flower targets you wish to macro, you can start the macro by clicking the `Start Macroing` button in the toolbar.

You can add and remove flower targets from the queue at any time, even while the macro is running.

See more information about [Flowering](./main-window/flowering-tab.md).

## How do I flower my entire group? (Lyliac Vineyard)

You can cast `Lyliac Vineyard` for your entire group by enabling the option at the top of the `Flowering` tab.
This will automatically cast `Lyliac Plant` on all other characters in your group, when it is available on cooldown.

You can enable this option at any time, even while individually casting `Lyliac Plant` on other characters.

Remember, you must click the `Start Macroing` button in the toolbar to start the process.

## Can I use all of these together?

Yes, absolutely! You can macro skills, spells, and flower your entire group at the same time.
SleepHunter has been designed to support all of these features at the same time and will automatically handle the timing of each macro.

It will also switch staves and cast `fas spiorad` as needed to regain mana.

## How do I bind hotkeys to a character?

You can assign custom hotkeys to each of your characters by selecting them in SleepHunter and pressing the hotkey combination you wish to use.
This will toggle their macro state when you press the hotkey, even if the main SleepHunter window is not in focus.

To remove the hotkey, simply press the `Delete` or `Backspace` key while the character is selected.
If you assign the same hotkey to another character, the previous character will have the hotkey unbound.

There is a visual indicator on the character list item to show if a hotkey is assigned.

## Do I have to redo this every time I start SleepHunter?

SleepHunter will automatically save the state of your macroing when you close the application, including hotkeys.
This means that when you start SleepHunter again, it will automatically restore the state of your macroing for that character.

**NOTE:** This does not mean that SleepHunter will automatically start macroing when you start the application, only that it will restore the state of your character's macro when you close the application.

## What about the Dojo?

By default SleepHunter is configured to stop macroing once a character's map changes.
This is to prevent macros continuing to run once you leave the Dojo (or are kicked out).

You can also configure SleepHunter to close the game client entirely when you leave the Dojo.

## Can I customize certain behaviors?

Yes, many of the behaviors of SleepHunter can be customized to your liking in the `Settings` tab.
It is recommended that you review the settings before starting macroing.
The default settings are designed to work for most users, but you may want to customize them to your needs.
