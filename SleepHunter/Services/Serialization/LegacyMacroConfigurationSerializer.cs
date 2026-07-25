using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SleepHunter.Macro;

namespace SleepHunter.Services.Serialization
{
    public sealed class LegacyMacroConfigurationSerializer :
        ILegacyMacroConfigurationSerializer
    {
        private const int DefaultBufferSize = 4096;

        public void Serialize(
            PlayerMacroConfiguration configuration,
            string file)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            ArgumentException.ThrowIfNullOrWhiteSpace(file);

            var tempFile = Path.GetTempFileName();
            using var stream = File.Create(tempFile);
            using var writer = new StreamWriter(stream, Encoding.UTF8, DefaultBufferSize, false);

            Serialize(configuration, writer);

            writer.Flush();
            writer.Close();

            if (File.Exists(file))
            {
                File.Replace(tempFile, file, null);
            }
            else
            {
                File.Move(tempFile, file);
            }
        }

        public void Serialize(
            PlayerMacroConfiguration configuration,
            Stream stream,
            bool leaveOpen = true)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(stream);

            using var writer = new StreamWriter(stream, Encoding.UTF8, DefaultBufferSize, leaveOpen);
            Serialize(configuration, writer);
            writer.Flush();
        }

        public void Serialize(
            PlayerMacroConfiguration configuration,
            TextWriter writer)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(writer);

            var serializedState = SerializeConfiguration(
                configuration);

            var xs = new XmlSerializer(typeof(SerializedMacroState), string.Empty);
            xs.Serialize(writer, serializedState);
        }

        public SerializedMacroState Deserialize(string file)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(file);

            using var stream = File.OpenRead(file);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            return Deserialize(reader);
        }

        public SerializedMacroState Deserialize(
            Stream stream,
            bool leaveOpen = true)
        {
            ArgumentNullException.ThrowIfNull(stream);

            using var reader = new StreamReader(stream, Encoding.UTF8, false, DefaultBufferSize, leaveOpen);

            return Deserialize(reader);
        }

        public SerializedMacroState Deserialize(TextReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var xs = new XmlSerializer(typeof(SerializedMacroState), string.Empty);
            var result = xs.Deserialize(reader);

            if (result is not SerializedMacroState deserializedState)
            {
                throw new InvalidOperationException(
                    "Unable to parse the legacy macro configuration.");
            }

            return deserializedState;
        }

        private static SerializedMacroState SerializeConfiguration(
            PlayerMacroConfiguration configuration)
        {
            var client = configuration.Client;

            var serialized = new SerializedMacroState
            {
                Name = client.Name,
                Description = string.Empty,
                SpellRotation = configuration.SpellQueueRotation,
                UseLyliacVineyard =
                    configuration.UseLyliacVineyard,
                FlowerAlternateCharacters =
                    configuration.FlowerAlternateCharacters
            };

            if (client.HasHotkey)
            {
                serialized.Hotkey = new SerializedHotkey
                {
                    Key = client.Hotkey.Key,
                    Modifiers = client.Hotkey.Modifiers
                };
            }

            foreach (var skillName in client.Skillbook.ActiveSkills)
                serialized.Skills.Add(new SerializedSkillState { SkillName = skillName });

            var queuedSpellsSnapshot =
                configuration.GetSpellQueueSnapshot();
            foreach (var spell in queuedSpellsSnapshot)
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

            var flowerTargetsSnapshot =
                configuration.GetFlowerQueueSnapshot();
            foreach (var flower in flowerTargetsSnapshot)
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
