using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using SleepHunter.Macro;

namespace SleepHunter.Services.Serialization
{
    public sealed class MacroStateSerializer : IMacroStateSerializer
    {
        private const int DefaultBufferSize = 4096;

        public void Serialize(PlayerMacroState state, string file)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using var stream = File.OpenWrite(file);
            using var writer = new StreamWriter(stream, Encoding.UTF8);

            Serialize(state, writer);
        }

        public void Serialize(PlayerMacroState state, Stream stream, bool leaveOpen = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var writer = new StreamWriter(stream, Encoding.UTF8, DefaultBufferSize, leaveOpen);
            Serialize(state, writer);
        }

        public void Serialize(PlayerMacroState state, TextWriter writer)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var serializedState = SerializeState(state);

            var xs = new XmlSerializer(typeof(SerializedMacroState), string.Empty);
            xs.Serialize(writer, serializedState);
        }

        public SerializedMacroState Deserialize(PlayerMacroState state, string file)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using var stream = File.OpenRead(file);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            return Deserialize(state, reader);
        }

        public SerializedMacroState Deserialize(PlayerMacroState state, Stream stream, bool leaveOpen = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var reader = new StreamReader(stream, Encoding.UTF8, false, DefaultBufferSize, leaveOpen);

            return Deserialize(state, reader);
        }

        public SerializedMacroState Deserialize(PlayerMacroState state, TextReader reader)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var xs = new XmlSerializer(typeof(SerializedMacroState), string.Empty);
            var result = xs.Deserialize(reader);

            if (result is not SerializedMacroState deserializedState)
                throw new InvalidOperationException("Unable to parse macro state");

            return deserializedState;
        }

        private static SerializedMacroState SerializeState(PlayerMacroState state)
        {
            var client = state.Client;

            var serialized = new SerializedMacroState
            {
                Name = client.Name,
                Description = string.Empty,
                SpellRotation = state.SpellQueueRotation,
                UseLyliacVineyard = state.UseLyliacVineyard,
                FlowerAlternateCharacters = state.FlowerAlternateCharacters
            };

            if (state.Client.HasHotkey)
            {
                serialized.Hotkey = new SerializedHotkey
                {
                    Key = client.Hotkey.Key,
                    Modifiers = client.Hotkey.Modifiers
                };
            }

            foreach (var skillName in client.Skillbook.ActiveSkills)
                serialized.Skills.Add(new SerializedSkillState { SkillName = skillName });

            foreach(var spell in state.QueuedSpells)
            {
                serialized.Spells.Add(new SerializedSpellState
                {
                    SpellName = spell.Name,
                    TargetMode = spell.Target.Mode,
                    TargetName = spell.Target.CharacterName,
                    LocationX = spell.Target.Location.X,
                    LocationY = spell.Target.Location.Y,
                    OffsetX = spell.Target.Offset.X,
                    OffsetY = spell.Target.Offset.Y,
                    InnerRadius = spell.Target.InnerRadius,
                    OuterRadius = spell.Target.OuterRadius,
                    TargetLevel = spell.TargetLevel ?? 0
                });
            }

            foreach (var flower in state.FlowerTargets)
            {
                serialized.FlowerTargets.Add(new SerializedFlowerState
                {
                    TargetMode = flower.Target.Mode,
                    TargetName = flower.Target.CharacterName,
                    LocationX = flower.Target.Location.X,
                    LocationY = flower.Target.Location.Y,
                    OffsetX = flower.Target.Offset.X,
                    OffsetY = flower.Target.Offset.Y,
                    InnerRadius = flower.Target.InnerRadius,
                    OuterRadius = flower.Target.OuterRadius,
                    Interval = flower.Interval ?? TimeSpan.Zero,
                    ManaThreshold = flower.ManaThreshold ?? 0
                });
            }

            return serialized;
        }
    }
}
