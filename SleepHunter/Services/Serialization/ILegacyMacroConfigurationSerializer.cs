using System.IO;
using SleepHunter.Macro;

namespace SleepHunter.Services.Serialization
{
    public interface ILegacyMacroConfigurationSerializer
    {
        void Serialize(
            PlayerMacroConfiguration configuration,
            TextWriter writer);
        void Serialize(
            PlayerMacroConfiguration configuration,
            Stream stream,
            bool leaveOpen = true);
        void Serialize(
            PlayerMacroConfiguration configuration,
            string file);

        SerializedMacroState Deserialize(TextReader reader);
        SerializedMacroState Deserialize(
            Stream stream,
            bool leaveOpen = true);
        SerializedMacroState Deserialize(string file);
    }
}
