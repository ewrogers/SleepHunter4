using System.IO;
using SleepHunter.Macro;

namespace SleepHunter.Services.Serialization
{
    public interface IMacroStateSerializer
    {
        void SaveState(PlayerMacroState state, TextWriter writer);
        void SaveState(PlayerMacroState state, Stream stream, bool leaveOpen = true);
        void SaveState(PlayerMacroState state, string file);

        void LoadState(PlayerMacroState state, TextReader reader);
        void LoadState(PlayerMacroState state, Stream stream, bool leaveOpen = true);
        void LoadState(PlayerMacroState state, string file);
    }
}
