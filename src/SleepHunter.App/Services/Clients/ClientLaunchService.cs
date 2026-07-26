using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SleepHunter.IO.Process;
using SleepHunter.Services.Logging;
using SleepHunter.Settings;
using SleepHunter.Win32;

namespace SleepHunter.Services.Clients
{
    public sealed class ClientLaunchService :
        IClientLaunchService
    {
        private static readonly (
            long Address,
            byte[] Expected,
            byte[] Replacement)[]
            SuppressLoginNotificationPatches =
            {
                (
                    0x4B897C,
                    new byte[] { 0x75, 0x6C },
                    new byte[] { 0xEB, 0x6C }),
                (
                    0x4B8ACF,
                    new byte[] { 0x75, 0x6D },
                    new byte[] { 0xEB, 0x6D }),
                (
                    0x564855,
                    new byte[]
                    {
                        0x68,
                        0xE8,
                        0x03,
                        0x00,
                        0x00
                    },
                    new byte[]
                    {
                        0x68,
                        0x00,
                        0x00,
                        0x00,
                        0x00
                    })
            };

        private readonly ILogger logger;

        public ClientLaunchService(ILogger logger)
        {
            this.logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        public void Launch(
            ClientLaunchOptions options,
            ClientLayout layout)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(layout);

            var clientPath = options.ExecutablePath;
            logger.LogInfo(
                $"Attempting to launch client executable: {clientPath}");
            if (!File.Exists(clientPath))
            {
                logger.LogError(
                    "Client executable not found, unable to launch");
                throw new FileNotFoundException(
                    "The client executable was not found.",
                    clientPath);
            }

            var process = StartClientProcess(clientPath);
            PatchAndResume(
                process,
                options,
                layout,
                clientPath);
        }

        private ProcessInformation StartClientProcess(
            string clientPath)
        {
            var startupInfo = new StartupInfo
            {
                Size = Marshal.SizeOf<StartupInfo>()
            };
            var processSecurity = new SecurityAttributes
            {
                Size = Marshal.SizeOf<SecurityAttributes>()
            };
            var threadSecurity = new SecurityAttributes
            {
                Size = Marshal.SizeOf<SecurityAttributes>()
            };

            logger.LogInfo(
                $"Attempting to create process for executable: {clientPath}");
            var wasCreated = NativeMethods.CreateProcess(
                clientPath,
                null,
                ref processSecurity,
                ref threadSecurity,
                false,
                ProcessCreationFlags.Suspended,
                nint.Zero,
                null,
                ref startupInfo,
                out var process);
            if (!wasCreated || process.ProcessId == 0)
            {
                var errorCode =
                    Marshal.GetLastPInvokeError();
                var errorMessage =
                    Marshal.GetLastPInvokeErrorMessage();
                logger.LogError(
                    $"Failed to create client process, code = {errorCode}, message = {errorMessage}");
                throw new Win32Exception(
                    errorCode,
                    "Unable to create client process");
            }

            logger.LogInfo(
                $"Created client process successfully with pid {process.ProcessId}");
            return process;
        }

        private void PatchAndResume(
            ProcessInformation process,
            ClientLaunchOptions options,
            ClientLayout layout,
            string clientPath)
        {
            var processId = process.ProcessId;
            var processResumed = false;
            var plan = ClientPatchPlan.Create(
                options,
                layout);
            logger.LogInfo(
                $"Attempting to patch client process {processId} with the configured layout");

            try
            {
                if (plan.HasClientPatches)
                    ClientPatcher.VerifyPatchClient(
                        clientPath);

                if (plan.HasClientPatches)
                {
                    ApplyPatches(
                        process,
                        options,
                        layout,
                        plan);
                }

                if (NativeMethods.ResumeThread(
                        process.ThreadHandle) == -1)
                {
                    throw new Win32Exception(
                        Marshal.GetLastPInvokeError(),
                        "Unable to resume the patched client process");
                }

                processResumed = true;
            }
            finally
            {
                if (!processResumed)
                {
                    logger.LogWarn(
                        $"Client patching failed; terminating suspended process {processId}");
                    if (!NativeMethods.TerminateProcess(
                            process.ProcessHandle,
                            1))
                    {
                        logger.LogError(
                            $"Unable to terminate suspended client process {processId}");
                    }
                }

                CloseHandle(
                    process.ThreadHandle,
                    "thread",
                    processId);
                CloseHandle(
                    process.ProcessHandle,
                    "process",
                    processId);
            }
        }

        private void ApplyPatches(
            ProcessInformation process,
            ClientLaunchOptions options,
            ClientLayout layout,
            ClientPatchPlan plan)
        {
            var processId = process.ProcessId;
            using var accessor = new ProcessMemoryAccessor(
                processId);
            using var patchStream =
                accessor.GetWritableStream();
            using var writer = new BinaryWriter(
                patchStream,
                Encoding.ASCII,
                leaveOpen: true);

            if (plan.ApplyMultipleInstances)
            {
                logger.LogInfo(
                    $"Applying multiple instance patch to process {processId} (0x{layout.MultipleInstanceAddress:x8})");
                patchStream.Position =
                    layout.MultipleInstanceAddress;
                writer.Write((byte)0x31);
                writer.Write((byte)0xC0);
                writer.Write((byte)0x90);
                writer.Write((byte)0x90);
                writer.Write((byte)0x90);
                writer.Write((byte)0x90);
            }

            if (plan.SkipIntroVideo)
            {
                logger.LogInfo(
                    $"Applying skip intro video patch to process {processId} (0x{layout.IntroVideoAddress:x8})");
                patchStream.Position =
                    layout.IntroVideoAddress;
                writer.Write((byte)0x83);
                writer.Write((byte)0xFA);
                writer.Write((byte)0x00);
                writer.Write((byte)0x90);
                writer.Write((byte)0x90);
                writer.Write((byte)0x90);
            }

            if (plan.SuppressLoginNotification)
            {
                logger.LogInfo(
                    $"Applying suppress login notification patch to process {processId}");
                ApplySuppressLoginNotificationPatch(
                    patchStream,
                    writer);
            }

            if (plan.RemoveWalls)
            {
                logger.LogInfo(
                    $"Applying no walls patch to process {processId} (0x{layout.NoWallAddress:x8})");
                patchStream.Position =
                    layout.NoWallAddress;
                writer.Write((byte)0xEB);
                writer.Write((byte)0x17);
                writer.Write((byte)0x90);
            }

            if (plan.ApplyModifiersKeyFix)
            {
                logger.LogInfo(
                    $"Applying modifiers key fix to process {processId}");
                ClientPatcher.ApplyModifiersKeyFix(
                    patchStream,
                    process.ProcessHandle);
            }

            if (plan.AllowAltToShowGroundItems)
            {
                logger.LogInfo(
                    $"Applying Alt ground-item hints patch to process {processId}");
                ClientPatcher
                    .ApplyAllowAltToShowGroundItems(
                        patchStream,
                        process.ProcessHandle);
            }

            if (plan.ApplyImprovedAutoFollow)
            {
                logger.LogInfo(
                    $"Applying improved auto-follow patch to process {processId}");
                ClientPatcher.ApplyImprovedAutoFollow(
                    patchStream,
                    process.ProcessHandle,
                    options
                        .ImprovedAutoFollowMinimumDistance);
            }

            if (plan.ShowItemQuantitiesInDialogs)
            {
                logger.LogInfo(
                    $"Applying item quantities in dialogs patch to process {processId}");
                ClientPatcher
                    .ApplyShowItemQuantitiesInDialogs(
                        patchStream,
                        process.ProcessHandle);
            }

            if (plan.MakeExchangeDialogDraggable)
            {
                logger.LogInfo(
                    $"Applying draggable exchange dialog patch to process {processId}");
                ClientPatcher
                    .ApplyMakeExchangeDialogDraggable(
                        patchStream,
                        process.ProcessHandle);
            }

            if (plan.ShowExchangeResultsInMessageBar)
            {
                logger.LogInfo(
                    $"Applying exchange results message bar patch to process {processId}");
                ClientPatcher
                    .ApplyShowExchangeResultsInMessageBar(
                        patchStream,
                        process.ProcessHandle);
            }

            logger.LogInfo(
                $"Flushing instruction cache for process {processId}");
            ClientPatcher.FlushInstructionCache(
                process.ProcessHandle);
        }

        internal static void
            ApplySuppressLoginNotificationPatch(
            Stream patchStream,
            BinaryWriter writer)
        {
            ArgumentNullException.ThrowIfNull(patchStream);
            ArgumentNullException.ThrowIfNull(writer);

            foreach (var patch in
                SuppressLoginNotificationPatches)
            {
                patchStream.Position = patch.Address;
                var actual =
                    new byte[patch.Expected.Length];
                patchStream.ReadExactly(actual);

                if (!actual.SequenceEqual(patch.Expected))
                {
                    throw new InvalidDataException(
                        $"Unexpected client bytes at 0x{patch.Address:X}: " +
                        $"expected {Convert.ToHexString(patch.Expected)}, " +
                        $"found {Convert.ToHexString(actual)}.");
                }
            }

            foreach (var patch in
                SuppressLoginNotificationPatches)
            {
                patchStream.Position = patch.Address;
                writer.Write(patch.Replacement);
            }
        }

        private void CloseHandle(
            nint handle,
            string handleKind,
            int processId)
        {
            if (handle == nint.Zero ||
                NativeMethods.CloseHandle(handle))
            {
                return;
            }

            logger.LogWarn(
                $"Unable to close client {handleKind} handle for process {processId}");
        }
    }
}
