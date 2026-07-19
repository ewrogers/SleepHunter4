using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal static class ClientPatcher
    {
        private const long SupportedClientSize = 3_112_960;
        private const string SupportedClientSha256 =
            "054A5D6ADC56099C6BFD9D2A58675AFF62DC788B63209A3D906492F5B89E96C6";

        private const int ModifiersKeyFixCallRva = 0x000A9D81;
        private const int InputGetEventManagerRva = 0x00027380;
        private const int InputPostKeyUpRva = 0x00066E60;
        private const int OriginalActivationFunctionRva = 0x000AC950;
        private const int GetMessageTimeThunkRva = 0x0022006E;

        private const int DialogItemLabelMaxBytes = 20;
        private const int ItemQuantityDialogHookRva = 0x0013609C;
        private const int DialogItemNameCopyRva = 0x002228C4;
        private const int DialogUiGetGuiBackPaneRva = 0x001A9C40;
        private const int UiTextTruncateDbcsSafeRva = 0x0007D670;
        private const int WsprintfImportAddressRva = 0x00269380;

        private const int PatchVerificationPadding = 8;
        private const int ModifiersKeyFixStubSize = 68;

        private static readonly byte[] ExpectedModifiersKeyFixCall =
            new byte[] { 0xE8, 0xCA, 0x2B, 0x00, 0x00 };

        private static readonly byte[] ExpectedItemQuantityDialogCall =
            new byte[] { 0xE8, 0x23, 0xC8, 0x0E, 0x00 };

        private static readonly byte[] ShowItemQuantitiesInDialogsStubTemplate =
            new byte[]
            {
                0x55, 0x89, 0xE5, 0x53, 0x56, 0x57, 0x81, 0xEC, 0x20, 0x01, 0x00, 0x00, 0x8B, 0x75, 0x10, 0x89,
                0xB5, 0xD8, 0xFE, 0xFF, 0xFF, 0x8B, 0x7D, 0x00, 0x8B, 0x7F, 0xE8, 0x0F, 0xB6, 0x3F, 0x90, 0x90,
                0x90, 0x90, 0x90, 0x83, 0xFF, 0x01, 0x0F, 0x82, 0x0D, 0x01, 0x00, 0x00, 0x83, 0xFF, 0x3C, 0x0F,
                0x87, 0x04, 0x01, 0x00, 0x00, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x85, 0xC0, 0x0F, 0x84, 0xF7, 0x00,
                0x00, 0x00, 0x8B, 0x80, 0x88, 0x4F, 0x00, 0x00, 0x85, 0xC0, 0x0F, 0x84, 0xE9, 0x00, 0x00, 0x00,
                0x8B, 0x84, 0xB8, 0x9C, 0x01, 0x00, 0x00, 0x85, 0xC0, 0x0F, 0x84, 0xDA, 0x00, 0x00, 0x00, 0x80,
                0xB8, 0x44, 0x02, 0x00, 0x00, 0x00, 0x0F, 0x84, 0xCD, 0x00, 0x00, 0x00, 0x8B, 0x88, 0x40, 0x02,
                0x00, 0x00, 0x49, 0x0F, 0x8E, 0xC0, 0x00, 0x00, 0x00, 0x41, 0x89, 0x8D, 0xD4, 0xFE, 0xFF, 0xFF,
                0xE8, 0x06, 0x00, 0x00, 0x00, 0x20, 0x28, 0x25, 0x75, 0x29, 0x00, 0x58, 0xFF, 0xB5, 0xD4, 0xFE,
                0xFF, 0xFF, 0x50, 0x8D, 0x45, 0xE4, 0x50, 0xFF, 0x15, 0x00, 0x00, 0x00, 0x00, 0x83, 0xC4, 0x0C,
                0x83, 0xF8, 0x01, 0x0F, 0x8E, 0x90, 0x00, 0x00, 0x00, 0x83, 0xF8, 0x0F, 0x0F, 0x83, 0x87, 0x00,
                0x00, 0x00, 0x89, 0x85, 0xE0, 0xFE, 0xFF, 0xFF, 0x31, 0xC9, 0x81, 0xF9, 0x00, 0x01, 0x00, 0x00,
                0x73, 0x09, 0x80, 0x3C, 0x0E, 0x00, 0x74, 0x03, 0x41, 0xEB, 0xEF, 0xBA, 0x00, 0x00, 0x00, 0x00,
                0x2B, 0x95, 0xE0, 0xFE, 0xFF, 0xFF, 0x39, 0xD1, 0x76, 0x3D, 0x83, 0xEA, 0x02, 0x8D, 0xBD, 0xE4,
                0xFE, 0xFF, 0xFF, 0x89, 0xD1, 0xFC, 0xF3, 0xA4, 0xC6, 0x07, 0x00, 0x52, 0x8D, 0x85, 0xE4, 0xFE,
                0xFF, 0xFF, 0x50, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x83, 0xC4, 0x08, 0x8D, 0xBD, 0xE4, 0xFE, 0xFF,
                0xFF, 0x80, 0x3F, 0x00, 0x74, 0x03, 0x47, 0xEB, 0xF8, 0x66, 0xC7, 0x07, 0x2E, 0x2E, 0x90, 0x90,
                0x90, 0x90, 0x83, 0xC7, 0x02, 0xEB, 0x09, 0x8D, 0xBD, 0xE4, 0xFE, 0xFF, 0xFF, 0xFC, 0xF3, 0xA4,
                0x8D, 0x75, 0xE4, 0x8B, 0x8D, 0xE0, 0xFE, 0xFF, 0xFF, 0x41, 0xFC, 0xF3, 0xA4, 0x8D, 0x85, 0xE4,
                0xFE, 0xFF, 0xFF, 0x89, 0x85, 0xD8, 0xFE, 0xFF, 0xFF, 0xFF, 0xB5, 0xD8, 0xFE, 0xFF, 0xFF, 0xFF,
                0x75, 0x0C, 0xFF, 0x75, 0x08, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x83, 0xC4, 0x0C, 0x8D, 0x65, 0xF4,
                0x5F, 0x5E, 0x5B, 0x5D, 0xC3
            };

        internal static void VerifyRuntimePatchClient(string clientExecutablePath)
        {
            using var stream = File.OpenRead(clientExecutablePath);
            if (stream.Length != SupportedClientSize)
            {
                throw new InvalidDataException(
                    $"Unsupported Dark Ages client size: expected {SupportedClientSize:N0} bytes, " +
                    $"found {stream.Length:N0} bytes.");
            }

            var actualHash = SHA256.HashData(stream);
            var actualHashText = Convert.ToHexString(actualHash);
            if (!string.Equals(actualHashText, SupportedClientSha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Unsupported Dark Ages client hash: expected {SupportedClientSha256}, " +
                    $"found {actualHashText}.");
            }
        }

        internal static void ApplyModifiersKeyFix(Stream patchStream, nint processHandle)
        {
            var stage = "resolve the suspended client image base";

            try
            {
                var moduleBaseAddress = GetImageBaseAddress(processHandle);
                var callAddress = Add(moduleBaseAddress, ModifiersKeyFixCallRva);
                var patchWindowAddress = Add(callAddress, -PatchVerificationPadding);

                stage = "verify the modifiers-key-fix call site";
                var originalPatchWindow = ReadRemoteBytes(patchStream, patchWindowAddress,
                    PatchVerificationPadding + ExpectedModifiersKeyFixCall.Length + PatchVerificationPadding);

                var actualCall = originalPatchWindow.AsSpan(
                    PatchVerificationPadding, ExpectedModifiersKeyFixCall.Length);

                if (!actualCall.SequenceEqual(ExpectedModifiersKeyFixCall))
                {
                    throw new InvalidDataException($"Unexpected client bytes at 0x{callAddress:X}: " +
                                                   $"expected {Convert.ToHexString(ExpectedModifiersKeyFixCall)}, " +
                                                   $"found {Convert.ToHexString(actualCall)}.");
                }

                stage = "allocate the modifiers-key-fix stub";
                var stubAddress = NativeMethods.VirtualAllocEx(processHandle, nint.Zero,
                    ModifiersKeyFixStubSize,
                    VirtualMemoryAllocationType.Commit | VirtualMemoryAllocationType.Reserve,
                    VirtualMemoryProtection.ReadWrite);

                if (stubAddress == nint.Zero)
                    throw new Win32Exception(Marshal.GetLastPInvokeError());

                var stub = BuildModifiersKeyFixStub(moduleBaseAddress, stubAddress);

                stage = "write the modifiers-key-fix stub";
                WriteRemoteBytes(patchStream, stubAddress, stub);

                stage = "protect and verify the modifiers-key-fix stub";
                if (!NativeMethods.VirtualProtectEx(processHandle, stubAddress, (nuint)stub.Length,
                    VirtualMemoryProtection.ExecuteRead, out _))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                VerifyRemoteBytes(patchStream, stubAddress, stub);

                var replacementCall = BuildModifiersKeyFixCall(callAddress, stubAddress);

                stage = "write the modifiers-key-fix call";
                if (!NativeMethods.VirtualProtectEx(processHandle, callAddress, (nuint)replacementCall.Length,
                    VirtualMemoryProtection.ExecuteReadWrite, out var originalProtection))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                try
                {
                    WriteRemoteBytes(patchStream, callAddress, replacementCall);
                }
                finally
                {
                    if (!NativeMethods.VirtualProtectEx(processHandle, callAddress, (nuint)replacementCall.Length,
                        originalProtection, out _))
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    }
                }

                stage = "verify the modifiers-key-fix call";
                var expectedPatchWindow = originalPatchWindow.ToArray();
                replacementCall.CopyTo(expectedPatchWindow, PatchVerificationPadding);
                VerifyRemoteBytes(patchStream, patchWindowAddress, expectedPatchWindow);

                stage = "flush the modifiers-key-fix instruction cache";
                FlushInstructionCache(processHandle, stubAddress, stub.Length);
                FlushInstructionCache(processHandle, callAddress, replacementCall.Length);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", exception);
            }
        }

        internal static void ApplyShowItemQuantitiesInDialogs(Stream patchStream, nint processHandle)
        {
            var stage = "resolve the suspended client image base";

            try
            {
                var moduleBaseAddress = GetImageBaseAddress(processHandle);
                var callAddress = Add(moduleBaseAddress, ItemQuantityDialogHookRva);

                stage = "verify the dialog item-quantity call site";
                var actualCall = ReadRemoteBytes(patchStream, callAddress, ExpectedItemQuantityDialogCall.Length);
                if (!actualCall.SequenceEqual(ExpectedItemQuantityDialogCall))
                {
                    throw new InvalidDataException($"Unexpected client bytes at 0x{callAddress:X}: " +
                                                   $"expected {Convert.ToHexString(ExpectedItemQuantityDialogCall)}, " +
                                                   $"found {Convert.ToHexString(actualCall)}.");
                }

                stage = "allocate the dialog item-quantity stub";
                var stubAddress = NativeMethods.VirtualAllocEx(processHandle, nint.Zero,
                    (nuint)ShowItemQuantitiesInDialogsStubTemplate.Length,
                    VirtualMemoryAllocationType.Commit | VirtualMemoryAllocationType.Reserve,
                    VirtualMemoryProtection.ReadWrite);

                if (stubAddress == nint.Zero)
                    throw new Win32Exception(Marshal.GetLastPInvokeError());

                var stub = BuildShowItemQuantitiesInDialogsStub(moduleBaseAddress, stubAddress);

                stage = "write the dialog item-quantity stub";
                WriteRemoteBytes(patchStream, stubAddress, stub);

                stage = "protect and verify the dialog item-quantity stub";
                if (!NativeMethods.VirtualProtectEx(processHandle, stubAddress, (nuint)stub.Length,
                    VirtualMemoryProtection.ExecuteRead, out _))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                VerifyRemoteBytes(patchStream, stubAddress, stub);

                var replacementCall = BuildShowItemQuantitiesInDialogsCall(callAddress, stubAddress);

                stage = "write the dialog item-quantity call";
                if (!NativeMethods.VirtualProtectEx(processHandle, callAddress, (nuint)replacementCall.Length,
                    VirtualMemoryProtection.ExecuteReadWrite, out var originalProtection))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                try
                {
                    WriteRemoteBytes(patchStream, callAddress, replacementCall);
                }
                finally
                {
                    if (!NativeMethods.VirtualProtectEx(processHandle, callAddress, (nuint)replacementCall.Length,
                        originalProtection, out _))
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    }
                }

                stage = "verify the dialog item-quantity call";
                VerifyRemoteBytes(patchStream, callAddress, replacementCall);

                stage = "flush the dialog item-quantity instruction cache";
                FlushInstructionCache(processHandle, stubAddress, stub.Length);
                FlushInstructionCache(processHandle, callAddress, replacementCall.Length);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", exception);
            }
        }

        internal static void FlushInstructionCache(nint processHandle)
        {
            if (!NativeMethods.FlushInstructionCache(processHandle, nint.Zero, 0))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        internal static byte[] BuildModifiersKeyFixCall(nint callAddress, nint stubAddress)
        {
            var call = new byte[ExpectedModifiersKeyFixCall.Length];
            call[0] = 0xE8;
            WriteInt32(call, 1, GetRelativeOffset(callAddress, call.Length, stubAddress));
            return call;
        }

        internal static byte[] BuildModifiersKeyFixStub(nint moduleBaseAddress, nint stubAddress)
        {
            using var stream = new MemoryStream(ModifiersKeyFixStubSize);
            using var writer = new BinaryWriter(stream);

            writer.Write((byte)0x9C); // PUSHFD
            writer.Write((byte)0x60); // PUSHAD

            writer.Write((byte)0xE8); // CALL input_get_event_manager
            writer.Write(GetRelativeOffset(Add(stubAddress, checked((int)stream.Position)), sizeof(int),
                Add(moduleBaseAddress, InputGetEventManagerRva)));

            writer.Write(new byte[] { 0x85, 0xC0 }); // TEST EAX, EAX
            writer.Write(new byte[] { 0x74, 0x32 }); // JZ cleanup complete
            writer.Write(new byte[] { 0x89, 0xC3 }); // MOV EBX, EAX

            writer.Write((byte)0xE8); // CALL GetMessageTime import thunk
            writer.Write(GetRelativeOffset(Add(stubAddress, checked((int)stream.Position)), sizeof(int),
                Add(moduleBaseAddress, GetMessageTimeThunkRva)));
            writer.Write(new byte[] { 0x89, 0xC7 }); // MOV EDI, EAX
            writer.Write(new byte[] { 0x31, 0xF6 }); // XOR ESI, ESI

            writer.Write(new byte[] { 0xF6, 0x84, 0x33, 0x34, 0x03, 0x00, 0x00, 0x80 });
            writer.Write(new byte[] { 0x74, 0x0D }); // JZ next scan code
            writer.Write(new byte[] { 0x6A, 0x00 }); // PUSH 0
            writer.Write((byte)0x57); // PUSH EDI
            writer.Write(new byte[] { 0x6A, 0x00 }); // PUSH 0
            writer.Write((byte)0x56); // PUSH ESI
            writer.Write(new byte[] { 0x89, 0xD9 }); // MOV ECX, EBX

            writer.Write((byte)0xE8); // CALL input_post_key_up
            writer.Write(GetRelativeOffset(Add(stubAddress, checked((int)stream.Position)), sizeof(int),
                Add(moduleBaseAddress, InputPostKeyUpRva)));

            writer.Write((byte)0x46); // INC ESI
            writer.Write(new byte[] { 0x81, 0xFE, 0x00, 0x01, 0x00, 0x00 }); // CMP ESI, 256
            writer.Write(new byte[] { 0x7C, 0xE0 }); // JL scan loop
            writer.Write(new byte[] { 0xC6, 0x83, 0x34, 0x04, 0x00, 0x00, 0x00 }); // MOV [EBX + 0x434], 0

            writer.Write((byte)0x61); // POPAD
            writer.Write((byte)0x9D); // POPFD
            writer.Write((byte)0xE9); // JMP original activation function
            writer.Write(GetRelativeOffset(Add(stubAddress, checked((int)stream.Position)), sizeof(int),
                Add(moduleBaseAddress, OriginalActivationFunctionRva)));

            if (stream.Length != ModifiersKeyFixStubSize)
            {
                throw new InvalidOperationException(
                    $"Unexpected modifiers-key-fix stub size: expected {ModifiersKeyFixStubSize}, " +
                    $"found {stream.Length}.");
            }

            return stream.ToArray();
        }

        internal static byte[] BuildShowItemQuantitiesInDialogsStub(nint moduleBaseAddress, nint stubAddress)
        {
            var stub = ShowItemQuantitiesInDialogsStubTemplate.ToArray();

            WriteInt32(stub, 0x36, GetRelativeOffset(Add(stubAddress, 0x36), sizeof(int),
                Add(moduleBaseAddress, DialogUiGetGuiBackPaneRva)));
            WriteUInt32(stub, 0x99, checked((uint)Add(moduleBaseAddress, WsprintfImportAddressRva)));
            WriteInt32(stub, 0xCC, DialogItemLabelMaxBytes);
            WriteInt32(stub, 0xF4, GetRelativeOffset(Add(stubAddress, 0xF4), sizeof(int),
                Add(moduleBaseAddress, UiTextTruncateDbcsSafeRva)));
            WriteInt32(stub, 0x146, GetRelativeOffset(Add(stubAddress, 0x146), sizeof(int),
                Add(moduleBaseAddress, DialogItemNameCopyRva)));

            return stub;
        }

        internal static byte[] BuildShowItemQuantitiesInDialogsCall(nint callAddress, nint stubAddress)
        {
            var call = new byte[ExpectedItemQuantityDialogCall.Length];
            call[0] = 0xE8;
            WriteInt32(call, 1, GetRelativeOffset(callAddress, call.Length, stubAddress));
            return call;
        }

        private static nint GetImageBaseAddress(nint processHandle)
        {
            nint pebAddress;

            if (Environment.Is64BitProcess)
            {
                var status = NativeMethods.NtQueryInformationProcess(processHandle,
                    ProcessInformationClass.Wow64Information, out pebAddress, nint.Size, out _);
                ThrowIfNtStatusFailed(status);

                if (pebAddress == nint.Zero)
                    throw new InvalidOperationException("The suspended client is not a 32-bit process.");
            }
            else
            {
                var status = NativeMethods.NtQueryInformationProcess(processHandle,
                    ProcessInformationClass.BasicInformation, out ProcessBasicInformation processInformation,
                    Marshal.SizeOf<ProcessBasicInformation>(), out _);
                ThrowIfNtStatusFailed(status);
                pebAddress = processInformation.PebBaseAddress;
            }

            var imageBaseBytes = new byte[sizeof(uint)];
            var imageBasePointerAddress = Add(pebAddress, 0x08);
            if (!NativeMethods.ReadProcessMemory(processHandle, imageBasePointerAddress, imageBaseBytes,
                imageBaseBytes.Length, out var bytesRead) || bytesRead != imageBaseBytes.Length)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(),
                    "Could not read the client image base from the suspended process.");
            }

            var imageBaseAddress = (nint)BitConverter.ToUInt32(imageBaseBytes);
            return imageBaseAddress != nint.Zero
                ? imageBaseAddress
                : throw new InvalidOperationException("The suspended client image base is null.");
        }

        private static nint Add(nint address, int offset) => checked(address + offset);

        private static int GetRelativeOffset(nint instructionAddress, int instructionSize, nint targetAddress) =>
            checked((int)(targetAddress - instructionAddress - instructionSize));

        private static byte[] ReadRemoteBytes(Stream patchStream, nint address, int size)
        {
            var bytes = new byte[size];
            patchStream.Position = address;
            patchStream.ReadExactly(bytes);
            return bytes;
        }

        private static void WriteRemoteBytes(Stream patchStream, nint address, byte[] bytes)
        {
            patchStream.Position = address;
            patchStream.Write(bytes, 0, bytes.Length);
        }

        private static void VerifyRemoteBytes(Stream patchStream, nint address, byte[] expected)
        {
            var actual = ReadRemoteBytes(patchStream, address, expected.Length);
            if (!actual.SequenceEqual(expected))
            {
                throw new InvalidDataException($"Client memory verification failed at 0x{address:X}: " +
                                               $"expected {Convert.ToHexString(expected)}, " +
                                               $"found {Convert.ToHexString(actual)}.");
            }
        }

        private static void FlushInstructionCache(nint processHandle, nint address, int size)
        {
            if (!NativeMethods.FlushInstructionCache(processHandle, address, (nuint)size))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        private static void ThrowIfNtStatusFailed(int status)
        {
            if (status >= 0)
                return;

            var error = NativeMethods.RtlNtStatusToDosError(status);
            throw new Win32Exception(checked((int)error));
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }
    }
}
