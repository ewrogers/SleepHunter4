using System.IO;
using SleepHunter.Macro;

namespace SleepHunter.Services.Serialization
{
    public interface IMacroStateSerializer
    {
        void Serialize(PlayerMacroState state, TextWriter writer);
        void Serialize(PlayerMacroState state, Stream stream, bool leaveOpen = true);
        void Serialize(PlayerMacroState state, string file);

        SerializedMacroState Deserialize(PlayerMacroState state, TextReader reader);
        SerializedMacroState Deserialize(PlayerMacroState state, Stream stream, bool leaveOpen = true);
        SerializedMacroState Deserialize(PlayerMacroState state, string file);
    }
}
