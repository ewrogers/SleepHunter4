using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal static partial class ClientPatcher
    {
        private const int AutoFollowStateSize = 0x18;
        private const int AutoFollowActiveDistanceOffset = 0x0C;
        private const int AutoFollowShiftDistanceOffset = 0x10;
        private const int AutoFollowMinimumDistance = 1;
        private const int AutoFollowMaximumDistance = 10;

        private const int AutoFollowSelectHookRva = 0x001EF33A;
        private const int AutoFollowShiftDispatchHookRva = 0x001D48DF;
        private const int AutoFollowRouteHookRva = 0x001F49E5;
        private const int AutoFollowGenerationHookRva = 0x001F4AA2;
        private const int AutoFollowAttackHookRva = 0x001F4D0E;

        private const int AutoFollowSelectStubOffset = 0x000;
        private const int AutoFollowAttackStubOffset = 0x044;
        private const int AutoFollowGenerationStubOffset = 0x072;
        private const int AutoFollowRouteStubOffset = 0x0A8;
        private const int AutoFollowGenerationReplayStubOffset = 0x190;
        private const int AutoFollowShiftDispatchStubOffset = 0x19B;

        private const int AutoFollowPursuitRva = 0x001F4A70;
        private const int AutoFollowSendAttackRva = 0x001F44B0;
        private const int AutoFollowGenerationNonZeroRva = 0x001F4AAD;
        private const int AutoFollowGenerationZeroRva = 0x001F4AA8;
        private const int AutoFollowResetMovementRva = 0x001F4900;
        private const int AutoFollowRouteEpilogueRva = 0x001F4A5B;
        private const int AutoFollowRouteContinuationRva = 0x001F49EF;
        private const int AutoFollowShiftDispatchAllowedRva = 0x001D48F1;
        private const int AutoFollowShiftDispatchNativeFilterRva = 0x001D48E5;

        private static readonly byte[] ExpectedAutoFollowSelectHook =
            new byte[] { 0xE8, 0x31, 0x57, 0x00, 0x00 };

        private static readonly byte[] ExpectedAutoFollowShiftDispatchHook =
            new byte[] { 0x83, 0x7D, 0xA0, 0x08, 0x74, 0x0C };

        private static readonly byte[] ExpectedAutoFollowRouteHook =
            new byte[] { 0x8B, 0x45, 0xE0, 0x83, 0xB8, 0xB8, 0x02, 0x00, 0x00, 0x00 };

        private static readonly byte[] ExpectedAutoFollowGenerationHook =
            new byte[] { 0x83, 0x7D, 0x08, 0x00, 0x75, 0x05 };

        private static readonly byte[] ExpectedAutoFollowAttackHook =
            new byte[] { 0xE8, 0x9D, 0xF7, 0xFF, 0xFF };

        private static readonly byte[] AutoFollowStubTemplate = Convert.FromHexString(
            "8B450CF6402C04742F890D000000008B442404A300000000A100000000A300000000A000000000A200000000C6050000000001E900000000C6050000000000E9" +
            "00000000803D00000000007420390D0000000075188B81C80200003B0500000000750A803D00000000007501C3E900000000803D000000000074248B45B43B05" +
            "0000000075198B45083B0500000000750E8B45B48B80C8020000A300000000837D0800E900000000803D0000000000745E8B45E03B050000000075538B90C802" +
            "00003B150000000074198B0D000000004139CA753A8B0D000000003988BC020000752C8B90B802000083C2018B0D0000000083F9017D05B90100000039CA7F0F" +
            "6A018B4DE0E800000000E9000000008B45E083B8B802000000E9000000005589E58B4D08890D000000008B450CA3000000008B551083FA017D05BA0100000081" +
            "FAFF0000007E05BAFF0000008915000000008B451485C00F95C0A200000000C6050000000001FF750C8B4D08E80000000089EC5DC21000C60500000000008B4C" +
            "24046A00E800000000C2040000000000" +
            "0F8500000000E900000000" +
            "837DA0080F8400000000807DC3000F84000000008B450C80780C050F8500000000837DA0010F8400000000837DA0020F8400000000E900000000");

        internal static void ApplyImprovedAutoFollow(Stream patchStream, nint processHandle, int minimumDistance)
        {
            var stage = "resolve the suspended client image base";
            var moduleBaseAddress = nint.Zero;
            var stateAddress = nint.Zero;
            var stubAddress = nint.Zero;
            var selectHookWriteStarted = false;
            var shiftDispatchHookWriteStarted = false;
            var routeHookWriteStarted = false;
            var generationHookWriteStarted = false;
            var attackHookWriteStarted = false;

            try
            {
                moduleBaseAddress = GetImageBaseAddress(processHandle);
                var selectHookAddress = Add(moduleBaseAddress, AutoFollowSelectHookRva);
                var shiftDispatchHookAddress = Add(moduleBaseAddress, AutoFollowShiftDispatchHookRva);
                var routeHookAddress = Add(moduleBaseAddress, AutoFollowRouteHookRva);
                var generationHookAddress = Add(moduleBaseAddress, AutoFollowGenerationHookRva);
                var attackHookAddress = Add(moduleBaseAddress, AutoFollowAttackHookRva);

                stage = "verify the improved auto-follow hooks";
                VerifyRemoteBytes(patchStream, selectHookAddress, ExpectedAutoFollowSelectHook);
                VerifyRemoteBytes(patchStream, shiftDispatchHookAddress, ExpectedAutoFollowShiftDispatchHook);
                VerifyRemoteBytes(patchStream, routeHookAddress, ExpectedAutoFollowRouteHook);
                VerifyRemoteBytes(patchStream, generationHookAddress, ExpectedAutoFollowGenerationHook);
                VerifyRemoteBytes(patchStream, attackHookAddress, ExpectedAutoFollowAttackHook);

                stage = "allocate the improved auto-follow state and stub";
                var expectedState = BuildImprovedAutoFollowState(minimumDistance);
                stateAddress = AllocateAutoFollowMemory(processHandle, expectedState.Length);
                stubAddress = AllocateAutoFollowMemory(processHandle, AutoFollowStubTemplate.Length);
                WriteRemoteBytes(patchStream, stateAddress, expectedState);

                var stub = BuildImprovedAutoFollowStub(moduleBaseAddress, stubAddress, stateAddress);

                stage = "write the improved auto-follow stub";
                WriteRemoteBytes(patchStream, stubAddress, stub);

                stage = "protect and verify the improved auto-follow state and stub";
                ProtectAutoFollowStub(patchStream, processHandle, stubAddress, stub);
                VerifyRemoteBytes(patchStream, stateAddress, expectedState);
                FlushInstructionCache(processHandle, stubAddress, stub.Length);

                var selectHook = BuildImprovedAutoFollowHook(selectHookAddress, stubAddress,
                    AutoFollowSelectStubOffset, 0xE8, ExpectedAutoFollowSelectHook.Length);
                var shiftDispatchHook = BuildImprovedAutoFollowHook(shiftDispatchHookAddress, stubAddress,
                    AutoFollowShiftDispatchStubOffset, 0xE9, ExpectedAutoFollowShiftDispatchHook.Length);
                var routeHook = BuildImprovedAutoFollowHook(routeHookAddress, stubAddress,
                    AutoFollowRouteStubOffset, 0xE9, ExpectedAutoFollowRouteHook.Length);
                var generationHook = BuildImprovedAutoFollowHook(generationHookAddress, stubAddress,
                    AutoFollowGenerationStubOffset, 0xE9, ExpectedAutoFollowGenerationHook.Length);
                var attackHook = BuildImprovedAutoFollowHook(attackHookAddress, stubAddress,
                    AutoFollowAttackStubOffset, 0xE8, ExpectedAutoFollowAttackHook.Length);

                stage = "write the improved auto-follow selection hook";
                WriteAutoFollowHook(patchStream, processHandle, selectHookAddress, selectHook,
                    ref selectHookWriteStarted);

                stage = "write the improved auto-follow route hook";
                WriteAutoFollowHook(patchStream, processHandle, routeHookAddress, routeHook,
                    ref routeHookWriteStarted);

                stage = "write the improved auto-follow generation hook";
                WriteAutoFollowHook(patchStream, processHandle, generationHookAddress, generationHook,
                    ref generationHookWriteStarted);

                stage = "write the improved auto-follow attack hook";
                WriteAutoFollowHook(patchStream, processHandle, attackHookAddress, attackHook,
                    ref attackHookWriteStarted);

                stage = "write the improved auto-follow Shift dispatch hook";
                WriteAutoFollowHook(patchStream, processHandle, shiftDispatchHookAddress, shiftDispatchHook,
                    ref shiftDispatchHookWriteStarted);

                stage = "verify the improved auto-follow hooks";
                VerifyRemoteBytes(patchStream, selectHookAddress, selectHook);
                VerifyRemoteBytes(patchStream, shiftDispatchHookAddress, shiftDispatchHook);
                VerifyRemoteBytes(patchStream, routeHookAddress, routeHook);
                VerifyRemoteBytes(patchStream, generationHookAddress, generationHook);
                VerifyRemoteBytes(patchStream, attackHookAddress, attackHook);
            }
            catch (Exception exception)
            {
                var cleanupExceptions = new List<Exception>();
                var selectHookRestored = !selectHookWriteStarted;
                var shiftDispatchHookRestored = !shiftDispatchHookWriteStarted;
                var routeHookRestored = !routeHookWriteStarted;
                var generationHookRestored = !generationHookWriteStarted;
                var attackHookRestored = !attackHookWriteStarted;

                if (moduleBaseAddress != nint.Zero)
                {
                    TryRestoreAutoFollowHook(patchStream, processHandle,
                        Add(moduleBaseAddress, AutoFollowShiftDispatchHookRva),
                        ExpectedAutoFollowShiftDispatchHook, shiftDispatchHookWriteStarted, cleanupExceptions,
                        ref shiftDispatchHookRestored);
                    TryRestoreAutoFollowHook(patchStream, processHandle,
                        Add(moduleBaseAddress, AutoFollowAttackHookRva),
                        ExpectedAutoFollowAttackHook, attackHookWriteStarted, cleanupExceptions,
                        ref attackHookRestored);
                    TryRestoreAutoFollowHook(patchStream, processHandle,
                        Add(moduleBaseAddress, AutoFollowGenerationHookRva),
                        ExpectedAutoFollowGenerationHook, generationHookWriteStarted, cleanupExceptions,
                        ref generationHookRestored);
                    TryRestoreAutoFollowHook(patchStream, processHandle,
                        Add(moduleBaseAddress, AutoFollowRouteHookRva),
                        ExpectedAutoFollowRouteHook, routeHookWriteStarted, cleanupExceptions,
                        ref routeHookRestored);
                    TryRestoreAutoFollowHook(patchStream, processHandle,
                        Add(moduleBaseAddress, AutoFollowSelectHookRva),
                        ExpectedAutoFollowSelectHook, selectHookWriteStarted, cleanupExceptions,
                        ref selectHookRestored);
                }

                var allHooksRestored = selectHookRestored && shiftDispatchHookRestored && routeHookRestored &&
                                       generationHookRestored && attackHookRestored;
                TryFreeAutoFollowAllocation(processHandle, stubAddress, allHooksRestored, cleanupExceptions);
                TryFreeAutoFollowAllocation(processHandle, stateAddress, allHooksRestored, cleanupExceptions);

                Exception innerException = exception;
                if (cleanupExceptions.Count > 0)
                {
                    var exceptions = new List<Exception> { exception };
                    exceptions.AddRange(cleanupExceptions);
                    innerException = new AggregateException(exceptions);
                }

                throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", innerException);
            }
        }

        internal static byte[] BuildImprovedAutoFollowState(int minimumDistance)
        {
            if (minimumDistance < AutoFollowMinimumDistance || minimumDistance > AutoFollowMaximumDistance)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumDistance), minimumDistance,
                    $"Improved auto-follow distance must be between {AutoFollowMinimumDistance} " +
                    $"and {AutoFollowMaximumDistance} tiles.");
            }

            var state = new byte[AutoFollowStateSize];
            WriteInt32(state, AutoFollowActiveDistanceOffset, minimumDistance);
            WriteInt32(state, AutoFollowShiftDistanceOffset, minimumDistance);
            return state;
        }

        internal static byte[] BuildImprovedAutoFollowStub(nint moduleBaseAddress, nint stubAddress,
            nint stateAddress)
        {
            var stub = new byte[AutoFollowStubTemplate.Length];
            AutoFollowStubTemplate.CopyTo(stub, 0);

            WriteAutoFollowAddresses(stub, Add(stateAddress, 0x00), 0x00B, 0x04F, 0x080, 0x0B6, 0x126);
            WriteAutoFollowAddresses(stub, Add(stateAddress, 0x04), 0x014, 0x08B, 0x0D7, 0x12E);
            WriteAutoFollowAddresses(stub, Add(stateAddress, 0x08), 0x05D, 0x09B, 0x0C4, 0x0CC);
            WriteAutoFollowAddresses(stub, Add(stateAddress, 0x0C), 0x01E, 0x0EE, 0x14E);
            WriteAutoFollowAddresses(stub, Add(stateAddress, 0x10), 0x019);
            WriteAutoFollowAddresses(stub, Add(stateAddress, 0x14),
                0x02E, 0x03A, 0x046, 0x074, 0x0AA, 0x161, 0x179);
            WriteAutoFollowAddresses(stub, Add(stateAddress, 0x15), 0x028, 0x065, 0x15B);
            WriteAutoFollowAddresses(stub, Add(stateAddress, 0x16), 0x023);

            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowPursuitRva), 0x034, 0x040, 0x16D);
            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowSendAttackRva), 0x06E);

            // Replay the generation branch outside the bytes replaced by the generation hook.
            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(stubAddress, AutoFollowGenerationReplayStubOffset), 0x0A4);
            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowGenerationNonZeroRva), 0x192);
            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowGenerationZeroRva), 0x197);

            // Admit only the Shift-modified pursuit gesture for players and monsters.
            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowShiftDispatchAllowedRva), 0x1A1, 0x1C2, 0x1CC);
            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowShiftDispatchNativeFilterRva), 0x1AB, 0x1B8, 0x1D1);

            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowResetMovementRva), 0x106, 0x185);
            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowRouteEpilogueRva), 0x10B);
            WriteAutoFollowRelativeOffsets(stub, stubAddress,
                Add(moduleBaseAddress, AutoFollowRouteContinuationRva), 0x11A);

            return stub;
        }

        internal static byte[] BuildImprovedAutoFollowHook(nint hookAddress, nint stubAddress,
            int stubOffset, byte opcode, int hookLength)
        {
            var hook = new byte[hookLength];
            Array.Fill(hook, (byte)0x90);
            hook[0] = opcode;
            WriteInt32(hook, 1, GetRelativeOffset(hookAddress, 5, Add(stubAddress, stubOffset)));
            return hook;
        }

        private static void WriteAutoFollowAddresses(byte[] stub, nint address, params int[] offsets)
        {
            var absoluteAddress = checked((uint)address.ToInt64());
            foreach (var offset in offsets)
                WriteUInt32(stub, offset, absoluteAddress);
        }

        private static void WriteAutoFollowRelativeOffsets(byte[] stub, nint stubAddress,
            nint targetAddress, params int[] offsets)
        {
            foreach (var offset in offsets)
            {
                WriteInt32(stub, offset,
                    GetRelativeOffset(Add(stubAddress, offset), sizeof(int), targetAddress));
            }
        }

        private static nint AllocateAutoFollowMemory(nint processHandle, int size)
        {
            var address = NativeMethods.VirtualAllocEx(processHandle, nint.Zero, (nuint)size,
                VirtualMemoryAllocationType.Commit | VirtualMemoryAllocationType.Reserve,
                VirtualMemoryProtection.ReadWrite);

            if (address == nint.Zero)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            return address;
        }

        private static void ProtectAutoFollowStub(Stream patchStream, nint processHandle, nint address, byte[] stub)
        {
            if (!NativeMethods.VirtualProtectEx(processHandle, address, (nuint)stub.Length,
                VirtualMemoryProtection.ExecuteRead, out _))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            VerifyRemoteBytes(patchStream, address, stub);
        }

        private static void WriteAutoFollowHook(Stream patchStream, nint processHandle, nint address,
            byte[] hook, ref bool writeStarted)
        {
            if (!NativeMethods.VirtualProtectEx(processHandle, address, (nuint)hook.Length,
                VirtualMemoryProtection.ExecuteReadWrite, out var originalProtection))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            try
            {
                writeStarted = true;
                WriteRemoteBytes(patchStream, address, hook);
                FlushInstructionCache(processHandle, address, hook.Length);
            }
            finally
            {
                if (!NativeMethods.VirtualProtectEx(processHandle, address, (nuint)hook.Length,
                    originalProtection, out _))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }
            }
        }

        private static void TryRestoreAutoFollowHook(Stream patchStream, nint processHandle, nint hookAddress,
            byte[] expected, bool writeStarted, List<Exception> cleanupExceptions, ref bool restored)
        {
            if (!writeStarted)
                return;

            try
            {
                if (!NativeMethods.VirtualProtectEx(processHandle, hookAddress, (nuint)expected.Length,
                    VirtualMemoryProtection.ExecuteReadWrite, out var originalProtection))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                try
                {
                    WriteRemoteBytes(patchStream, hookAddress, expected);
                    FlushInstructionCache(processHandle, hookAddress, expected.Length);
                }
                finally
                {
                    if (!NativeMethods.VirtualProtectEx(processHandle, hookAddress, (nuint)expected.Length,
                        originalProtection, out _))
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    }
                }

                VerifyRemoteBytes(patchStream, hookAddress, expected);
                restored = true;
            }
            catch (Exception exception)
            {
                cleanupExceptions.Add(exception);
            }
        }

        private static void TryFreeAutoFollowAllocation(nint processHandle, nint address, bool safeToFree,
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
    }
}
