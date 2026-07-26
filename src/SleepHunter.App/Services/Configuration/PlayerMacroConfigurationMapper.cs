using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Spells;
using PersistenceHotkeyModifiers =
    SleepHunter.Persistence.Configuration.HotkeyModifiers;
using RuntimeSpellTarget =
    SleepHunter.Runtime.Automation.Spells.SpellTarget;

namespace SleepHunter.Services.Configuration
{
    public sealed class PlayerMacroConfigurationMapper :
        IPlayerMacroConfigurationMapper
    {
        public MacroConfiguration CreateSnapshot(
            PlayerMacroConfiguration source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var spells = source
                .GetSpellQueueSnapshot()
                .Select(
                    item => new SpellQueueEntry(
                        new SpellQueueEntryId(item.Id),
                        item.Name,
                        item.TargetLevel,
                        ToRuntimeTarget(item.Target),
                        item.HealthCondition))
                .ToImmutableArray();
            var flowers = source
                .GetFlowerQueueSnapshot()
                .Select(
                    item => new FlowerQueueEntry(
                        new FlowerQueueEntryId(item.Id),
                        ToRuntimeTarget(item.Target),
                        item.Interval,
                        item.ManaThreshold))
                .ToImmutableArray();
            return new MacroConfiguration(
                source.Client.Name,
                source.Description,
                ToPersistenceHotkey(source.Client.Hotkey),
                ToRuntimeRotation(source.SpellQueueRotation),
                source.GetSkillQueueSnapshot().ToImmutableArray(),
                spells,
                flowers,
                new FlowerOptions(
                    source.UseLyliacVineyard,
                    source.FlowerAlternateCharacters,
                    source.PrioritizeAlternateCharacters,
                    source.MaximumFlowerXDistance,
                    source.MaximumFlowerYDistance));
        }

        public void Apply(
            PlayerMacroConfiguration destination,
            MacroConfigurationLoadResult loaded)
        {
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(loaded);

            var source = loaded.Configuration;
            var hotkey = ToEditorHotkey(source.Hotkey);
            var spells = source.Spells
                .Select(
                    entry => CreateSpell(
                        destination,
                        entry,
                        loaded.Format))
                .ToArray();
            var flowers = source.Flowers
                .Select(CreateFlower)
                .ToArray();

            destination.Description = source.Description;
            destination.SpellQueueRotation =
                ToEditorRotation(source.SpellRotation);
            destination.UseLyliacVineyard =
                source.FlowerOptions.UseVineyard;
            destination.FlowerAlternateCharacters =
                source.FlowerOptions.FlowerAlternateCharacters;
            destination.PrioritizeAlternateCharacters =
                source.FlowerOptions.PrioritizeAlternateCharacters;
            destination.MaximumFlowerXDistance =
                source.FlowerOptions.MaximumXDistance;
            destination.MaximumFlowerYDistance =
                source.FlowerOptions.MaximumYDistance;
            destination.ReplaceSkills(source.Skills);
            destination.ClearSpellQueue();
            foreach (var spell in spells)
                destination.AddToSpellQueue(spell);

            destination.ClearFlowerQueue();
            foreach (var flower in flowers)
                destination.AddToFlowerQueue(flower);

            destination.Client.Hotkey = hotkey;
        }

        private static SpellQueueItem CreateSpell(
            PlayerMacroConfiguration destination,
            SpellQueueEntry entry,
            MacroConfigurationFormat format)
        {
            var observed =
                destination.Client.Spellbook.GetSpell(entry.Name);
            var healthCondition =
                format == MacroConfigurationFormat.LegacyV4 &&
                !entry.HealthCondition.IsRestricted &&
                observed is not null
                    ? CreateHealthCondition(observed)
                    : entry.HealthCondition;
            return new SpellQueueItem
            {
                Id = entry.Id.Value,
                Icon = observed?.Icon,
                Name = entry.Name,
                Target = ToEditorTarget(entry.Target),
                TargetLevel = entry.TargetLevel,
                CurrentLevel = observed?.CurrentLevel ?? 0,
                MaximumLevel = observed?.MaximumLevel ?? 0,
                IsOnCooldown = observed?.IsOnCooldown ?? false,
                HealthCondition = healthCondition
            };
        }

        private static FlowerQueueItem CreateFlower(
            FlowerQueueEntry entry) =>
            new()
            {
                Id = entry.Id.Value,
                Target = ToEditorTarget(entry.Target),
                Interval = entry.Interval,
                ManaThreshold = entry.ManaThreshold
            };

        private static HealthCondition CreateHealthCondition(
            Spell spell) =>
            new(
                spell.MinHealthPercent,
                spell.MaxHealthPercent);

        private static HotkeyConfiguration ToPersistenceHotkey(
            Hotkey hotkey) =>
            hotkey is null
                ? null
                : new HotkeyConfiguration(
                    hotkey.Key.ToString(),
                    ToPersistenceModifiers(hotkey.Modifiers));

        private static Hotkey ToEditorHotkey(
            HotkeyConfiguration hotkey)
        {
            if (hotkey is null)
                return null;

            if (!Enum.TryParse<Key>(
                    hotkey.Key,
                    ignoreCase: true,
                    out var key) ||
                !Enum.IsDefined(key))
            {
                throw new InvalidOperationException(
                    $"Macro hotkey '{hotkey.Key}' is not a supported key.");
            }

            return new Hotkey(
                ToEditorModifiers(hotkey.Modifiers),
                key);
        }

        private static PersistenceHotkeyModifiers
            ToPersistenceModifiers(ModifierKeys modifiers)
        {
            var result = PersistenceHotkeyModifiers.None;
            if (modifiers.HasFlag(ModifierKeys.Alt))
                result |= PersistenceHotkeyModifiers.Alt;
            if (modifiers.HasFlag(ModifierKeys.Control))
                result |= PersistenceHotkeyModifiers.Control;
            if (modifiers.HasFlag(ModifierKeys.Shift))
                result |= PersistenceHotkeyModifiers.Shift;
            if (modifiers.HasFlag(ModifierKeys.Windows))
                result |= PersistenceHotkeyModifiers.Windows;
            return result;
        }

        private static ModifierKeys ToEditorModifiers(
            PersistenceHotkeyModifiers modifiers)
        {
            var result = ModifierKeys.None;
            if (modifiers.HasFlag(PersistenceHotkeyModifiers.Alt))
                result |= ModifierKeys.Alt;
            if (modifiers.HasFlag(PersistenceHotkeyModifiers.Control))
                result |= ModifierKeys.Control;
            if (modifiers.HasFlag(PersistenceHotkeyModifiers.Shift))
                result |= ModifierKeys.Shift;
            if (modifiers.HasFlag(PersistenceHotkeyModifiers.Windows))
                result |= ModifierKeys.Windows;
            return result;
        }

        private static SpellQueueRotation? ToRuntimeRotation(
            SpellRotationMode rotation) =>
            rotation switch
            {
                SpellRotationMode.Default => null,
                SpellRotationMode.None =>
                    SpellQueueRotation.Priority,
                SpellRotationMode.Singular =>
                    SpellQueueRotation.Sequential,
                SpellRotationMode.RoundRobin =>
                    SpellQueueRotation.RoundRobin,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(rotation),
                    rotation,
                    "The spell rotation mode is not supported.")
            };

