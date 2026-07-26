using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using SleepHunter.Interop.Mappings;

namespace SleepHunter.Settings
{
    public sealed class ClientLayoutManager
    {
        public const string LayoutFile = @"ClientLayout.xml";

        private const int MaximumLayoutBytes = 1_048_576;

        private static readonly ClientLayoutManager instance = new();

        private ClientLayoutManager()
        {
        }

        public static ClientLayoutManager Instance => instance;

        public ClientLayout Layout { get; private set; }

        public void LoadFromFile(string filename)
        {
            using var inputStream = File.Open(
                filename,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            LoadFromStream(inputStream);
        }

        public void LoadFromStream(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            using var buffer = ReadBounded(stream);
            ClientMemoryMapLoader.Load(buffer);
            buffer.Position = 0;

            var settings = new XmlReaderSettings
            {
                CloseInput = false,
                DtdProcessing = DtdProcessing.Prohibit,
                MaxCharactersFromEntities = 0,
                MaxCharactersInDocument = MaximumLayoutBytes,
                XmlResolver = null
            };
            using var reader = XmlReader.Create(buffer, settings);
            var serializer = new XmlSerializer(typeof(ClientLayout));
            if (serializer.Deserialize(reader) is not ClientLayout layout)
            {
                throw new InvalidDataException(
                    "The client layout document is empty.");
            }

            Validate(layout);
            Layout = layout;
        }

        private static MemoryStream ReadBounded(Stream stream)
        {
            var buffer = new MemoryStream();
            try
            {
                var chunk = new byte[81920];
                while (true)
                {
                    var bytesRead = stream.Read(
                        chunk,
                        0,
                        chunk.Length);
                    if (bytesRead == 0)
                        break;

                    if (buffer.Length + bytesRead >
                        MaximumLayoutBytes)
                    {
                        throw new InvalidDataException(
                            $"The client layout exceeds the {MaximumLayoutBytes:N0} byte limit.");
                    }

                    buffer.Write(chunk, 0, bytesRead);
                }

                buffer.Position = 0;
                return buffer;
            }
            catch
            {
                buffer.Dispose();
                throw;
            }
        }

        private static void Validate(ClientLayout layout)
        {
            if (!string.Equals(
                    layout.PointerWidth,
                    "Bit32",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"The client layout pointer width '{layout.PointerWidth}' is not supported.");
            }

            if (string.IsNullOrWhiteSpace(layout.WindowClassName))
            {
                throw new InvalidDataException(
                    "The client layout window class name is required.");
            }

            if (string.IsNullOrWhiteSpace(layout.WindowTitle))
            {
                throw new InvalidDataException(
                    "The client layout window title is required.");
            }

            if (string.IsNullOrWhiteSpace(layout.ExecutableName))
            {
                throw new InvalidDataException(
                    "The client layout executable name is required.");
            }

            if (layout.Variables is null || layout.Variables.Count == 0)
            {
                throw new InvalidDataException(
                    "The client layout must contain memory variables.");
            }
        }
    }
}
