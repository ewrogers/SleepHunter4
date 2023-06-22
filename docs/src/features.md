# Features

SleepHunter is an incredibly powerful automation tool for [Dark Ages](https://www.darkages.com).

This section is a high-level overview of the features that are available in SleepHunter.
You can find more detailed information about each feature as you read through the rest of this documentation.

### Direct Interaction

SleepHunter uses [Windows Messages](https://learn.microsoft.com/en-us/windows/win32/learnwin32/window-messages) to directly interact with the game client.
This allows SleepHunter to emulate user input without interrupting the user's normal workflow on their computer.
No errant mouse clicks or keyboard presses in other applications will occur while SleepHunter is running.

### Game Client Detection

SleepHunter scans all running processes to find Dark Ages game clients automatically.
It can detect multiple game clients running on the same computer at the same time.

Uses client signature to differentiate between varying client versions (including modified clients).

### Runtime Patching

SleepHunter can launch new game client instances and patch them at runtime, allowing tweaks such as multiple instances
and skipping the intro video on startup. This is useful for running multiple characters together.

### Character State Reading

SleepHunter can read the current state of each character logged into a game client.
This includes the character's name, level, health, mana, location, inventory, skills, and spells.
It can also read the state of various user interface elements such as the chat window, skill window, and spell window.

### Map Aware

SleepHunter reads the current map that the character is on, and can detect when the character has moved to a new map.
This allows automatic stopping of macros when the character has left the Dojo or any other map, avoiding the risk of punishment.

### Game Metadata

SleepHunter has a built-in database of game data, including the names of all skills, spells, and staves for casting.
This allows intelligent "wait for mana", "disarm", and other features that require knowledge of the game's mechanics and state.

There is even a built-in metadata editor that allows you to add your own custom skills, spells, and staves.

### Asynchronous Skills

SleepHunter can use multiple skills at the same time, interleaving them to maximize the skill increases.
It can also do this while casting spells, allowing you to level up your spells while you level up your skills.

### Spell Queue

SleepHunter can queue multiple spells to be leveled up at the same time, either one at a time or round-robin style.
You can set a maximum level for each spell, and it will stop casting that spell once it reaches that level.

### Flower Queue

SleepHunter can queue different targets to use Lyliac Plant (flower) for sharing mana to others.
It also supports Lyliac Vineyard for group-based mana distribution.

### Automatic Fas Spiorad

SleepHunter can cast "fas spiorad" automatically when your character is low on mana.
This includes when acting as a "mana battery" for other characters, including your own.

### Staff Switching

SleepHunter can automatically switch between staves when casting spells to ensure the fastest possible cast time.
It can also temporarily un-equip a staff for zero-line casting of some instant spells.

### Alternate Character Aware

SleepHunter is aware of all other characters logged in on the same computer.
This allows it special functionality when supporting other characters and prioritizing mana distribution.

### Hotkeys

SleepHunter allows user-defined hotkeys to start, pause, and resume automation for each character.
This can be used to pause automation when you need to do something manually, or to resume automation when you are done.

### Auto-Save Macro State

SleepHunter automatically saves the state of each character's last macro when the application is closed.
This allows you to easily resume automation where you left off, even after you close the application.

### Color Themes

SleepHunter supports multiple color themes that act as a "skin" for the application.
This allows you to customize the look and feel of the application to your liking.

### Automatic Updates

SleepHunter now supports automatic updates, checking the same GitHub releases page that you downloaded the application from.
If an update is available, you will be prompted to download and install it.
SleepHunter will automatically close and restart after the update is applied.

### ...and more!

As you read through the rest of this documentation, you will find many more features that are available in SleepHunter.