        private static SpellRotationMode ToEditorRotation(
            SpellQueueRotation? rotation) =>
            rotation switch
            {
                null => SpellRotationMode.Default,
                SpellQueueRotation.Priority =>
                    SpellRotationMode.None,
                SpellQueueRotation.Sequential =>
                    SpellRotationMode.Singular,
                SpellQueueRotation.RoundRobin =>
                    SpellRotationMode.RoundRobin,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(rotation),
                    rotation,
                    "The spell queue rotation is not supported.")
            };

        private static RuntimeSpellTarget ToRuntimeTarget(
            Models.SpellTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            var x = ToCoordinate(target.Location.X, "X");
            var y = ToCoordinate(target.Location.Y, "Y");
            var offset = new TargetOffset(
                ToCoordinate(target.Offset.X, "offset X"),
                ToCoordinate(target.Offset.Y, "offset Y"));
            var result = target.Mode switch
            {
                SpellTargetMode.None => RuntimeSpellTarget.None,
                SpellTargetMode.Self => RuntimeSpellTarget.Self,
                SpellTargetMode.Character =>
                    RuntimeSpellTarget.Character(
                        target.CharacterName),
                SpellTargetMode.RelativeTile =>
                    RuntimeSpellTarget.RelativeTile(x, y),
                SpellTargetMode.AbsoluteTile =>
                    RuntimeSpellTarget.AbsoluteTile(x, y),
                SpellTargetMode.AbsoluteXY =>
                    RuntimeSpellTarget.ScreenPoint(x, y),
                SpellTargetMode.RelativeRadius =>
                    CreateAreaTarget(
                        absolute: false,
                        x,
                        y,
                        target.InnerRadius,
                        target.OuterRadius),
                SpellTargetMode.AbsoluteRadius =>
                    CreateAreaTarget(
                        absolute: true,
                        x,
                        y,
                        target.InnerRadius,
                        target.OuterRadius),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(target),
                    target.Mode,
                    "The spell target mode is not supported.")
            };
            return result.Kind == SpellTargetKind.None ||
                   offset == TargetOffset.Zero
                ? result
                : result.WithOffset(offset.X, offset.Y);
        }

        private static RuntimeSpellTarget CreateAreaTarget(
            bool absolute,
            int x,
            int y,
            int innerRadius,
            int outerRadius)
        {
            if (outerRadius <= 0 || innerRadius > outerRadius)
            {
                return absolute
                    ? RuntimeSpellTarget.AbsoluteTile(x, y)
                    : RuntimeSpellTarget.RelativeTile(x, y);
            }

            var normalizedInner = Math.Max(0, innerRadius);
            return absolute
                ? RuntimeSpellTarget.AbsoluteArea(
                    x,
                    y,
                    normalizedInner,
                    outerRadius)
                : RuntimeSpellTarget.RelativeArea(
                    x,
                    y,
                    normalizedInner,
                    outerRadius);
        }

        private static Models.SpellTarget ToEditorTarget(
            RuntimeSpellTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return new Models.SpellTarget
            {
                Mode = target.Kind switch
                {
                    SpellTargetKind.None =>
                        SpellTargetMode.None,
                    SpellTargetKind.Self =>
                        SpellTargetMode.Self,
                    SpellTargetKind.Character =>
                        SpellTargetMode.Character,
                    SpellTargetKind.RelativeTile =>
                        SpellTargetMode.RelativeTile,
                    SpellTargetKind.AbsoluteTile =>
                        SpellTargetMode.AbsoluteTile,
                    SpellTargetKind.ScreenPoint =>
                        SpellTargetMode.AbsoluteXY,
                    SpellTargetKind.RelativeArea =>
                        SpellTargetMode.RelativeRadius,
                    SpellTargetKind.AbsoluteArea =>
                        SpellTargetMode.AbsoluteRadius,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(target),
                        target.Kind,
                        "The spell target kind is not supported.")
                },
                CharacterName = target.CharacterName,
                Location = new Point(
                    target.X ?? 0,
                    target.Y ?? 0),
                Offset = new Point(
                    target.Offset.X,
                    target.Offset.Y),
                InnerRadius = target.InnerRadius ?? 0,
                OuterRadius = target.OuterRadius ?? 0
            };
        }

        private static int ToCoordinate(
            double value,
            string name)
        {
            if (!double.IsFinite(value) ||
                value < int.MinValue ||
                value > int.MaxValue ||
                Math.Truncate(value) != value)
            {
                throw new InvalidOperationException(
                    $"Macro target {name} must be a 32-bit integer.");
            }

            return (int)value;
        }
    }
}
