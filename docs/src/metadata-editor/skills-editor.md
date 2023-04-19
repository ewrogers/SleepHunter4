# Skills Editor

![image](../screenshots/metadata-skills-editor.png)

The `Skills` tab in the `Metadata Editor` allows you to edit the database of known skills.
This allows SleepHunter to recognize skills and configure behaviors when using them.

### Adding a New Skill

To add a new skill, click the `Add` button in the bottom bar. You will be prompted to enter the skill information.

### Editing a Skill

To edit a skill, double-click the skill in the list or select it and click the `Edit` button in the bottom bar. 
You will be prompted to enter the skill information, similar to the `Add Skill` dialog.

### Removing a Skill

To remove a skill, select it and click the `Remove` button in the bottom bar.

### Clearing All Skills

To clear all skills, click the `Clear All` button in the bottom bar.

### Save Changes

Save all changes to the database file. This will overwrite the existing file.

### Revert Changes

Discard all changes and revert to the last saved state (from file).

## Add/Edit Skill Dialog

![image](../screenshots/metadata-skill-dialog.png)

The `Skill` dialog allows you to enter the information for a skill, either for adding a new skill or editing an existing one.

### Skill Name

The name of the skill. This is the name that will be displayed in the UI.

### Group Name

The group name of the skill, used for categorizing skills for certain interactions.

### Mana Cost

The mana cost of the skill, in MP.

### Cooldown

The cooldown of the skill, in seconds (if applicable). A zero-cool down means the skill has no cooldown.

### Opens Dialog on Use

Whether the skill opens a dialog when used. This is used to determine if the popup should be dismissed to continue macroing.
For example `Peek`, `Sense`, `Martial Awareness`, etc.

### Does Not Level

Whether the skill does not increase on level. If this is set the skill level will not be displayed in the UI.

### Is an Assail Skill

Whether the skill is an `Assail` skill. This is used to determine if the skill should be treated as an `Assail` skill for certain behaviors, like space-bar performing.

### Disarm Before Using

Whether the character should disarm before using the skill. This is useful when using skills like monk kicks.

### Character Class

The character classes that can use the skill.

**NOTE:** This is currently unused but may be used in the future for certain behaviors.
