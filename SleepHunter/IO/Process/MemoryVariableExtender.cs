using System.IO;

using SleepHunter.Extensions;

namespace SleepHunter.IO.Process
{
    public static class MemoryVariableExtender
    {
        private const long MinimumAddress = 0x00400000;
        private const long MaximumAddress = 0x7FFFFFFF;

        public static long DereferencePointer(long address, BinaryReader reader)
        {
            if (address == 0)
                return 0;

            reader.BaseStream.Position = address;
            var reference = reader.ReadUInt32();

            return reference;
        }

        public static bool TryDeferenceValue(this MemoryVariable variable, BinaryReader reader, out long address, bool isStringType = false)
        {
            address = DereferenceValue(variable, reader, isStringType);
            return address != 0;
        }

        public static long DereferenceValue(this MemoryVariable variable, BinaryReader reader, bool isStringType = false)
        {
            long address = variable.Address;

            if (variable is DynamicMemoryVariable)
            {
                var dynamicVar = variable as DynamicMemoryVariable;

                foreach (var offset in dynamicVar.Offsets)
                {
                    if (address < MinimumAddress || address > MaximumAddress)
                        return 0;

                    address = DereferencePointer(address, reader);

                    if (address == 0)
                        return 0;

                    if (offset.IsNegative)
                        address -= offset.Offset;
                    else
                        address += offset.Offset;
                }

                if (isStringType)
                {
                    reader.BaseStream.Position = address;

                    for (int i = 0; i < 4; i++)
                    {
                        var c = reader.ReadChar();

                        bool allowWhiteSpace = i != 0;
                        bool allowDash = i == 0;

                        if (char.IsLetterOrDigit(c))
                            continue;

                        if (char.IsWhiteSpace(c) && allowWhiteSpace)
                            continue;

                        if (c == '-' && allowDash)
                            continue;

                        if (c == '\0')
                            continue;

                        address = DereferencePointer(address, reader);
                        break;
                    }
                }
            }

            return address;
        }

        public static bool TryReadBoolean(this MemoryVariable variable, BinaryReader reader, out bool value)
        {
            value = false;

            try
            {
                var success = TryReadByte(variable, reader, out var byteValue);

                if (!success)
                    return false;

                value = byteValue != 0;
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadChar(this MemoryVariable variable, BinaryReader reader, out char value)
        {
            value = '\0';

            try
            {
                var success = TryReadByte(variable, reader, out var byteValue);

                if (!success)
                    return false;

                value = (char)byteValue;
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadByte(this MemoryVariable variable, BinaryReader reader, out byte value)
        {
            value = 0;

            try
            {
                var address = DereferenceValue(variable, reader);

                if (address == 0)
                    return false;

                reader.BaseStream.Position = address;

                value = reader.ReadByte();
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadInt16(this MemoryVariable variable, BinaryReader reader, out short value)
        {
            value = 0;

            try
            {
                var address = DereferenceValue(variable, reader);

                if (address == 0)
                    return false;

                reader.BaseStream.Position = address;

                value = reader.ReadInt16();
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadUInt16(this MemoryVariable variable, BinaryReader reader, out ushort value)
        {
            value = 0;

            try
            {
                var address = DereferenceValue(variable, reader);

                if (address == 0)
                    return false;

                reader.BaseStream.Position = address;

                value = reader.ReadUInt16();
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadInt32(this MemoryVariable variable, BinaryReader reader, out int value)
        {
            value = 0;

            try
            {
                var address = DereferenceValue(variable, reader);

                if (address == 0)
                    return false;

                reader.BaseStream.Position = address;

                value = reader.ReadInt32();
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadUInt32(this MemoryVariable variable, BinaryReader reader, out uint value)
        {
            value = 0;

            try
            {
                var address = DereferenceValue(variable, reader);

                if (address == 0)
                    return false;

                reader.BaseStream.Position = address;

                value = reader.ReadUInt32();
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadInt64(this MemoryVariable variable, BinaryReader reader, out long value)
        {
            value = 0;

            try
            {
                var address = DereferenceValue(variable, reader);

                if (address == 0)
                    return false;

                reader.BaseStream.Position = address;

                value = reader.ReadInt64();
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadUInt64(this MemoryVariable variable, BinaryReader reader, out ulong value)
        {
            value = 0;

            try
            {
                var address = DereferenceValue(variable, reader);

                if (address == 0)
                    return false;

                reader.BaseStream.Position = address;

                value = reader.ReadUInt64();
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadString(this MemoryVariable variable, BinaryReader reader, out string value)
        {
            value = null;

            try
            {
                var address = DereferenceValue(variable, reader, isStringType: true);

                if (address == 0)
                    return false;

                reader.BaseStream.Position = address;

                value = reader.ReadNullTerminatedString(variable.MaxLength);
                return true;
            }
            catch { return false; }
        }

        public static bool TryReadIntegerString(this MemoryVariable variable, BinaryReader reader, out long value)
        {
            value = 0;

            try
            {
                var success = TryReadString(variable, reader, out var stringValue);

                if (!success)
                    return false;

                if (long.TryParse(stringValue.Trim(), out var integerValue))
                    value = integerValue;
                else
                    value = 0;

                return true;
            }
            catch { return false; }
        }
    }
}
