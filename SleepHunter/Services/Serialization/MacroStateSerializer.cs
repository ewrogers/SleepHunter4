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

        public MacroStateSerializer() { }

        public void SaveState(PlayerMacroState state, string file)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using var stream = File.OpenWrite(file);
            using var writer = new StreamWriter(stream, Encoding.UTF8);

            SaveState(state, writer);
        }

        public void SaveState(PlayerMacroState state, Stream stream, bool leaveOpen = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var writer = new StreamWriter(stream, Encoding.UTF8, DefaultBufferSize, leaveOpen);
            SaveState(state, writer);
        }

        public void SaveState(PlayerMacroState state, TextWriter writer)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var serializedState = SerializeState(state);

            var xs = new XmlSerializer(typeof(SerializedMacroState), string.Empty);
            xs.Serialize(writer, serializedState);
        }

        public void LoadState(PlayerMacroState state, string file)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using var stream = File.OpenRead(file);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            LoadState(state, reader);
        }

        public void LoadState(PlayerMacroState state, Stream stream, bool leaveOpen = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var reader = new StreamReader(stream, Encoding.UTF8, false, DefaultBufferSize, leaveOpen);
            LoadState(state, reader);
        }

        public void LoadState(PlayerMacroState state, TextReader reader)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var xs = new XmlSerializer(typeof(SerializedMacroState), string.Empty);
            var result = xs.Deserialize(reader);

            if (result is SerializedMacroState deserializedState)
                DeserializeStateInto(state, deserializedState);
        }

        private SerializedMacroState SerializeState(PlayerMacroState state)
        {
            return new SerializedMacroState();
        }

        private void DeserializeStateInto(PlayerMacroState state, SerializedMacroState deserializedState)
        {

        }
    }
}
