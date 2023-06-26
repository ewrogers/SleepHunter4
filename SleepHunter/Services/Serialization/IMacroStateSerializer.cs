using System.IO;
using System.Xml;
using SleepHunter.Macro;

namespace SleepHunter.Services.Serialization
{
    public interface IMacroStateSerializer
    {
        void Serialize(PlayerMacroState state, XmlWriter writer, bool leaveOpen = true);
        void Serialize(PlayerMacroState state, Stream stream, bool leaveOpen = true);
        void Serialize(PlayerMacroState state, string file);
    }
}
