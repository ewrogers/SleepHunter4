using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal static partial class ClientPatcher
    {
        private const int ExchangeDialogDraggableRva = 0x00069E33;
        private const int ExchangeCancelledHandlerRva = 0x0006A9E0;
        private const int ExchangeCancelledHandlerContinuationRva = 0x0006A9E5;
        private const int ExchangeCancelledAlertRva = 0x0006AA81;
        private const int ExchangeAcceptedHandlerRva = 0x0006AB20;
        private const int ExchangeAcceptedHandlerContinuationRva = 0x0006AB25;
        private const int ExchangeAcceptedAlertRva = 0x0006AC57;
        private const int FloatingPaletteAppendRva = 0x000803A0;
        private const int FloatingPaletteNewlineRva = 0x0028BC68;

        private const int ExchangeResultMessageMaxBytes = 130;
        private const int ExchangeResultStubAllocationSize = 256;

        private static readonly byte[] ExpectedExchangeDialogDraggable =
            new byte[] { 0xC7, 0x82, 0x2C, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        private static readonly byte[] ExchangeDialogDraggableReplacement =
            new byte[] { 0xC7, 0x82, 0x2C, 0x06, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };

        private static readonly byte[] ExpectedExchangeResultHandlerHook =
            new byte[] { 0x55, 0x8B, 0xEC, 0x6A, 0xFF };

        private static readonly byte[] ExpectedExchangeCancelledAlert =
            new byte[] { 0x6A, 0x00, 0x68, 0x34, 0x06, 0x00, 0x00, 0xE8, 0x43, 0x9A, 0x04, 0x00 };

        private static readonly byte[] ExpectedExchangeAcceptedAlert =
            new byte[] { 0x6A, 0x00, 0x68, 0x34, 0x06, 0x00, 0x00, 0xE8, 0x6D, 0x98, 0x04, 0x00 };

        private static readonly byte[] SuppressExchangeAlertReplacement =
            new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };

        internal static void ApplyMakeExchangeDialogDraggable(Stream patchStream, nint processHandle)
        {
            var stage = "resolve the suspended client image base";

            try
            {
                var moduleBaseAddress = GetImageBaseAddress(processHandle);
                var address = Add(moduleBaseAddress, ExchangeDialogDraggableRva);

                stage = "verify the exchange dialog drag setting";
                VerifyRemoteBytes(patchStream, address, ExpectedExchangeDialogDraggable);

                stage = "write the exchange dialog drag setting";
                var writeStarted = false;
                WriteExchangeUiPatch(patchStream, processHandle, address,
                    ExchangeDialogDraggableReplacement, ref writeStarted);

                stage = "verify the exchange dialog drag setting";
                VerifyRemoteBytes(patchStream, address, ExchangeDialogDraggableReplacement);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", exception);
            }
        }

        internal static void ApplyShowExchangeResultsInMessageBar(Stream patchStream, nint processHandle)
        {
            var stage = "resolve the suspended client image base";
            var cancelledStubAddress = nint.Zero;
            var acceptedStubAddress = nint.Zero;
            var cancelledAlertWriteStarted = false;
            var acceptedAlertWriteStarted = false;
            var cancelledHookWriteStarted = false;
            var acceptedHookWriteStarted = false;
            var moduleBaseAddress = nint.Zero;

            try
            {
                moduleBaseAddress = GetImageBaseAddress(processHandle);
                var cancelledHookAddress = Add(moduleBaseAddress, ExchangeCancelledHandlerRva);
                var acceptedHookAddress = Add(moduleBaseAddress, ExchangeAcceptedHandlerRva);
                var cancelledAlertAddress = Add(moduleBaseAddress, ExchangeCancelledAlertRva);
                var acceptedAlertAddress = Add(moduleBaseAddress, ExchangeAcceptedAlertRva);

                stage = "verify the exchange result hooks";
                VerifyRemoteBytes(patchStream, cancelledHookAddress, ExpectedExchangeResultHandlerHook);
                VerifyRemoteBytes(patchStream, acceptedHookAddress, ExpectedExchangeResultHandlerHook);
                VerifyRemoteBytes(patchStream, cancelledAlertAddress, ExpectedExchangeCancelledAlert);
                VerifyRemoteBytes(patchStream, acceptedAlertAddress, ExpectedExchangeAcceptedAlert);

                stage = "allocate the exchange result stubs";
                cancelledStubAddress = AllocateExchangeResultStub(processHandle);
                acceptedStubAddress = AllocateExchangeResultStub(processHandle);
                var cancelledStub = BuildExchangeResultHandlerStub(moduleBaseAddress, cancelledStubAddress, false);
                var acceptedStub = BuildExchangeResultHandlerStub(moduleBaseAddress, acceptedStubAddress, true);

                stage = "write the exchange result stubs";
                WriteExchangeResultStub(patchStream, processHandle, cancelledStubAddress, cancelledStub);
                WriteExchangeResultStub(patchStream, processHandle, acceptedStubAddress, acceptedStub);

                var cancelledHook = BuildExchangeEntryHook(cancelledHookAddress, cancelledStubAddress);
                var acceptedHook = BuildExchangeEntryHook(acceptedHookAddress, acceptedStubAddress);

                stage = "write the exchange result alerts";
                WriteExchangeUiPatch(patchStream, processHandle, cancelledAlertAddress,
                    SuppressExchangeAlertReplacement, ref cancelledAlertWriteStarted);
                WriteExchangeUiPatch(patchStream, processHandle, acceptedAlertAddress,
                    SuppressExchangeAlertReplacement, ref acceptedAlertWriteStarted);

                stage = "write the exchange result hooks";
                WriteExchangeUiPatch(patchStream, processHandle, cancelledHookAddress, cancelledHook,
                    ref cancelledHookWriteStarted);
                WriteExchangeUiPatch(patchStream, processHandle, acceptedHookAddress, acceptedHook,
                    ref acceptedHookWriteStarted);

                stage = "verify the exchange result hooks";
                VerifyRemoteBytes(patchStream, cancelledAlertAddress, SuppressExchangeAlertReplacement);
                VerifyRemoteBytes(patchStream, acceptedAlertAddress, SuppressExchangeAlertReplacement);
                VerifyRemoteBytes(patchStream, cancelledHookAddress, cancelledHook);
                VerifyRemoteBytes(patchStream, acceptedHookAddress, acceptedHook);
            }
            catch (Exception exception)
            {
                var cleanupExceptions = new List<Exception>();
                var cancelledHookRestored = !cancelledHookWriteStarted;
                var acceptedHookRestored = !acceptedHookWriteStarted;

                if (moduleBaseAddress != nint.Zero)
                {
                    TryRestoreExchangeUiPatch(patchStream, processHandle,
                        Add(moduleBaseAddress, ExchangeAcceptedHandlerRva), ExpectedExchangeResultHandlerHook,
                        acceptedHookWriteStarted, cleanupExceptions, ref acceptedHookRestored);
                    TryRestoreExchangeUiPatch(patchStream, processHandle,
                        Add(moduleBaseAddress, ExchangeCancelledHandlerRva), ExpectedExchangeResultHandlerHook,
                        cancelledHookWriteStarted, cleanupExceptions, ref cancelledHookRestored);

                    var ignored = false;
                    TryRestoreExchangeUiPatch(patchStream, processHandle,
                        Add(moduleBaseAddress, ExchangeAcceptedAlertRva), ExpectedExchangeAcceptedAlert,
                        acceptedAlertWriteStarted, cleanupExceptions, ref ignored);
                    ignored = false;
                    TryRestoreExchangeUiPatch(patchStream, processHandle,
                        Add(moduleBaseAddress, ExchangeCancelledAlertRva), ExpectedExchangeCancelledAlert,
                        cancelledAlertWriteStarted, cleanupExceptions, ref ignored);
                }

                TryFreeExchangeResultStub(processHandle, acceptedStubAddress, acceptedHookRestored,
                    cleanupExceptions);
                TryFreeExchangeResultStub(processHandle, cancelledStubAddress, cancelledHookRestored,
                    cleanupExceptions);

                var innerException = cleanupExceptions.Count == 0
                    ? exception
                    : new AggregateException(new[] { exception }.Concat(cleanupExceptions));
                throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", innerException);
            }
        }

        internal static byte[] BuildExchangeResultHandlerStub(nint moduleBaseAddress, nint stubAddress,
            bool accepted)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(new byte[] { 0x55, 0x89, 0xE5 }); // PUSH EBP; MOV EBP, ESP
            writer.Write(new byte[] { 0x81, 0xEC, 0x88, 0x00, 0x00, 0x00 }); // SUB ESP, 0x88
            writer.Write(new byte[] { 0x53, 0x56, 0x57 }); // PUSH EBX; PUSH ESI; PUSH EDI
            writer.Write(new byte[] { 0x89, 0x4D, 0xFC }); // MOV [EBP - 4], ECX
            writer.Write(new byte[] { 0x8B, 0x5D, 0x08 }); // MOV EBX, [EBP + 8]

            var skipMessageJumps = new List<int>();
            if (accepted)
            {
                writer.Write(new byte[] { 0x80, 0x7B, 0x02, 0x00 }); // CMP BYTE PTR [EBX + 2], 0
                var nonLocalPartyJump = WriteNearJumpPlaceholder(writer, 0x85); // JNE non-local party

                writer.Write(new byte[] { 0x8B, 0x45, 0xFC }); // MOV EAX, [EBP - 4]
                writer.Write(new byte[] { 0x80, 0xB8, 0x36, 0x06, 0x00, 0x00, 0x01 }); // CMP [EAX + 0x636], 1
                skipMessageJumps.Add(WriteNearJumpPlaceholder(writer, 0x85));
                var displayMessageJump = WriteNearJumpPlaceholder(writer, null);

                var nonLocalPartyOffset = checked((int)stream.Position);
                PatchLocalBranch(stream, nonLocalPartyJump, nonLocalPartyOffset);
                writer.Write(new byte[] { 0x8B, 0x45, 0xFC }); // MOV EAX, [EBP - 4]
                writer.Write(new byte[] { 0x80, 0xB8, 0x35, 0x06, 0x00, 0x00, 0x01 }); // CMP [EAX + 0x635], 1
                skipMessageJumps.Add(WriteNearJumpPlaceholder(writer, 0x85));

                PatchLocalBranch(stream, displayMessageJump, checked((int)stream.Position));
            }

            writer.Write(new byte[] { 0x0F, 0xB6, 0x4B, 0x03 }); // MOVZX ECX, BYTE PTR [EBX + 3]
            writer.Write(new byte[] { 0x81, 0xF9 }); // CMP ECX, 130
            writer.Write(ExchangeResultMessageMaxBytes);
            writer.Write(new byte[] { 0x76, 0x05 }); // JBE copy message
            writer.Write((byte)0xB9); // MOV ECX, 130
            writer.Write(ExchangeResultMessageMaxBytes);
            writer.Write(new byte[] { 0x8D, 0x73, 0x04 }); // LEA ESI, [EBX + 4]
            writer.Write(new byte[] { 0x8D, 0xBD, 0x78, 0xFF, 0xFF, 0xFF }); // LEA EDI, [EBP - 0x88]
            writer.Write(new byte[] { 0xFC, 0xF3, 0xA4 }); // CLD; REP MOVSB
            writer.Write(new byte[] { 0xC6, 0x07, 0x00 }); // MOV BYTE PTR [EDI], 0

            writer.Write(new byte[] { 0x6A, 0x58 }); // PUSH palette 0x58
            writer.Write(new byte[] { 0x8D, 0x85, 0x78, 0xFF, 0xFF, 0xFF }); // LEA EAX, [EBP - 0x88]
            writer.Write((byte)0x50); // PUSH EAX
            WriteRelativeCall(writer, stubAddress, Add(moduleBaseAddress, FloatingPaletteAppendRva));
            writer.Write(new byte[] { 0x83, 0xC4, 0x08 }); // ADD ESP, 8

            writer.Write(new byte[] { 0x6A, 0x58 }); // PUSH palette 0x58
            writer.Write((byte)0x68); // PUSH normal newline
            WriteAddress(writer, Add(moduleBaseAddress, FloatingPaletteNewlineRva));
            WriteRelativeCall(writer, stubAddress, Add(moduleBaseAddress, FloatingPaletteAppendRva));
            writer.Write(new byte[] { 0x83, 0xC4, 0x08 }); // ADD ESP, 8

            var skipMessageOffset = checked((int)stream.Position);
            foreach (var jump in skipMessageJumps)
                PatchLocalBranch(stream, jump, skipMessageOffset);

            writer.Write(new byte[] { 0x5F, 0x5E, 0x5B }); // POP EDI; POP ESI; POP EBX
            writer.Write(new byte[] { 0x8B, 0x4D, 0xFC }); // MOV ECX, [EBP - 4]
            writer.Write(new byte[] { 0x89, 0xEC, 0x5D }); // MOV ESP, EBP; POP EBP
            writer.Write(ExpectedExchangeResultHandlerHook);
            WriteRelativeJump(writer, stubAddress, Add(moduleBaseAddress,
                accepted ? ExchangeAcceptedHandlerContinuationRva : ExchangeCancelledHandlerContinuationRva));

            var stub = stream.ToArray();
            if (stub.Length > ExchangeResultStubAllocationSize)
            {
                throw new InvalidOperationException(
                    $"Exchange result stub exceeds its {ExchangeResultStubAllocationSize}-byte allocation.");
            }

            return stub;
        }

        internal static byte[] BuildExchangeEntryHook(nint hookAddress, nint stubAddress)
        {
            var hook = new byte[ExpectedExchangeResultHandlerHook.Length];
            hook[0] = 0xE9;
            WriteInt32(hook, 1, GetRelativeOffset(hookAddress, hook.Length, stubAddress));
            return hook;
        }

        internal static byte[] GetExchangeDialogDraggableReplacement() =>
            ExchangeDialogDraggableReplacement.ToArray();

        internal static byte[] GetSuppressExchangeAlertReplacement() =>
            SuppressExchangeAlertReplacement.ToArray();

        private static nint AllocateExchangeResultStub(nint processHandle)
        {
            var address = NativeMethods.VirtualAllocEx(processHandle, nint.Zero,
                ExchangeResultStubAllocationSize,
                VirtualMemoryAllocationType.Commit | VirtualMemoryAllocationType.Reserve,
                VirtualMemoryProtection.ReadWrite);

            return address != nint.Zero
                ? address
                : throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        private static void WriteExchangeResultStub(Stream patchStream, nint processHandle, nint address,
            byte[] stub)
        {
            WriteRemoteBytes(patchStream, address, stub);
            if (!NativeMethods.VirtualProtectEx(processHandle, address, (nuint)stub.Length,
                VirtualMemoryProtection.ExecuteRead, out _))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            VerifyRemoteBytes(patchStream, address, stub);
            FlushInstructionCache(processHandle, address, stub.Length);
        }

        private static void WriteExchangeUiPatch(Stream patchStream, nint processHandle, nint address,
            byte[] bytes, ref bool writeStarted)
        {
            if (!NativeMethods.VirtualProtectEx(processHandle, address, (nuint)bytes.Length,
                VirtualMemoryProtection.ExecuteReadWrite, out var originalProtection))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            try
            {
                writeStarted = true;
                WriteRemoteBytes(patchStream, address, bytes);
            }
            finally
            {
                if (!NativeMethods.VirtualProtectEx(processHandle, address, (nuint)bytes.Length,
                    originalProtection, out _))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }
            }

            FlushInstructionCache(processHandle, address, bytes.Length);
        }

        private static void TryRestoreExchangeUiPatch(Stream patchStream, nint processHandle, nint address,
            byte[] original, bool writeStarted, List<Exception> cleanupExceptions, ref bool restored)
        {
            if (!writeStarted)
                return;

            try
            {
                var restoreStarted = false;
                WriteExchangeUiPatch(patchStream, processHandle, address, original, ref restoreStarted);
                VerifyRemoteBytes(patchStream, address, original);
                restored = true;
            }
            catch (Exception exception)
            {
                cleanupExceptions.Add(exception);
            }
        }

        private static void TryFreeExchangeResultStub(nint processHandle, nint address, bool safeToFree,
            List<Exception> cleanupExceptions)
        {
            if (address == nint.Zero || !safeToFree)
                return;

            try
            {
                if (!NativeMethods.VirtualFreeEx(processHandle, address, 0, VirtualMemoryFreeType.Release))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            catch (Exception exception)
            {
                cleanupExceptions.Add(exception);
            }
        }

        private static void WriteRelativeCall(BinaryWriter writer, nint stubAddress, nint targetAddress)
        {
            var instructionOffset = checked((int)writer.BaseStream.Position);
            writer.Write((byte)0xE8);
            writer.Write(GetRelativeOffset(Add(stubAddress, instructionOffset), 5, targetAddress));
        }

        private static void WriteRelativeJump(BinaryWriter writer, nint stubAddress, nint targetAddress)
        {
            var instructionOffset = checked((int)writer.BaseStream.Position);
            writer.Write((byte)0xE9);
            writer.Write(GetRelativeOffset(Add(stubAddress, instructionOffset), 5, targetAddress));
        }

        private static int WriteNearJumpPlaceholder(BinaryWriter writer, byte? condition)
        {
            if (condition is null)
            {
                writer.Write((byte)0xE9);
            }
            else
            {
                writer.Write((byte)0x0F);
                writer.Write(condition.Value);
            }

            var operandOffset = checked((int)writer.BaseStream.Position);
            writer.Write(0);
            return operandOffset;
        }

        private static void PatchLocalBranch(MemoryStream stream, int operandOffset, int targetOffset)
        {
            var returnPosition = stream.Position;
            stream.Position = operandOffset;
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
                writer.Write(checked(targetOffset - operandOffset - sizeof(int)));

            stream.Position = returnPosition;
        }

        private static void WriteAddress(BinaryWriter writer, nint address) =>
            writer.Write(checked((uint)address));
    }
}
