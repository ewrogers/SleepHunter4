using System;
using System.IO;
using System.Text;

namespace SleepHunter.IO.Process
{
    internal static class RuntimeMemoryReader
    {
        internal const uint MinimumAddress = 0x00400000;
        internal const uint MaximumAddress = 0x7FFFFFFF;

        public static bool IsPlausibleAddress(long address) =>
            address >= MinimumAddress && address <= MaximumAddress;

        public static bool TryReadUInt32(BinaryReader reader, long address, out uint value)
        {
            value = 0;

            if (reader == null || !IsPlausibleAddress(address))
                return false;

            try
            {
                reader.BaseStream.Position = address;
                value = reader.ReadUInt32();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryReadBytes(BinaryReader reader, long address, int count, out byte[] value)
        {
            value = null;

            if (reader == null || count < 0 || !IsPlausibleAddress(address))
                return false;

            try
            {
                reader.BaseStream.Position = address;
                value = reader.ReadBytes(count);
                return value.Length == count;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public static bool TryReadAsciiString(
            BinaryReader reader,
            long address,
            int maximumLength,
            out string value,
            bool requireTerminator = false)
        {
            value = null;

            if (maximumLength <= 0 ||
                !TryReadBytes(reader, address, maximumLength, out var bytes))
            {
                return false;
            }

            var terminator = Array.IndexOf(bytes, (byte)0);
            if (terminator < 0)
            {
                if (requireTerminator)
                    return false;

                terminator = bytes.Length;
            }

            value = Encoding.ASCII.GetString(bytes, 0, terminator);
            return true;
        }

        public static bool TryReadRttiClassName(BinaryReader reader, uint objectAddress, out string className)
        {
            className = null;

            if (!TryReadUInt32(reader, objectAddress, out var virtualTableAddress) ||
                virtualTableAddress < MinimumAddress + sizeof(uint) ||
                !TryReadUInt32(reader, virtualTableAddress - sizeof(uint), out var locatorAddress) ||
                !TryReadUInt32(reader, locatorAddress + 0x0C, out var typeDescriptorAddress) ||
                !TryReadAsciiString(reader, typeDescriptorAddress + 0x08, 192, out var decoratedName, true))
            {
                return false;
            }

            className = NormalizeRttiClassName(decoratedName);
            return !string.IsNullOrWhiteSpace(className);
        }

        internal static string NormalizeRttiClassName(string decoratedName)
        {
            if (string.IsNullOrWhiteSpace(decoratedName))
                return null;

            const string classPrefix = ".?AV";
            const string structPrefix = ".?AU";

            var start = decoratedName.StartsWith(classPrefix, StringComparison.Ordinal)
                ? classPrefix.Length
                : decoratedName.StartsWith(structPrefix, StringComparison.Ordinal)
                    ? structPrefix.Length
                    : 0;

            if (start == 0)
                return decoratedName.Trim();

            var end = decoratedName.IndexOf("@@", start, StringComparison.Ordinal);
            if (end < 0)
                end = decoratedName.Length;

            return decoratedName[start..end];
        }
    }
}
