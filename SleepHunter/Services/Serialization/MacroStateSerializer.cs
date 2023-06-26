using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SleepHunter.Macro;

namespace SleepHunter.Services.Serialization
{
    public sealed class MacroStateSerializer : IMacroStateSerializer
    {
       public MacroStateSerializer() { }

        public void Serialize(PlayerMacroState state, XmlWriter writer, bool leaveOpen = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (!leaveOpen)
                writer.Close();
        }

        public void Serialize(PlayerMacroState state, Stream stream, bool leaveOpen = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!leaveOpen)
                stream.Close();
        }

        public void Serialize(PlayerMacroState state, string file)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (file == null)
                throw new ArgumentNullException(nameof(file));
        }
    }
}
